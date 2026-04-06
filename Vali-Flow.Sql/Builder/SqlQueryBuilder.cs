using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly List<(bool Negate, string Column, SqlQueryResult Subquery)> _inSubqueryClauses = new();
    private readonly List<JoinClause> _joins = new();
    private readonly List<(string JoinType, SqlQueryResult Subquery, string Alias, string On)> _subqueryJoins = new();
    private readonly List<string> _groupBys = new();
    private string? _havingRaw;
    private SqlHavingBuilder<T>? _havingBuilder;
    private SqlWhereBuilder<T>? _whereBuilder;
    private readonly List<OrderByClause> _orderBys = new();
    private int? _take;
    private int? _skip;
    private readonly List<(bool All, SqlQueryResult Query)> _unions = new();
    private readonly List<(string Name, SqlQueryResult Query)> _ctes = new();
    private readonly List<(string Name, SqlQueryResult Anchor, SqlQueryResult Recursive)> _recursiveCtes = new();
    private (SqlQueryResult Query, string Alias)? _fromSubquery;
    private string? _tag;
    private Action<string>? _tagLogger;
    private readonly List<string> _rawOrderBys = new();

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

    /// <summary>Adds <c>COUNT([col])</c> (optionally aliased) to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectCount(Expression<Func<T, object>> column, string? alias = null)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        var expr = alias != null
            ? $"COUNT({col}) AS {_dialect.QuoteIdentifier(alias)}"
            : $"COUNT({col})";
        _columns.Add(expr);
        return this;
    }

    /// <summary>Adds <c>COUNT(DISTINCT [col])</c> (optionally aliased) to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectCountDistinct(Expression<Func<T, object>> column, string? alias = null)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        var expr = alias != null
            ? $"COUNT(DISTINCT {col}) AS {_dialect.QuoteIdentifier(alias)}"
            : $"COUNT(DISTINCT {col})";
        _columns.Add(expr);
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

    /// <summary>Includes column only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> SelectIf(bool condition, Expression<Func<T, object>> column, string? alias = null)
    {
        if (condition) Select(column, alias);
        return this;
    }

    /// <summary>Adds a <see cref="CaseWhenBuilder"/> expression to the SELECT list.</summary>
    public SqlQueryBuilder<T> SelectCase(CaseWhenBuilder caseWhen)
    {
        if (caseWhen == null) throw new ArgumentNullException(nameof(caseWhen));
        _columns.Add(caseWhen.Build(_dialect));
        return this;
    }

    /// <summary>
    /// Adds <c>COALESCE([col], fallbackSql) AS [alias]</c> to the SELECT list.
    /// <para>Example: <c>.SelectCoalesce(x => x.Department, "'N/A'", "Dept")</c>
    /// → <c>COALESCE([Department], 'N/A') AS [Dept]</c></para>
    /// </summary>
    public SqlQueryBuilder<T> SelectCoalesce(
        Expression<Func<T, object>> column, string fallbackSql, string alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (string.IsNullOrWhiteSpace(fallbackSql))
            throw new ArgumentException("fallbackSql cannot be empty.", nameof(fallbackSql));
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("alias cannot be empty.", nameof(alias));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        _columns.Add($"{_dialect.CoalesceExpression(col, fallbackSql)} AS {_dialect.QuoteIdentifier(alias)}");
        return this;
    }

    /// <summary>
    /// Adds <c>CAST([col] AS typeName) AS [alias]</c> to the SELECT list.
    /// <para>Example: <c>.SelectCast(x => x.Age, "FLOAT", "AgeFloat")</c>
    /// → <c>CAST([Age] AS FLOAT) AS [AgeFloat]</c></para>
    /// </summary>
    public SqlQueryBuilder<T> SelectCast(
        Expression<Func<T, object>> column, string typeName, string alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("typeName cannot be empty.", nameof(typeName));
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("alias cannot be empty.", nameof(alias));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        _columns.Add($"{_dialect.CastExpression(col, typeName)} AS {_dialect.QuoteIdentifier(alias)}");
        return this;
    }

    /// <summary>
    /// Adds a dialect-aware string concatenation expression to SELECT.
    /// <para>SQL Server: <c>[col1] + [col2]</c> | PostgreSQL/SQLite: <c>[col1] || [col2]</c></para>
    /// </summary>
    public SqlQueryBuilder<T> SelectConcat(
        string alias, params Expression<Func<T, object>>[] columns)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("alias cannot be empty.", nameof(alias));
        if (columns == null || columns.Length == 0)
            throw new ArgumentException("At least one column must be provided.", nameof(columns));
        var parts = columns.Select(c => _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(c))).ToArray();
        _columns.Add($"{_dialect.ConcatExpression(parts)} AS {_dialect.QuoteIdentifier(alias)}");
        return this;
    }

    /// <summary>Adds a raw SELECT expression only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> SelectRawIf(bool condition, string rawSql)
    {
        if (condition) SelectRaw(rawSql);
        return this;
    }

    // ── CTE ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prepends a Common Table Expression: <c>WITH name AS (query)</c>.
    /// Multiple calls add multiple CTEs separated by commas.
    /// </summary>
    /// <example>
    /// <code>
    /// var activeCte = new SqlQueryBuilder&lt;User&gt;(dialect)
    ///     .From("Users").Where(w => w.EqualTo(x => x.IsActive, true)).Build();
    ///
    /// new SqlQueryBuilder&lt;User&gt;(dialect)
    ///     .WithCte("ActiveUsers", activeCte)
    ///     .From("ActiveUsers")
    ///     .Build();
    /// // WITH "ActiveUsers" AS (SELECT * FROM "Users" WHERE ...) SELECT * FROM "ActiveUsers"
    /// </code>
    /// </example>
    public SqlQueryBuilder<T> WithCte(string name, SqlQueryResult cteQuery)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("CTE name cannot be empty.", nameof(name));
        if (cteQuery == null) throw new ArgumentNullException(nameof(cteQuery));
        _ctes.Add((name, cteQuery));
        return this;
    }

    /// <summary>
    /// Prepends a CTE defined inline.
    /// </summary>
    public SqlQueryBuilder<T> WithCte(string name, Action<SqlQueryBuilder<T>> configure)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("CTE name cannot be empty.", nameof(name));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var sub = new SqlQueryBuilder<T>(_dialect);
        configure(sub);
        return WithCte(name, sub.Build());
    }

    /// <summary>
    /// Adds a recursive CTE.
    /// Generates: WITH [RECURSIVE] name AS (anchor UNION ALL recursive)
    /// </summary>
    /// <param name="name">CTE name.</param>
    /// <param name="anchor">The non-recursive anchor member (base case).</param>
    /// <param name="recursive">The recursive member that references the CTE name.</param>
    public SqlQueryBuilder<T> WithRecursive(string name, SqlQueryResult anchor, SqlQueryResult recursive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CTE name cannot be null or whitespace.", nameof(name));
        if (anchor == null) throw new ArgumentNullException(nameof(anchor));
        if (recursive == null) throw new ArgumentNullException(nameof(recursive));
        _recursiveCtes.Add((name, anchor, recursive));
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

    /// <summary>
    /// Uses a prebuilt subquery as the FROM source: <c>FROM (subquery) alias</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// var inner = new SqlQueryBuilder&lt;User&gt;(dialect).From("Users").Where(...).Build();
    /// new SqlQueryBuilder&lt;User&gt;(dialect).From(inner, "sub").Select(x => x.Name).Build();
    /// // SELECT "Name" FROM (SELECT * FROM "Users" WHERE ...) sub
    /// </code>
    /// </example>
    public SqlQueryBuilder<T> From(SqlQueryResult subquery, string alias)
    {
        if (subquery == null) throw new ArgumentNullException(nameof(subquery));
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("Subquery alias cannot be empty.", nameof(alias));
        _fromSubquery = (subquery, alias);
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

    // ── Subquery Joins ────────────────────────────────────────────────────────

    /// <summary>Adds an INNER JOIN against a derived table (subquery).</summary>
    public SqlQueryBuilder<T> InnerJoinSubquery(SqlQueryResult subquery, string alias, string on)
        => AddJoinSubquery("INNER JOIN", subquery, alias, on);

    /// <summary>Adds a LEFT JOIN against a derived table (subquery).</summary>
    public SqlQueryBuilder<T> LeftJoinSubquery(SqlQueryResult subquery, string alias, string on)
        => AddJoinSubquery("LEFT JOIN", subquery, alias, on);

    /// <summary>Adds a RIGHT JOIN against a derived table (subquery).</summary>
    public SqlQueryBuilder<T> RightJoinSubquery(SqlQueryResult subquery, string alias, string on)
        => AddJoinSubquery("RIGHT JOIN", subquery, alias, on);

    /// <summary>Adds a FULL OUTER JOIN against a derived table (subquery).</summary>
    public SqlQueryBuilder<T> FullOuterJoinSubquery(SqlQueryResult subquery, string alias, string on)
        => AddJoinSubquery("FULL OUTER JOIN", subquery, alias, on);

    private SqlQueryBuilder<T> AddJoinSubquery(string joinType, SqlQueryResult subquery, string alias, string on)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("Alias must not be empty.", nameof(alias));
        if (string.IsNullOrWhiteSpace(on))
            throw new ArgumentException("ON clause must not be empty.", nameof(on));
        _subqueryJoins.Add((joinType, subquery, alias, on));
        return this;
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets a typed WHERE predicate from a <see cref="ValiFlow{T}"/> filter.
    /// </summary>
    /// <remarks>
    /// Parameters emitted by the expression translator use the <c>p</c> prefix (e.g. <c>@p0</c>).
    /// <see cref="Where(SqlWhereBuilder{T})"/> uses the <c>pw</c> prefix (e.g. <c>@pw0</c>), so
    /// both overloads can be combined safely without parameter-name collisions.
    /// Do NOT call both <c>Where(ValiFlow&lt;T&gt;)</c> and <c>Where(Expression&lt;…&gt;)</c>
    /// — the second call overwrites the first (only one predicate is stored in <c>_wherePredicate</c>).
    /// </remarks>
    public SqlQueryBuilder<T> Where(ValiFlow<T> filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        _wherePredicate = filter.Build();
        return this;
    }

    /// <summary>
    /// Sets a typed WHERE predicate from a lambda expression.
    /// </summary>
    /// <remarks>
    /// Parameters emitted by the expression translator use the <c>p</c> prefix (e.g. <c>@p0</c>).
    /// <see cref="Where(SqlWhereBuilder{T})"/> uses the <c>pw</c> prefix (e.g. <c>@pw0</c>), so
    /// both overloads can be combined safely without parameter-name collisions.
    /// Do NOT call both <c>Where(Expression&lt;…&gt;)</c> and <c>Where(ValiFlow&lt;T&gt;)</c>
    /// — the second call overwrites the first (only one predicate is stored in <c>_wherePredicate</c>).
    /// </remarks>
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
    /// Appends <c>[col] IN (subquery)</c> to the WHERE clause (ANDed).
    /// Subquery parameters are automatically remapped to avoid collisions.
    /// </summary>
    /// <example>
    /// <code>
    /// var sub = new SqlQueryBuilder&lt;Order&gt;(dialect).From("Orders")
    ///     .Select(x => x.UserId).Build();
    /// query.WhereInSubquery(x => x.Id, sub);
    /// // WHERE [Id] IN (SELECT [UserId] FROM [Orders])
    /// </code>
    /// </example>
    public SqlQueryBuilder<T> WhereInSubquery<TValue>(Expression<Func<T, TValue>> column, SqlQueryResult subquery)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (subquery == null) throw new ArgumentNullException(nameof(subquery));
        _inSubqueryClauses.Add((false, ExpressionHelper.GetMemberName(column), subquery));
        return this;
    }

    /// <summary>
    /// Appends <c>[col] NOT IN (subquery)</c> to the WHERE clause (ANDed).
    /// </summary>
    public SqlQueryBuilder<T> WhereNotInSubquery<TValue>(Expression<Func<T, TValue>> column, SqlQueryResult subquery)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (subquery == null) throw new ArgumentNullException(nameof(subquery));
        _inSubqueryClauses.Add((true, ExpressionHelper.GetMemberName(column), subquery));
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

    /// <summary>
    /// Configures the WHERE clause inline using a <see cref="SqlWhereBuilder{T}"/> action.
    /// <c>.Where(w => w.EqualTo(x => x.IsActive, true).GreaterThan(x => x.Age, 18))</c>
    /// </summary>
    public SqlQueryBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var builder = new SqlWhereBuilder<T>();
        configure(builder);
        return Where(builder);
    }

    /// <summary>Applies WHERE clause only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> WhereIf(bool condition, Action<SqlWhereBuilder<T>> configure)
    {
        if (condition) Where(configure);
        return this;
    }

    /// <summary>Applies WHERE predicate only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
    {
        if (condition) Where(predicate);
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

    /// <summary>
    /// Adds a raw SQL fragment to the GROUP BY clause.
    /// Useful for multi-table queries where table-qualified column names are needed.
    /// <c>.GroupByRaw("\"Products\".\"Id\", \"Products\".\"Name\"")</c>
    /// </summary>
    public SqlQueryBuilder<T> GroupByRaw(string rawSql)
    {
        if (string.IsNullOrWhiteSpace(rawSql)) throw new ArgumentException("rawSql cannot be empty.", nameof(rawSql));
        _groupBys.Add(rawSql);
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

    /// <summary>
    /// Configures the HAVING clause inline using a <see cref="SqlHavingBuilder{T}"/> action.
    /// <c>.Having(h => h.CountGreaterThan(5).SumGreaterThan(x => x.Amount, 1000m))</c>
    /// </summary>
    public SqlQueryBuilder<T> Having(Action<SqlHavingBuilder<T>> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var builder = new SqlHavingBuilder<T>();
        configure(builder);
        return Having(builder);
    }

    /// <summary>Applies GROUP BY only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> GroupByIf(bool condition, Expression<Func<T, object>> column)
    {
        if (condition) GroupBy(column);
        return this;
    }

    /// <summary>Applies a fluent HAVING clause only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> HavingIf(bool condition, Action<SqlHavingBuilder<T>> configure)
    {
        if (condition) Having(configure);
        return this;
    }

    /// <summary>Applies a raw HAVING clause only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> HavingIf(bool condition, string rawHavingSql)
    {
        if (condition) Having(rawHavingSql);
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

    /// <summary>
    /// Adds ORDER BY with optional NULLS FIRST / NULLS LAST clause.
    /// The nulls parameter is silently ignored on dialects that don't support it (SQL Server, MySQL).
    /// </summary>
    public SqlQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool ascending, NullsOrder nulls)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        _orderBys.Add(new OrderByClause(_dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column)), ascending, nulls));
        return this;
    }

    /// <summary>Applies ORDER BY only when <paramref name="condition"/> is true.</summary>
    public SqlQueryBuilder<T> OrderByIf(bool condition, Expression<Func<T, object>> column, bool ascending = true)
    {
        if (condition) OrderBy(column, ascending);
        return this;
    }

    /// <summary>Adds a secondary ORDER BY column.</summary>
    public SqlQueryBuilder<T> ThenBy(Expression<Func<T, object>> column, bool ascending = true)
        => OrderBy(column, ascending);

    /// <summary>
    /// Appends a raw ORDER BY expression verbatim.
    /// <para>Example: <c>.OrderByRaw("\"Products\".\"Price\" DESC, \"Name\" ASC")</c></para>
    /// </summary>
    public SqlQueryBuilder<T> OrderByRaw(string rawSql)
    {
        if (string.IsNullOrWhiteSpace(rawSql))
            throw new ArgumentException("Raw ORDER BY cannot be null or whitespace.", nameof(rawSql));
        _rawOrderBys.Add(rawSql);
        return this;
    }

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

    // ── EXCEPT / INTERSECT ────────────────────────────────────────────────────

    private readonly List<(string Keyword, SqlQueryResult Query)> _setOperations = new();
    private bool _forUpdate;
    private bool _forShare;

    /// <summary>Appends EXCEPT (removes rows in common) with a prebuilt query.</summary>
    public SqlQueryBuilder<T> Except(SqlQueryResult other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        _setOperations.Add(("EXCEPT", other));
        return this;
    }

    /// <summary>Appends INTERSECT (keeps only rows in common) with a prebuilt query.</summary>
    public SqlQueryBuilder<T> Intersect(SqlQueryResult other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        _setOperations.Add(("INTERSECT", other));
        return this;
    }

    // ── Row locking ───────────────────────────────────────────────────────────

    /// <summary>
    /// Appends FOR UPDATE at the end of the query for exclusive row locking.
    /// No-op on dialects that don't support row locking (e.g. SQL Server — use WithHint instead).
    /// </summary>
    public SqlQueryBuilder<T> ForUpdate() { _forUpdate = true; return this; }

    /// <summary>
    /// Appends FOR SHARE at the end of the query for shared row locking.
    /// No-op on dialects that don't support it.
    /// </summary>
    public SqlQueryBuilder<T> ForShare() { _forShare = true; return this; }

    // ── Window functions ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds a raw window function expression to SELECT.
    /// <c>.SelectWindowRaw("ROW_NUMBER() OVER (ORDER BY [CreatedAt] DESC)", "RowNum")</c>
    /// → <c>ROW_NUMBER() OVER (ORDER BY [CreatedAt] DESC) AS [RowNum]</c>
    /// </summary>
    public SqlQueryBuilder<T> SelectWindowRaw(string windowExpression, string alias)
    {
        if (string.IsNullOrWhiteSpace(windowExpression))
            throw new ArgumentException("windowExpression cannot be empty.", nameof(windowExpression));
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("alias cannot be empty.", nameof(alias));
        return SelectRaw($"{windowExpression} AS {_dialect.QuoteIdentifier(alias)}");
    }

    /// <summary>
    /// Adds <c>ROW_NUMBER() OVER (PARTITION BY [partitionCol] ORDER BY [orderCol] [ASC|DESC]) AS [alias]</c>.
    /// <paramref name="partitionBy"/> is optional (null = no PARTITION BY).
    /// </summary>
    public SqlQueryBuilder<T> SelectRowNumber(
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = "RowNum")
        => SelectWindowFunction("ROW_NUMBER", partitionBy, orderBy, ascending, alias ?? "RowNum");

    /// <summary>
    /// Adds <c>RANK() OVER (PARTITION BY [partitionCol] ORDER BY [orderCol] [ASC|DESC]) AS [alias]</c>.
    /// </summary>
    public SqlQueryBuilder<T> SelectRank(
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = "Rank")
        => SelectWindowFunction("RANK", partitionBy, orderBy, ascending, alias ?? "Rank");

    /// <summary>
    /// Adds <c>DENSE_RANK() OVER (PARTITION BY [partitionCol] ORDER BY [orderCol] [ASC|DESC]) AS [alias]</c>.
    /// </summary>
    public SqlQueryBuilder<T> SelectDenseRank(
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = "DenseRank")
        => SelectWindowFunction("DENSE_RANK", partitionBy, orderBy, ascending, alias ?? "DenseRank");

    // ── LAG / LEAD / FIRST_VALUE / LAST_VALUE ─────────────────────────────────

    /// <summary>
    /// Adds <c>LAG([col], offset) OVER ([PARTITION BY part] ORDER BY orderBy [ASC|DESC]) AS [alias]</c>.
    /// LAG retrieves the value from a previous row within the partition.
    /// </summary>
    public SqlQueryBuilder<T> SelectLag(
        Expression<Func<T, object>> column,
        int offset,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectOffsetWindowFunction("LAG", column, offset, partitionBy, orderBy, ascending,
            alias ?? $"Lag{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>LEAD([col], offset) OVER ([PARTITION BY part] ORDER BY orderBy [ASC|DESC]) AS [alias]</c>.
    /// LEAD retrieves the value from a subsequent row within the partition.
    /// </summary>
    public SqlQueryBuilder<T> SelectLead(
        Expression<Func<T, object>> column,
        int offset,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectOffsetWindowFunction("LEAD", column, offset, partitionBy, orderBy, ascending,
            alias ?? $"Lead{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>FIRST_VALUE([col]) OVER ([PARTITION BY part] ORDER BY orderBy [ASC|DESC]) AS [alias]</c>.
    /// FIRST_VALUE retrieves the first value in the window frame.
    /// </summary>
    public SqlQueryBuilder<T> SelectFirstValue(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectColumnWindowFunction("FIRST_VALUE", column, partitionBy, orderBy, ascending,
            alias ?? $"First{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>LAST_VALUE([col]) OVER ([PARTITION BY part] ORDER BY orderBy [ASC|DESC]) AS [alias]</c>.
    /// LAST_VALUE retrieves the last value in the window frame.
    /// </summary>
    public SqlQueryBuilder<T> SelectLastValue(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectColumnWindowFunction("LAST_VALUE", column, partitionBy, orderBy, ascending,
            alias ?? $"Last{ExpressionHelper.GetMemberName(column)}");

    // ── Aggregate window functions (SUM/AVG/COUNT/MIN/MAX OVER) ───────────────

    /// <summary>
    /// Adds <c>SUM([col]) OVER ([PARTITION BY part] [ORDER BY orderBy]) AS [alias]</c>.
    /// Useful for running totals.
    /// </summary>
    public SqlQueryBuilder<T> SelectSumOver(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>>? orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectAggregateOverFunction("SUM", column, partitionBy, orderBy, ascending,
            alias ?? $"Running{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>AVG([col]) OVER ([PARTITION BY part] [ORDER BY orderBy]) AS [alias]</c>.
    /// </summary>
    public SqlQueryBuilder<T> SelectAvgOver(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>>? orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectAggregateOverFunction("AVG", column, partitionBy, orderBy, ascending,
            alias ?? $"Avg{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>COUNT([col]) OVER ([PARTITION BY part] [ORDER BY orderBy]) AS [alias]</c>.
    /// </summary>
    public SqlQueryBuilder<T> SelectCountOver(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>>? orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectAggregateOverFunction("COUNT", column, partitionBy, orderBy, ascending,
            alias ?? $"Count{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>MIN([col]) OVER ([PARTITION BY part] [ORDER BY orderBy]) AS [alias]</c>.
    /// </summary>
    public SqlQueryBuilder<T> SelectMinOver(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>>? orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectAggregateOverFunction("MIN", column, partitionBy, orderBy, ascending,
            alias ?? $"Min{ExpressionHelper.GetMemberName(column)}");

    /// <summary>
    /// Adds <c>MAX([col]) OVER ([PARTITION BY part] [ORDER BY orderBy]) AS [alias]</c>.
    /// </summary>
    public SqlQueryBuilder<T> SelectMaxOver(
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>>? orderBy,
        bool ascending = true,
        string? alias = null)
        => SelectAggregateOverFunction("MAX", column, partitionBy, orderBy, ascending,
            alias ?? $"Max{ExpressionHelper.GetMemberName(column)}");

    // ── ROW_NUMBER with multiple PARTITION BY columns ─────────────────────────

    /// <summary>
    /// Adds ROW_NUMBER() OVER (PARTITION BY [col1], [col2], ... ORDER BY [orderCol] [ASC|DESC]) AS [alias].
    /// Use this overload when you need to partition by more than one column.
    /// </summary>
    public SqlQueryBuilder<T> SelectRowNumber(
        IReadOnlyList<Expression<Func<T, object>>> partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending = true,
        string? alias = "RowNum")
    {
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        var orderCol = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(orderBy));
        var direction = ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending;

        string overClause;
        if (partitionBy != null && partitionBy.Count > 0)
        {
            var partitionCols = string.Join(", ",
                partitionBy.Select(p => _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(p))));
            overClause = $"OVER (PARTITION BY {partitionCols} ORDER BY {orderCol} {direction})";
        }
        else
        {
            overClause = $"OVER (ORDER BY {orderCol} {direction})";
        }

        return SelectRaw($"ROW_NUMBER() {overClause} AS {_dialect.QuoteIdentifier(alias ?? "RowNum")}");
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Labels the query with a description that is written to the console when <see cref="Build"/> is called
    /// and prepended to the SQL as a <c>-- comment</c>.
    /// Useful for tracing and debugging.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Tag("Get active users over 18").Build();
    /// // Console: [SQL] Get active users over 18
    /// // SQL:     -- Get active users over 18
    /// //          SELECT * FROM [Users] WHERE ...
    /// </code>
    /// </example>
    public SqlQueryBuilder<T> Tag(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tag description cannot be null or whitespace.", nameof(description));
        _tag = description;
        return this;
    }

    /// <summary>
    /// Labels the query with a description and invokes <paramref name="logger"/> with
    /// <c>"[SQL] {description}"</c> when <see cref="Build"/> is called.
    /// When <paramref name="logger"/> is null, the tag is embedded only as a SQL comment.
    /// </summary>
    public SqlQueryBuilder<T> Tag(string description, Action<string>? logger)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tag description cannot be null or whitespace.", nameof(description));
        _tag = description;
        _tagLogger = logger;
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
        if (_tag != null)
        {
            string tagMessage = $"[SQL] {_tag}";
            _tagLogger?.Invoke(tagMessage);
        }

        ValidatePaginationConstraints();

        var sb = new StringBuilder();
        var parameters = new Dictionary<string, object>();

        // 0 — CTEs (WITH clause, including recursive)
        if (_ctes.Count > 0 || _recursiveCtes.Count > 0)
        {
            var cteParts = new List<string>();

            // Regular CTEs
            foreach (var (name, cteQuery) in _ctes)
            {
                var (remappedSql, remappedParams) = RemapParameters(cteQuery.Sql, cteQuery.Parameters, parameters.Count, _dialect.ParameterPrefix);
                foreach (var p in remappedParams)
                    parameters[p.Key] = p.Value;
                cteParts.Add($"{_dialect.QuoteTable(name)} AS ({remappedSql})");
            }

            // Recursive CTEs
            foreach (var (cteName, anchor, recursive) in _recursiveCtes)
            {
                var (anchorSql, anchorParams) = RemapParameters(anchor.Sql, anchor.Parameters, parameters.Count, _dialect.ParameterPrefix);
                foreach (var p in anchorParams)
                    parameters[p.Key] = p.Value;

                var (recursiveSql, recursiveParams) = RemapParameters(recursive.Sql, recursive.Parameters, parameters.Count, _dialect.ParameterPrefix);
                foreach (var p in recursiveParams)
                    parameters[p.Key] = p.Value;

                cteParts.Add($"{_dialect.QuoteTable(cteName)} AS ({anchorSql} UNION ALL {recursiveSql})");
            }

            string recursiveKw = _recursiveCtes.Count > 0 && !string.IsNullOrEmpty(_dialect.RecursiveCteKeyword)
                ? $" {_dialect.RecursiveCteKeyword}"
                : string.Empty;

            sb.Append($"WITH{recursiveKw} {string.Join(", ", cteParts)} ");
        }

        // 1 — SELECT [DISTINCT] [TOP N] columns
        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");

        bool useTop = _take.HasValue && (_skip == null || _skip == 0);
        string topFragment = _dialect.SelectTop(useTop ? _take : null);
        if (!string.IsNullOrEmpty(topFragment)) sb.Append(topFragment);

        sb.Append(_columns.Count > 0 ? string.Join(", ", _columns) : "*");

        // 2 — FROM [schema].[table] [WITH (hint)]
        sb.Append($" FROM {BuildTableSql(parameters)}");
        if (!string.IsNullOrEmpty(_tableHint))
            sb.Append($" WITH ({_tableHint})");

        // 3 — JOINs
        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} {join.TableSql}");
            if (!string.IsNullOrEmpty(join.OnSql))
                sb.Append($" ON {join.OnSql}");
        }

        // Subquery JOINs — parameters remapped at build time to avoid collisions
        foreach (var (joinType, subquery, alias, on) in _subqueryJoins)
        {
            var (remappedSql, remappedParams) = RemapParameters(subquery.Sql, subquery.Parameters, parameters.Count, _dialect.ParameterPrefix);
            foreach (var kv in remappedParams)
                parameters[kv.Key] = kv.Value;
            sb.Append($" {joinType} ({remappedSql}) {_dialect.QuoteIdentifier(alias)} ON {on}");
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
            var (remappedSql, remappedParams) = RemapParameters(subquery.Sql, subquery.Parameters, parameters.Count, _dialect.ParameterPrefix);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;
            string keyword = negate ? "NOT EXISTS" : "EXISTS";
            whereFragments.Add($"{keyword} ({remappedSql})");
        }

        foreach (var (negate, col, subquery) in _inSubqueryClauses)
        {
            var (remappedSql, remappedParams) = RemapParameters(subquery.Sql, subquery.Parameters, parameters.Count, _dialect.ParameterPrefix);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;
            string keyword = negate ? "NOT IN" : "IN";
            whereFragments.Add($"{_dialect.QuoteIdentifier(col)} {keyword} ({remappedSql})");
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
        if (_orderBys.Count > 0 || _rawOrderBys.Count > 0)
        {
            var orderParts = _orderBys.Select(o =>
            {
                string direction = o.Ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending;
                string nullsClause = _dialect.NullsOrderClause(o.Nulls);
                return string.IsNullOrEmpty(nullsClause)
                    ? $"{o.ColumnSql} {direction}"
                    : $"{o.ColumnSql} {direction} {nullsClause}";
            }).Concat(_rawOrderBys);
            sb.Append($" ORDER BY {string.Join(", ", orderParts)}");
        }

        // 8 — LIMIT / OFFSET
        string limitOffset = string.IsNullOrEmpty(topFragment)
            ? _dialect.LimitOffset(_take, _skip)
            : _dialect.LimitOffset(null, _skip);

        if (!string.IsNullOrEmpty(limitOffset))
            sb.Append($" {limitOffset}");

        // 8b — FOR UPDATE / FOR SHARE
        if (_forUpdate && !string.IsNullOrEmpty(_dialect.ForUpdateClause))
            sb.Append($" {_dialect.ForUpdateClause}");
        else if (_forShare && !string.IsNullOrEmpty(_dialect.ForShareClause))
            sb.Append($" {_dialect.ForShareClause}");

        // 9 — UNION / UNION ALL
        foreach (var (all, unionQuery) in _unions)
        {
            var (remappedSql, remappedParams) = RemapParameters(unionQuery.Sql, unionQuery.Parameters, parameters.Count, _dialect.ParameterPrefix);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;

            sb.Append(all ? " UNION ALL " : " UNION ");
            sb.Append(remappedSql);
        }

        // 10 — EXCEPT / INTERSECT
        foreach (var (keyword, setQuery) in _setOperations)
        {
            var (remappedSql, remappedParams) = RemapParameters(setQuery.Sql, setQuery.Parameters, parameters.Count, _dialect.ParameterPrefix);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;

            sb.Append($" {keyword} ");
            sb.Append(remappedSql);
        }

        string finalSql = _tag != null ? $"-- {_tag}\n{sb}" : sb.ToString();
        return new SqlQueryResult(finalSql, parameters);
    }

    private string BuildTableSql(Dictionary<string, object> parameters)
    {
        if (_fromSubquery.HasValue)
        {
            var (subQuery, alias) = _fromSubquery.Value;
            var (remappedSql, remappedParams) = RemapParameters(subQuery.Sql, subQuery.Parameters, parameters.Count, _dialect.ParameterPrefix);
            foreach (var p in remappedParams)
                parameters[p.Key] = p.Value;
            return $"({remappedSql}) {alias}";
        }

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

    private SqlQueryBuilder<T> SelectWindowFunction(
        string function,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending,
        string alias)
    {
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        var orderCol = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(orderBy));
        var direction = ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending;
        string overClause = BuildOverClause(partitionBy, orderCol, direction);
        return SelectRaw($"{function}() {overClause} AS {_dialect.QuoteIdentifier(alias)}");
    }

    /// <summary>
    /// Builds window function with column argument and offset: e.g. LAG([col], 2) OVER (...).
    /// </summary>
    private SqlQueryBuilder<T> SelectOffsetWindowFunction(
        string function,
        Expression<Func<T, object>> column,
        int offset,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending,
        string alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        var orderCol = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(orderBy));
        var direction = ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending;
        string overClause = BuildOverClause(partitionBy, orderCol, direction);
        return SelectRaw($"{function}({col}, {offset}) {overClause} AS {_dialect.QuoteIdentifier(alias)}");
    }

    /// <summary>
    /// Builds window function with a single column argument: e.g. FIRST_VALUE([col]) OVER (...).
    /// </summary>
    private SqlQueryBuilder<T> SelectColumnWindowFunction(
        string function,
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>> orderBy,
        bool ascending,
        string alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));
        var orderCol = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(orderBy));
        var direction = ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending;
        string overClause = BuildOverClause(partitionBy, orderCol, direction);
        return SelectRaw($"{function}({col}) {overClause} AS {_dialect.QuoteIdentifier(alias)}");
    }

    /// <summary>
    /// Builds aggregate window function: e.g. SUM([col]) OVER ([PARTITION BY p] [ORDER BY o]).
    /// </summary>
    private SqlQueryBuilder<T> SelectAggregateOverFunction(
        string function,
        Expression<Func<T, object>> column,
        Expression<Func<T, object>>? partitionBy,
        Expression<Func<T, object>>? orderBy,
        bool ascending,
        string alias)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        var col = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(column));

        var overParts = new List<string>();
        if (partitionBy != null)
            overParts.Add($"PARTITION BY {_dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(partitionBy))}");
        if (orderBy != null)
        {
            var orderCol = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(orderBy));
            var direction = ascending ? _dialect.OrderByAscending : _dialect.OrderByDescending;
            overParts.Add($"ORDER BY {orderCol} {direction}");
        }

        string overClause = overParts.Count > 0
            ? $"OVER ({string.Join(" ", overParts)})"
            : "OVER ()";

        return SelectRaw($"{function}({col}) {overClause} AS {_dialect.QuoteIdentifier(alias)}");
    }

    /// <summary>Builds the OVER (...) clause with optional PARTITION BY.</summary>
    private string BuildOverClause(
        Expression<Func<T, object>>? partitionBy,
        string orderColSql,
        string direction)
    {
        if (partitionBy != null)
        {
            var partitionCol = _dialect.QuoteIdentifier(ExpressionHelper.GetMemberName(partitionBy));
            return $"OVER (PARTITION BY {partitionCol} ORDER BY {orderColSql} {direction})";
        }
        return $"OVER (ORDER BY {orderColSql} {direction})";
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
        bool needsOrder = (_skip.HasValue && _skip > 0) || _take.HasValue;
        if (needsOrder && _orderBys.Count == 0 && _rawOrderBys.Count == 0)
        {
            throw new InvalidOperationException(
                $"Dialect '{_dialect.DialectName}': LIMIT/TOP/OFFSET requires at least one ORDER BY clause to produce deterministic results.");
        }
    }

    /// <summary>
    /// Remaps parameter names in <paramref name="sql"/> starting from <paramref name="baseIndex"/>,
    /// so they do not collide with existing parameters collected so far.
    /// Returns the renamed SQL and new parameter dictionary.
    /// </summary>
    /// <param name="parameterPrefix">
    /// The dialect-specific parameter prefix (e.g. "@" for SQL Server/PostgreSQL/MySQL/SQLite,
    /// ":" for Oracle). Passed from <c>_dialect.ParameterPrefix</c> at call sites.
    /// </param>
    private static (string RenamedSql, Dictionary<string, object> RenamedParams) RemapParameters(
        string sql, IReadOnlyDictionary<string, object> sourceParams, int baseIndex,
        string parameterPrefix = "@")
    {
        if (sourceParams.Count == 0)
            return (sql, new Dictionary<string, object>());

        string remappedSql = sql;
        var renamed = new Dictionary<string, object>();
        int i = baseIndex;

        // Sort by descending key length to prevent partial replacements (e.g. @p10 before @p1).
        // Use regex word-boundary replace to avoid matching @p1 inside @p10.
        foreach (var (key, value) in sourceParams.OrderByDescending(x => x.Key.Length))
        {
            string newKey = $"p{i++}";
            string oldParam = $"{parameterPrefix}{key}";
            remappedSql = Regex.Replace(remappedSql, Regex.Escape(oldParam) + @"\b", $"{parameterPrefix}{newKey}");
            renamed[newKey] = value;
        }

        return (remappedSql, renamed);
    }
}
