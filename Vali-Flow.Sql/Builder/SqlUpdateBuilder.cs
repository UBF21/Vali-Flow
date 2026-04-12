using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for a parameterized SQL UPDATE statement.
/// </summary>
/// <typeparam name="T">The entity type whose properties map to columns.</typeparam>
/// <example>
/// <code>
/// var result = new SqlUpdateBuilder&lt;User&gt;(new SqlServerDialect())
///     .Table("Users")
///     .Set(x => x.Name, "Bob")
///     .Set(x => x.IsActive, false)
///     .Where(w => w.EqualTo(x => x.Id, 42))
///     .Build();
///
/// // SQL: UPDATE [Users] SET [Name] = @pu0, [IsActive] = @pu1 WHERE [Id] = @pw0
/// </code>
/// </example>
public sealed class SqlUpdateBuilder<T> where T : class
{
    private readonly ISqlDialect _dialect;
    private string? _tableName;
    private string? _schema;
    private readonly List<(string Column, object? Value)> _assignments = new();
    private readonly List<string> _rawSetClauses = new();
    private SqlWhereBuilder<T>? _whereBuilder;
    private Expression<Func<T, bool>>? _wherePredicate;
    private bool _outputUpdated;
    private bool _returning;
    private readonly List<string> _returningColumns = new();
    private string? _tag;
    private int _paramIndex;
    private bool _allowUpdateAll;

    // ── UPDATE FROM / JOIN state ──────────────────────────────────────────────
    private string? _fromTable;
    private string? _fromAlias;
    private readonly List<(string JoinType, string On)> _fromJoins = new();

    /// <summary>Creates a new UPDATE builder using the specified SQL dialect.</summary>
    public SqlUpdateBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    /// <summary>Sets the target table name and optional schema.</summary>
    public SqlUpdateBuilder<T> Table(string tableName, string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        _tableName = tableName;
        _schema = schema;
        return this;
    }

    // ── Column assignments ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a SET clause for a typed column.
    /// <c>.Set(x => x.Name, "Bob")</c> → <c>[Name] = @pu0</c>
    /// </summary>
    public SqlUpdateBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        _assignments.Add((ExpressionHelper.GetMemberName(column), value));
        return this;
    }

    /// <summary>
    /// Adds a raw (non-parameterized) SET clause.
    /// <c>.SetRaw(x => x.Counter, "Counter + 1")</c> → <c>[Counter] = Counter + 1</c>
    /// <c>.SetRaw(x => x.UpdatedAt, "GETDATE()")</c> → <c>[UpdatedAt] = GETDATE()</c>
    /// </summary>
    public SqlUpdateBuilder<T> SetRaw(Expression<Func<T, object>> column, string rawExpression)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (string.IsNullOrWhiteSpace(rawExpression))
            throw new ArgumentException("Raw expression cannot be null or whitespace.", nameof(rawExpression));
        string colName = ExpressionHelper.GetMemberName(column);
        _rawSetClauses.Add($"{_dialect.QuoteIdentifier(colName)} = {rawExpression}");
        return this;
    }

    /// <summary>
    /// Copies one column's current value to another column with no parameters.
    /// <c>.SetColumn(x => x.NameBackup, x => x.Name)</c> → <c>[NameBackup] = [Name]</c>
    /// </summary>
    public SqlUpdateBuilder<T> SetColumn<TValue>(
        Expression<Func<T, TValue>> target,
        Expression<Func<T, TValue>> source)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (source == null) throw new ArgumentNullException(nameof(source));
        string targetCol = ExpressionHelper.GetMemberName(target);
        string sourceCol = ExpressionHelper.GetMemberName(source);
        _rawSetClauses.Add($"{_dialect.QuoteIdentifier(targetCol)} = {_dialect.QuoteIdentifier(sourceCol)}");
        return this;
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>Sets the WHERE clause from a <see cref="SqlWhereBuilder{T}"/> instance.</summary>
    public SqlUpdateBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
    {
        if (whereBuilder == null) throw new ArgumentNullException(nameof(whereBuilder));
        if (_whereBuilder != null)
            throw new InvalidOperationException(
                "Where condition already set. Combine conditions within the same SqlWhereBuilder.");
        _whereBuilder = whereBuilder;
        return this;
    }

    /// <summary>Configures the WHERE clause inline.</summary>
    public SqlUpdateBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var builder = new SqlWhereBuilder<T>();
        configure(builder);
        return Where(builder);
    }

    /// <summary>Sets the WHERE clause from a raw lambda expression.</summary>
    /// <remarks>If both <c>Where(Expression)</c> and <c>Where(SqlWhereBuilder)</c> are used,
    /// both conditions are combined with AND in the final SQL.</remarks>
    public SqlUpdateBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _wherePredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>Sets the WHERE clause from a <see cref="ValiFlow{T}"/> filter.</summary>
    public SqlUpdateBuilder<T> Where(ValiFlow<T> filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        _wherePredicate = filter.Build();
        return this;
    }

    // ── UPDATE FROM / JOIN ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a FROM (SQL Server / PostgreSQL) or JOIN (MySQL) source table to the UPDATE statement,
    /// enabling multi-table updates.
    /// </summary>
    /// <param name="sourceTable">The source table name to join against.</param>
    /// <param name="alias">Optional alias for the source table.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current dialect does not support UPDATE…FROM/JOIN.
    /// SQLite and Oracle do not support this syntax.
    /// </exception>
    public SqlUpdateBuilder<T> FromTable(string sourceTable, string? alias = null)
    {
        if (!_dialect.SupportsUpdateFrom)
            throw new InvalidOperationException(
                $"Dialect '{_dialect.DialectName}' does not support UPDATE\u2026FROM/JOIN syntax.");
        if (string.IsNullOrWhiteSpace(sourceTable))
            throw new ArgumentException("Source table name cannot be null or empty.", nameof(sourceTable));
        _fromTable = sourceTable;
        _fromAlias = alias;
        return this;
    }

    /// <summary>
    /// Adds a JOIN condition to the FROM clause of the UPDATE statement.
    /// For SQL Server and MySQL. Not needed for PostgreSQL (use WHERE instead).
    /// </summary>
    /// <param name="joinType">JOIN type keyword, e.g. "INNER JOIN", "LEFT JOIN".</param>
    /// <param name="onCondition">The ON clause SQL expression.</param>
    public SqlUpdateBuilder<T> JoinOn(string joinType, string onCondition)
    {
        if (string.IsNullOrWhiteSpace(joinType))
            throw new ArgumentException("Join type cannot be null or empty.", nameof(joinType));
        if (string.IsNullOrWhiteSpace(onCondition))
            throw new ArgumentException("ON condition cannot be null or empty.", nameof(onCondition));
        _fromJoins.Add((joinType, onCondition));
        return this;
    }

    // ── Safety guard ──────────────────────────────────────────────────────────

    /// <summary>
    /// Explicitly allows generating an UPDATE without a WHERE clause (affects all rows).
    /// Call this only when a full-table update is intentional.
    /// </summary>
    public SqlUpdateBuilder<T> AllowUpdateAll()
    {
        _allowUpdateAll = true;
        return this;
    }

    // ── OUTPUT / RETURNING ────────────────────────────────────────────────────

    /// <summary>
    /// Appends <c>OUTPUT INSERTED.*</c> after SET (SQL Server only).
    /// Returns the updated rows' new values.
    /// </summary>
    public SqlUpdateBuilder<T> OutputUpdated()
    {
        _outputUpdated = true;
        return this;
    }

    /// <summary>
    /// Appends <c>RETURNING *</c> (or specified columns) after WHERE (PostgreSQL / SQLite).
    /// </summary>
    public SqlUpdateBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
    {
        _returning = true;
        if (columns != null)
            foreach (var col in columns)
                _returningColumns.Add(ExpressionHelper.GetMemberName(col));
        return this;
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    /// <summary>Labels the query for console tracing and SQL comment.</summary>
    public SqlUpdateBuilder<T> Tag(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tag description cannot be null or whitespace.", nameof(description));
        _tag = description;
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>Assembles and returns the final <see cref="SqlQueryResult"/>.</summary>
    public SqlQueryResult Build()
    {
        _paramIndex = 0;

        if (_assignments.Count == 0 && _rawSetClauses.Count == 0)
            throw new InvalidOperationException("At least one SET assignment is required for UPDATE.");

        bool hasWhere = _wherePredicate != null || _whereBuilder != null;
        if (!hasWhere && !_allowUpdateAll)
            throw new InvalidOperationException(
                "UPDATE without a WHERE clause would affect all rows. " +
                "Call Where() to filter rows, or call AllowUpdateAll() to explicitly update all rows.");

        if (_outputUpdated && !_dialect.SupportsInlineOutput)
            throw new InvalidOperationException(
                $"OutputUpdated() requires SqlServerDialect. Current dialect is '{_dialect.DialectName}'. Use Returning() instead.");

        if (_returning && !_dialect.SupportsReturning)
            throw new InvalidOperationException(
                $"Returning() is not supported by '{_dialect.DialectName}'. Use OutputUpdated() for SQL Server.");

        var parameters = new Dictionary<string, object>();

        // SET clauses — use pu prefix to avoid collision with WHERE pw/p params
        var setClauses = new List<string>();
        foreach (var (col, value) in _assignments)
        {
            // Parameter prefix "pu" (update) is intentionally distinct from WHERE builder prefixes
            // ("pw", "p") to prevent key collisions when merging parameter dictionaries.
            string paramName = $"pu{_paramIndex++}";
            setClauses.Add($"{_dialect.QuoteIdentifier(col)} = {_dialect.ParameterPrefix}{paramName}");
            parameters[paramName] = value ?? DBNull.Value;
        }

        // Append raw SET clauses (no parameters) after parameterized ones
        setClauses.AddRange(_rawSetClauses);

        string tableSql = BuildTableSql();
        string setSql = string.Join(", ", setClauses);
        var sqlBuilder = new System.Text.StringBuilder();

        if (_fromTable != null && _dialect.UpdateJoinBeforeSet)
        {
            // MySQL: UPDATE table JOIN source ON … SET …
            string fromSql = BuildFromTableSql();
            sqlBuilder.Append($"UPDATE {tableSql}");
            AppendFromJoins(sqlBuilder, fromSql);
            sqlBuilder.Append($" SET {setSql}");
        }
        else
        {
            // SQL Server / PostgreSQL: UPDATE table SET … [FROM source [JOIN …]]
            sqlBuilder.Append($"UPDATE {tableSql} SET {setSql}");

            if (_outputUpdated)
                sqlBuilder.Append(" OUTPUT INSERTED.*");

            if (_fromTable != null)
            {
                string fromTableSql = BuildFromTableSql();
                string fromFragment = _dialect.UpdateFromClause(fromTableSql);
                if (!string.IsNullOrEmpty(fromFragment))
                {
                    sqlBuilder.Append($" {fromFragment}");
                    AppendFromJoins(sqlBuilder, null);
                }
            }
        }

        // WHERE
        var whereFragments = new List<string>();

        if (_wherePredicate != null)
        {
            var whereResult = ExpressionToSqlVisitor.Translate(_wherePredicate, _dialect);
            whereFragments.Add(whereResult.Sql);
            foreach (var p in whereResult.Parameters)
                parameters[p.Key] = p.Value;
        }

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
            sqlBuilder.Append($" WHERE {combined}");
        }

        if (_returning)
        {
            string retCols = _returningColumns.Count > 0
                ? string.Join(", ", _returningColumns.Select(c => _dialect.QuoteIdentifier(c)))
                : "*";
            sqlBuilder.Append($" RETURNING {retCols}");
        }

        string finalSql = _tag != null ? $"-- {_tag}\n{sqlBuilder}" : sqlBuilder.ToString();
        return new SqlQueryResult(finalSql, parameters, _dialect.ParameterPrefix);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private string BuildFromTableSql()
    {
        string quoted = _dialect.QuoteTable(_fromTable!);
        return _fromAlias != null
            ? $"{quoted} {_dialect.QuoteIdentifier(_fromAlias)}"
            : quoted;
    }

    private void AppendFromJoins(System.Text.StringBuilder sb, string? leadingTable)
    {
        if (leadingTable != null)
            sb.Append($" {leadingTable}");
        foreach (var (joinType, on) in _fromJoins)
            sb.Append($" {joinType} {on}");
    }

    private string BuildTableSql()
    {
        string quotedTable = _dialect.QuoteTable(_tableName ?? typeof(T).Name);
        return !string.IsNullOrEmpty(_schema)
            ? $"{_dialect.QuoteTable(_schema)}.{quotedTable}"
            : quotedTable;
    }
}
