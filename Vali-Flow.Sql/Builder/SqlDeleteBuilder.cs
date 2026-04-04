using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using Vali_Flow.Sql.Translators;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for a parameterized SQL DELETE statement.
/// </summary>
/// <typeparam name="T">The entity type that maps to the target table.</typeparam>
/// <example>
/// <code>
/// var result = new SqlDeleteBuilder&lt;User&gt;(new SqlServerDialect())
///     .From("Users")
///     .Where(w => w.EqualTo(x => x.Id, 42))
///     .Build();
///
/// // SQL: DELETE FROM [Users] WHERE [Id] = @pw0
/// </code>
/// </example>
public sealed class SqlDeleteBuilder<T> where T : class
{
    private readonly ISqlDialect _dialect;
    private string? _tableName;
    private string? _schema;
    private SqlWhereBuilder<T>? _whereBuilder;
    private Expression<Func<T, bool>>? _wherePredicate;
    private bool _outputDeleted;
    private bool _returning;
    private readonly List<string> _returningColumns = new();
    private string? _tag;
    private bool _allowDeleteAll;

    /// <summary>Creates a new DELETE builder using the specified SQL dialect.</summary>
    public SqlDeleteBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    /// <summary>Sets the target table name and optional schema.</summary>
    public SqlDeleteBuilder<T> From(string tableName, string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        _tableName = tableName;
        _schema = schema;
        return this;
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>Sets the WHERE clause from a <see cref="SqlWhereBuilder{T}"/> instance.</summary>
    public SqlDeleteBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
    {
        _whereBuilder = whereBuilder ?? throw new ArgumentNullException(nameof(whereBuilder));
        return this;
    }

    /// <summary>Configures the WHERE clause inline.</summary>
    public SqlDeleteBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var builder = new SqlWhereBuilder<T>();
        configure(builder);
        return Where(builder);
    }

    /// <summary>Sets the WHERE clause from a raw lambda expression.</summary>
    public SqlDeleteBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _wherePredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>Sets the WHERE clause from a <see cref="ValiFlow{T}"/> filter.</summary>
    public SqlDeleteBuilder<T> Where(ValiFlow<T> filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        _wherePredicate = filter.Build();
        return this;
    }

    // ── OUTPUT / RETURNING ────────────────────────────────────────────────────

    /// <summary>
    /// Appends <c>OUTPUT DELETED.*</c> after DELETE FROM (SQL Server only).
    /// Returns the deleted rows.
    /// </summary>
    public SqlDeleteBuilder<T> OutputDeleted()
    {
        _outputDeleted = true;
        return this;
    }

    /// <summary>
    /// Appends <c>RETURNING *</c> (or specified columns) after WHERE (PostgreSQL / SQLite).
    /// </summary>
    public SqlDeleteBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
    {
        _returning = true;
        if (columns != null)
            foreach (var col in columns)
                _returningColumns.Add(ExpressionHelper.GetMemberName(col));
        return this;
    }

    // ── Safety guard ──────────────────────────────────────────────────────────

    /// <summary>
    /// Explicitly allows generating a DELETE without a WHERE clause (affects all rows).
    /// Call this only when a full-table delete is intentional.
    /// </summary>
    public SqlDeleteBuilder<T> AllowDeleteAll()
    {
        _allowDeleteAll = true;
        return this;
    }

    // ── Tag ───────────────────────────────────────────────────────────────────

    /// <summary>Labels the query for console tracing and SQL comment.</summary>
    public SqlDeleteBuilder<T> Tag(string description)
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
        bool hasWhere = _wherePredicate != null || _whereBuilder != null;
        if (!hasWhere && !_allowDeleteAll)
            throw new InvalidOperationException(
                "DELETE without a WHERE clause would affect all rows. " +
                "Call Where() to filter rows, or call AllowDeleteAll() to explicitly delete all rows.");

        if (_outputDeleted && !_dialect.SupportsInlineOutput)
            throw new InvalidOperationException(
                $"OutputDeleted() requires SqlServerDialect. Current dialect is '{_dialect.DialectName}'. Use Returning() instead.");

        if (_returning && !_dialect.SupportsReturning)
            throw new InvalidOperationException(
                $"Returning() is not supported by '{_dialect.DialectName}'. Use OutputDeleted() for SQL Server.");

        var parameters = new Dictionary<string, object>();
        string tableSql = BuildTableSql();
        var sqlBuilder = new System.Text.StringBuilder($"DELETE FROM {tableSql}");

        if (_outputDeleted)
            sqlBuilder.Append(" OUTPUT DELETED.*");

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
        return new SqlQueryResult(finalSql, parameters);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private string BuildTableSql()
    {
        string quotedTable = _dialect.QuoteTable(_tableName ?? typeof(T).Name);
        return !string.IsNullOrEmpty(_schema)
            ? $"{_dialect.QuoteTable(_schema)}.{quotedTable}"
            : quotedTable;
    }
}
