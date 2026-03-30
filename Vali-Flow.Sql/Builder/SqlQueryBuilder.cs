using System.Linq.Expressions;
using System.Text;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for constructing a complete parameterized SQL SELECT query.
/// </summary>
/// <typeparam name="T">The entity type. Properties of <typeparamref name="T"/> are the available columns.</typeparam>
/// <example>
/// <code>
/// var result = new SqlQueryBuilder&lt;User&gt;(new SqlServerDialect())
///     .From("Users", schema: "dbo")
///     .Select(x => x.Id)
///     .Select(x => x.Name, "UserName")
///     .SelectCount("total")
///     .Where(new ValiFlow&lt;User&gt;().EqualTo(x => x.IsActive, true))
///     .InnerJoin("Orders", "o", "[Users].[Id] = [o].[UserId]")
///     .GroupBy(x => x.Department)
///     .Having("COUNT(*) > 5")
///     .OrderBy(x => x.CreatedAt, ascending: false)
///     .Page(1, 20)
///     .WithHint("NOLOCK")
///     .Build();
/// </code>
/// </example>
public sealed class SqlQueryBuilder<T> where T : class
{
    private readonly ISqlDialect _dialect;
    private string? _tableName;
    private string? _schema;
    private string? _tableHint;
    private readonly List<string> _columns = new();
    private bool _distinct;
    private Expression<Func<T, bool>>? _wherePredicate;
    private readonly List<string> _whereRaw = new();
    private readonly List<(bool Negate, SqlQueryResult Subquery)> _existsClauses = new();
    private readonly List<JoinClause> _joins = new();
    private readonly List<string> _groupBys = new();
    private string? _havingRaw;
    private SqlHavingBuilder<T>? _havingBuilder;
    private SqlWhereBuilder<T>? _whereBuilder;
    private readonly List<OrderByClause> _orderBys = new();
    private int? _take;
    private int? _skip;
    private readonly List<(bool All, SqlQueryResult Query)> _unions = new();

    /// <summary>Creates a new builder using the specified SQL dialect.</summary>
    public SqlQueryBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    // ── Column selection ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds one or more typed columns to the SELECT list.
    /// Use IntelliSense: <c>.Select(x => x.Id, x => x.Name)</c>
    /// </summary>
    public SqlQueryBuilder<T> Select(params Expression<Func<T, object>>[] columns)
    {
        if (columns == null) throw new ArgumentNullException(nameof(columns));
        foreach (var col in columns)
            _columns.Add(_dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(col)));
        return this;
    }

    /// <summary>
    /// Adds a single typed column with an optional alias.
    /// <c>.Select(x => x.Name, "UserName")</c> → <c>[Name] AS [UserName]</c>
    /// </summary>
    public SqlQueryBuilder<T> Select(Expression<Func<T, object>> column, string? alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        _columns.Add(alias != null ? $"{col} AS {_dialect.QuoteIdentifier(alias)}" : col);
        return this;
    }

    /// <summary>Resets the column list to <c>SELECT *</c>.</summary>
    public SqlQueryBuilder<T> SelectAll()
    {
        _columns.Clear();
        return this;
    }

    /// <summary>Appends a raw SQL fragment to the SELECT list. E.g. <c>"GETDATE() AS [Now]"</c>.</summary>
    public SqlQueryBuilder<T> SelectRaw(string rawSql)
    {
        if (string.IsNullOrWhiteSpace(rawSql)) throw new ArgumentException("rawSql cannot be empty.", nameof(rawSql));
        _columns.Add(rawSql);
        return this;
    }

    /// <summary>Adds <c>COUNT(*)</c> (optionally aliased) to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectCount(string? alias = null)
    {
        _columns.Add(alias != null ? $"COUNT(*) AS {_dialect.QuoteIdentifier(alias)}" : "COUNT(*)");
        return this;
    }

    /// <summary>Adds <c>SUM([col])</c> to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectSum(Expression<Func<T, object>> column, string? alias = null)
        => AddAggregate("SUM", column, alias);

    /// <summary>Adds <c>AVG([col])</c> to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectAvg(Expression<Func<T, object>> column, string? alias = null)
        => AddAggregate("AVG", column, alias);

    /// <summary>Adds <c>MIN([col])</c> to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectMin(Expression<Func<T, object>> column, string? alias = null)
        => AddAggregate("MIN", column, alias);

    /// <summary>Adds <c>MAX([col])</c> to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectMax(Expression<Func<T, object>> column, string? alias = null)
        => AddAggregate("MAX", column, alias);

    /// <summary>Adds a <see cref="CaseWhenBuilder"/> expression to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectCase(CaseWhenBuilder caseWhen)
    {
        if (caseWhen == null) throw new ArgumentNullException(nameof(caseWhen));
        _columns.Add(caseWhen.Build(_dialect));
        return this;
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the table name and optional schema.
    /// <c>.From("Users", "dbo")</c> → <c>[dbo].[Users]</c> in SQL Server.
    /// Defaults to <c>typeof(T).Name</c>.
    /// </summary>
    public SqlQueryBuilder<T> From(string tableName, string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        _tableName = tableName;
        _schema = schema;
        return this;
    }

    // ── Joins ─────────────────────────────────────────────────────────────────

    /// <summary>Adds an INNER JOIN clause.</summary>
    /// <param name="table">Joined table name (will be quoted).</param>
    /// <param name="alias">Optional table alias (not quoted — use as-is).</param>
    /// <param name="on">ON condition as raw SQL.</param>
    public SqlQueryBuilder<T> InnerJoin(string table, string? alias, string on)
        => AddJoin("INNER JOIN", table, alias, on);

    /// <summary>Adds a LEFT JOIN clause.</summary>
    public SqlQueryBuilder<T> LeftJoin(string table, string? alias, string on)
        => AddJoin("LEFT JOIN", table, alias, on);

    /// <summary>Adds a RIGHT JOIN clause.</summary>
    public SqlQueryBuilder<T> RightJoin(string table, string? alias, string on)
        => AddJoin("RIGHT JOIN", table, alias, on);

    /// <summary>Adds a FULL OUTER JOIN clause.</summary>
    public SqlQueryBuilder<T> FullOuterJoin(string table, string? alias, string on)
        => AddJoin("FULL OUTER JOIN", table, alias, on);

    /// <summary>Adds a CROSS JOIN clause (no ON condition).</summary>
    public SqlQueryBuilder<T> CrossJoin(string table, string? alias = null)
        => AddJoin("CROSS JOIN", table, alias, onSql: null);

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>Sets a typed WHERE predicate from a <see cref="ValiFlow{T}"/> filter.</summary>
    public SqlQueryBuilder<T> Where(ValiFlow<T> filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        _wherePredicate = filter.Build();
        return this;
    }

    /// <summary>Sets a typed WHERE predicate from a lambda expression.</summary>
    public SqlQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _wherePredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>
    /// Appends a raw SQL fragment to the WHERE clause (ANDed with other conditions).
    /// <para>Parameters in the raw fragment are the caller's responsibility.</para>
    /// </summary>
    public SqlQueryBuilder<T> WhereRaw(string rawSql)
    {
        if (string.IsNullOrWhiteSpace(rawSql)) throw new ArgumentException("rawSql cannot be empty.", nameof(rawSql));
        _whereRaw.Add(rawSql);
        return this;
    }

    /// <summary>
    /// Appends <c>EXISTS (subquery)</c> to the WHERE clause (ANDed).
    /// Subquery parameters are automatically merged and renamed to avoid collisions.
    /// </summary>
    public SqlQueryBuilder<T> WhereExists(SqlQueryResult subquery)
    {
        if (subquery == null) throw new ArgumentNullException(nameof(subquery));
        _existsClauses.Add((false, subquery));
        return this;
    }

    /// <summary>
    /// Appends <c>NOT EXISTS (subquery)</c> to the WHERE clause (ANDed).
    /// </summary>
    public SqlQueryBuilder<T> WhereNotExists(SqlQueryResult subquery)
    {
        if (subquery == null) throw new ArgumentNullException(nameof(subquery));
        _existsClauses.Add((true, subquery));
        return this;
    }

    /// <summary>
    /// Sets the WHERE clause from a <see cref="SqlWhereBuilder{T}"/> fluent builder.
    /// Can be combined with other <c>Where</c> overloads — the result is ANDed with existing conditions.
    /// </summary>
    public SqlQueryBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
    {
        _whereBuilder = whereBuilder ?? throw new ArgumentNullException(nameof(whereBuilder));
        return this;
    }

    // ── GROUP BY / HAVING ─────────────────────────────────────────────────────

    /// <summary>Adds GROUP BY columns: <c>.GroupBy(x => x.Dept, x => x.Status)</c>.</summary>
    public SqlQueryBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)
    {
        if (columns == null) throw new ArgumentNullException(nameof(columns));
        foreach (var col in columns)
            _groupBys.Add(_dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(col)));
        return this;
    }

    /// <summary>Sets a raw HAVING clause (used after <see cref="GroupBy"/>).</summary>
    public SqlQueryBuilder<T> Having(string rawHavingSql)
    {
        if (string.IsNullOrWhiteSpace(rawHavingSql))
            throw new ArgumentException("rawHavingSql cannot be empty.", nameof(rawHavingSql));
        _havingRaw = rawHavingSql;
        return this;
    }

    /// <summary>
    /// Sets the HAVING clause from a <see cref="SqlHavingBuilder{T}"/> fluent builder.
    /// Can be combined with the raw <c>Having(string)</c> overload — both are ANDed.
    /// </summary>
    public SqlQueryBuilder<T> Having(SqlHavingBuilder<T> havingBuilder)
    {
        _havingBuilder = havingBuilder ?? throw new ArgumentNullException(nameof(havingBuilder));
        return this;
    }

    // ── Ordering ──────────────────────────────────────────────────────────────

    /// <summary>Adds a primary ORDER BY column.</summary>
    public SqlQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool ascending = true)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        _orderBys.Add(new OrderByClause(_dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column)), ascending));
        return this;
    }

    /// <summary>Adds a secondary ORDER BY column.</summary>
    public SqlQueryBuilder<T> ThenBy(Expression<Func<T, object>> column, bool ascending = true)
        => OrderBy(column, ascending);

    // ── Pagination ────────────────────────────────────────────────────────────

    /// <summary>Limits the number of rows returned (TOP / LIMIT).</summary>
    public SqlQueryBuilder<T> Take(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Take must be greater than zero.");
        _take = count;
        return this;
    }

    /// <summary>Skips the specified number of rows (OFFSET).</summary>
    public SqlQueryBuilder<T> Skip(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Skip cannot be negative.");
        _skip = count;
        return this;
    }

    /// <summary>
    /// Sets 1-based page pagination.
    /// <c>.Page(2, 20)</c> → Skip 20 rows, take 20. Equivalent to <c>.Skip(20).Take(20)</c>.
    /// </summary>
    public SqlQueryBuilder<T> Page(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be >= 1.");
        _skip = (pageNumber - 1) * pageSize;
        _take = pageSize;
        return this;
    }

    // ── Distinct ──────────────────────────────────────────────────────────────

    /// <summary>Adds DISTINCT to the SELECT clause.</summary>
    public SqlQueryBuilder<T> Distinct()
    {
        _distinct = true;
        return this;
    }

    // ── SQL Server table hints ────────────────────────────────────────────────

    /// <summary>
    /// Appends a table hint after FROM (SQL Server: <c>WITH (NOLOCK)</c>).
    /// <c>.WithHint("NOLOCK")</c> → <c>[Table] WITH (NOLOCK)</c>
    /// </summary>
    public SqlQueryBuilder<T> WithHint(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint)) throw new ArgumentException("Hint cannot be empty.", nameof(hint));
        _tableHint = hint;
        return this;
    }

    // ── UNION ─────────────────────────────────────────────────────────────────

    /// <summary>Appends a UNION (deduplicating) with a prebuilt query.</summary>
    public SqlQueryBuilder<T> Union(SqlQueryResult other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        _unions.Add((false, other));
        return this;
    }

    /// <summary>Appends a UNION ALL (including duplicates) with a prebuilt query.</summary>
    public SqlQueryBuilder<T> UnionAll(SqlQueryResult other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        _unions.Add((true, other));
        return this;
    }

    // ── Preview ───────────────────────────────────────────────────────────────

    /// <summary>Returns a preview of the SQL mid-chain (useful in debugger watch window).</summary>
    public string ToPreviewSql()
    {
        try { return BuildInternal().Sql; }
        catch { return "(preview not available — builder state is incomplete)"; }
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>Assembles and returns the final <see cref="SqlQueryResult"/>.</summary>
    public SqlQueryResult Build() => BuildInternal();

    // ── Internal ──────────────────────────────────────────────────────────────

    private SqlQueryResult BuildInternal()
    {
        ValidatePaginationConstraints();

        var sb = new StringBuilder();
        var parameters = new Dictionary<string, object>();

        // 1 — SELECT [DISTINCT] [TOP N] columns
        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");

        bool useTop = _take.HasValue && (_skip == null || _skip == 0);
        string topFragment = _dialect.SelectTop(useTop ? _take : null);
        if (!string.IsNullOrEmpty(topFragment)) sb.Append(topFragment);

        sb.Append(_columns.Count > 0 ? string.Join(", ", _columns) : "*");

        // 2 — FROM [schema].[table] [WITH (hint)]
        sb.Append($" FROM {BuildTableSql()}");
        if (!string.IsNullOrEmpty(_tableHint))
            sb.Append($" WITH ({_tableHint})");

        // 3 — JOINs
        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} {join.TableSql}");
            if (!string.IsNullOrEmpty(join.OnSql))
                sb.Append($" ON {join.OnSql}");
        }

        // 4 — WHERE (typed predicate + EXISTS clauses + raw fragments)
        var whereFragments = new List<string>();

        if (_wherePredicate != null)
        {
            var whereResult = ExpressionToSqlVisitor.Translate(_wherePredicate, _dialect);
            whereFragments.Add(whereResult.Sql);
            foreach (var p in whereResult.Parameters)
                parameters[p.Key] = p.Value;
        }

        foreach (var (negate, subquery) in _existsClauses)
        {
            var (remappedSql, remappedParams) = RemapParameters(subquery.Sql, subquery.Parameters, parameters.Count);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;
            string keyword = negate ? "NOT EXISTS" : "EXISTS";
            whereFragments.Add($"{keyword} ({remappedSql})");
        }

        whereFragments.AddRange(_whereRaw);

        if (_whereBuilder != null)
        {
            var (builderSql, builderParams) = _whereBuilder.Build(_dialect);
            if (!string.IsNullOrEmpty(builderSql))
            {
                whereFragments.Add(builderSql);
                foreach (var p in builderParams)
                    parameters[p.Key] = p.Value;
            }
        }

        if (whereFragments.Count > 0)
        {
            string combined = whereFragments.Count == 1
                ? whereFragments[0]
                : string.Join(" AND ", whereFragments.Select(f => $"({f})"));
            sb.Append($" WHERE {combined}");
        }

        // 5 — GROUP BY
        if (_groupBys.Count > 0)
            sb.Append($" GROUP BY {string.Join(", ", _groupBys)}");

        // 6 — HAVING
        string? resolvedHaving = _havingRaw;

        if (_havingBuilder != null)
        {
            var (havingSql, havingParams) = _havingBuilder.Build(_dialect);
            if (!string.IsNullOrEmpty(havingSql))
            {
                foreach (var p in havingParams)
                    parameters[p.Key] = p.Value;
                resolvedHaving = !string.IsNullOrEmpty(resolvedHaving)
                    ? $"({resolvedHaving}) AND ({havingSql})"
                    : havingSql;
            }
        }

        if (!string.IsNullOrEmpty(resolvedHaving))
            sb.Append($" HAVING {resolvedHaving}");

        // 7 — ORDER BY
        if (_orderBys.Count > 0)
        {
            var orderParts = _orderBys.Select(o =>
                $"{o.ColumnSql} {(o.Ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending)}");
            sb.Append($" ORDER BY {string.Join(", ", orderParts)}");
        }

        // 8 — LIMIT / OFFSET
        string limitOffset = string.IsNullOrEmpty(topFragment)
            ? _dialect.LimitOffset(_take, _skip)
            : _dialect.LimitOffset(null, _skip);

        if (!string.IsNullOrEmpty(limitOffset))
            sb.Append($" {limitOffset}");

        // 9 — UNION / UNION ALL
        foreach (var (all, unionQuery) in _unions)
        {
            var (remappedSql, remappedParams) = RemapParameters(unionQuery.Sql, unionQuery.Parameters, parameters.Count);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;

            sb.Append(all ? " UNION ALL " : " UNION ");
            sb.Append(remappedSql);
        }

        return new SqlQueryResult(sb.ToString(), parameters);
    }

    private string BuildTableSql()
    {
        string quotedTable = _dialect.QuoteTable(_tableName ?? typeof(T).Name);
        return !string.IsNullOrEmpty(_schema)
            ? $"{_dialect.QuoteTable(_schema)}.{quotedTable}"
            : quotedTable;
    }

    private SqlQueryBuilder<T> AddJoin(string joinType, string table, string? alias, string? onSql)
    {
        if (string.IsNullOrWhiteSpace(table)) throw new ArgumentException("Join table cannot be empty.", nameof(table));
        string tableSql = _dialect.QuoteTable(table);
        if (!string.IsNullOrEmpty(alias)) tableSql += $" {alias}";
        _joins.Add(new JoinClause(joinType, tableSql, onSql ?? string.Empty));
        return this;
    }

    private SqlQueryBuilder<T> AddAggregate(string function, Expression<Func<T, object>> column, string? alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        var expr = alias != null
            ? $"{function}({col}) AS {_dialect.QuoteIdentifier(alias)}"
            : $"{function}({col})";
        _columns.Add(expr);
        return this;
    }

    private void ValidatePaginationConstraints()
    {
        if (_skip.HasValue && _skip > 0 && _orderBys.Count == 0 && _dialect is SqlServerDialect)
        {
            throw new InvalidOperationException(
                "SQL Server requires at least one ORDER BY when using Skip (OFFSET). " +
                "Call .OrderBy(...) before .Skip(...).");
        }
    }

    /// <summary>
    /// Remaps parameter names in <paramref name="sql"/> starting from <paramref name="baseIndex"/>,
    /// so they do not collide with existing parameters collected so far.
    /// Returns the renamed SQL and new parameter dictionary.
    /// </summary>
    private static (string RenamedSql, Dictionary<string, object> RenamedParams) RemapParameters(
        string sql, IReadOnlyDictionary<string, object> sourceParams, int baseIndex)
    {
        if (sourceParams.Count == 0)
            return (sql, new Dictionary<string, object>());

        string remappedSql = sql;
        var renamed = new Dictionary<string, object>();
        int i = baseIndex;

        // Sort by descending key length to prevent partial replacements (e.g. @p10 before @p1)
        foreach (var (key, value) in sourceParams.OrderByDescending(x => x.Key.Length))
        {
            string newKey = $"p{i++}";
            remappedSql = remappedSql.Replace($"@{key}", $"@{newKey}");
            renamed[newKey] = value;
        }

        return (remappedSql, renamed);
    }
}
