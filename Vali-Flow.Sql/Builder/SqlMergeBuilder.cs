using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for SQL MERGE statements (SQL Server only).
/// Use <see cref="SqlInsertBuilder{T}"/> OnConflictDoUpdate for PostgreSQL upsert.
/// </summary>
public sealed class SqlMergeBuilder<TTarget, TSource>
{
    private readonly ISqlDialect _dialect;
    private string? _targetTable;
    private string? _targetSchema;
    private string? _sourceTable;
    private string? _sourceAlias;
    private readonly List<string> _onConditions = new();
    private readonly List<(string TargetCol, string SourceExpr)> _matchedUpdateClauses = new();
    private readonly List<(string TargetCol, string SourceExpr)> _notMatchedInsertClauses = new();
    private bool _whenNotMatchedBySourceDelete;
    private string? _tag;
    private readonly Dictionary<string, object> _parameters = new();
    private int _paramIndex;

    /// <summary>Creates a new MERGE builder using the specified SQL dialect.</summary>
    public SqlMergeBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <summary>Sets the target table for the MERGE.</summary>
    public SqlMergeBuilder<TTarget, TSource> Into(string tableName, string? schema = null)
    {
        _targetTable = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _targetSchema = schema;
        return this;
    }

    /// <summary>Sets the source table name and alias used in the USING clause.</summary>
    public SqlMergeBuilder<TTarget, TSource> Using(string sourceTable, string alias = "src")
    {
        _sourceTable = sourceTable ?? throw new ArgumentNullException(nameof(sourceTable));
        _sourceAlias = alias;
        return this;
    }

    /// <summary>
    /// Adds a join condition: target.col = source.col.
    /// Can be called multiple times for composite keys.
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> On(
        Expression<Func<TTarget, object>> targetKey,
        Expression<Func<TSource, object>> sourceKey)
    {
        if (_sourceTable == null)
            throw new InvalidOperationException("Call Using() before On().");
        string tc = ExpressionHelper.GetMemberName(targetKey);
        string sc = ExpressionHelper.GetMemberName(sourceKey);
        _onConditions.Add(
            $"target.{_dialect.QuoteIdentifier(tc)} = {_sourceAlias ?? "src"}.{_dialect.QuoteIdentifier(sc)}");
        return this;
    }

    // ── WHEN MATCHED ─────────────────────────────────────────────────────────

    /// <summary>
    /// Configures WHEN MATCHED THEN UPDATE SET ... using column-to-column copies.
    /// Use <see cref="MatchedSetColumn"/> for col=col and <see cref="MatchedSetValue{TValue}"/> for col=value.
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> WhenMatchedUpdate(
        Action<SqlMergeBuilder<TTarget, TSource>> configure)
    {
        configure(this);
        return this;
    }

    /// <summary>
    /// In WHEN MATCHED UPDATE: sets target.targetCol = source.sourceCol.
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> MatchedSetColumn(
        Expression<Func<TTarget, object>> targetCol,
        Expression<Func<TSource, object>> sourceCol)
    {
        string tc = ExpressionHelper.GetMemberName(targetCol);
        string sc = ExpressionHelper.GetMemberName(sourceCol);
        _matchedUpdateClauses.Add((tc, $"{_sourceAlias ?? "src"}.{_dialect.QuoteIdentifier(sc)}"));
        return this;
    }

    /// <summary>
    /// In WHEN MATCHED UPDATE: sets target.targetCol = @pmN (parameterized value).
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> MatchedSetValue<TValue>(
        Expression<Func<TTarget, object>> targetCol, TValue value)
    {
        string tc = ExpressionHelper.GetMemberName(targetCol);
        string paramName = $"pm{_paramIndex++}";
        _parameters[paramName] = value ?? (object)DBNull.Value;
        _matchedUpdateClauses.Add((tc, $"{_dialect.ParameterPrefix}{paramName}"));
        return this;
    }

    // ── WHEN NOT MATCHED BY TARGET ────────────────────────────────────────────

    /// <summary>
    /// Configures WHEN NOT MATCHED BY TARGET THEN INSERT using column mappings.
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> WhenNotMatchedInsert(
        Action<SqlMergeBuilder<TTarget, TSource>> configure)
    {
        configure(this);
        return this;
    }

    /// <summary>
    /// In WHEN NOT MATCHED INSERT: maps target.targetCol = source.sourceCol.
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> NotMatchedInsertColumn(
        Expression<Func<TTarget, object>> targetCol,
        Expression<Func<TSource, object>> sourceCol)
    {
        string tc = ExpressionHelper.GetMemberName(targetCol);
        string sc = ExpressionHelper.GetMemberName(sourceCol);
        _notMatchedInsertClauses.Add((tc, $"{_sourceAlias ?? "src"}.{_dialect.QuoteIdentifier(sc)}"));
        return this;
    }

    /// <summary>
    /// In WHEN NOT MATCHED INSERT: maps target.targetCol = @pmN (parameterized value).
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> NotMatchedInsertValue<TValue>(
        Expression<Func<TTarget, object>> targetCol, TValue value)
    {
        string tc = ExpressionHelper.GetMemberName(targetCol);
        string paramName = $"pm{_paramIndex++}";
        _parameters[paramName] = value ?? (object)DBNull.Value;
        _notMatchedInsertClauses.Add((tc, $"{_dialect.ParameterPrefix}{paramName}"));
        return this;
    }

    // ── WHEN NOT MATCHED BY SOURCE ─────────────────────────────────────────────

    /// <summary>
    /// Adds WHEN NOT MATCHED BY SOURCE THEN DELETE.
    /// Removes target rows that have no corresponding source row.
    /// </summary>
    public SqlMergeBuilder<TTarget, TSource> WhenNotMatchedBySourceDelete()
    {
        _whenNotMatchedBySourceDelete = true;
        return this;
    }

    // ── Tag ────────────────────────────────────────────────────────────────────

    /// <summary>Adds a SQL comment header and console tag for traceability.</summary>
    public SqlMergeBuilder<TTarget, TSource> Tag(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tag description cannot be null or whitespace.", nameof(description));
        _tag = description;
        return this;
    }

    // ── Build ──────────────────────────────────────────────────────────────────

    /// <summary>Builds and returns the parameterized MERGE statement.</summary>
    public SqlQueryResult Build()
    {
        if (!_dialect.SupportsMerge)
            throw new InvalidOperationException(
                $"MERGE is not supported by '{_dialect.DialectName}'. " +
                $"Use SqlInsertBuilder.OnConflictDoUpdate() for PostgreSQL upsert.");

        if (string.IsNullOrWhiteSpace(_targetTable))
            throw new InvalidOperationException("Into() must be called before Build() to specify the target table.");

        if (string.IsNullOrWhiteSpace(_sourceTable))
            throw new InvalidOperationException("Using() must be called before Build().");

        if (_onConditions.Count == 0)
            throw new InvalidOperationException("At least one On() condition is required.");

        if (_matchedUpdateClauses.Count == 0 && _notMatchedInsertClauses.Count == 0 && !_whenNotMatchedBySourceDelete)
            throw new InvalidOperationException(
                "At least one of WhenMatchedUpdate(), WhenNotMatchedInsert(), or WhenNotMatchedBySourceDelete() must be called.");

        var parameters = new Dictionary<string, object>(_parameters);
        var sb = new System.Text.StringBuilder();

        // Target table
        string targetTable = BuildTargetTableSql();
        sb.Append($"MERGE INTO {targetTable} {_dialect.MergeTargetAlias("target")}\n");

        // USING
        string quotedSource = _dialect.QuoteTable(_sourceTable!);
        string alias = _sourceAlias ?? "src";
        sb.Append($"USING {quotedSource} AS {alias} ON {string.Join(" AND ", _onConditions)}\n");

        // WHEN MATCHED
        if (_matchedUpdateClauses.Count > 0)
        {
            var setList = _matchedUpdateClauses.Select(c =>
                $"target.{_dialect.QuoteIdentifier(c.TargetCol)} = {c.SourceExpr}");
            sb.Append("WHEN MATCHED THEN\n");
            sb.Append($"    UPDATE SET {string.Join(", ", setList)}\n");
        }

        // WHEN NOT MATCHED BY TARGET
        if (_notMatchedInsertClauses.Count > 0)
        {
            if (!_dialect.SupportsMergeNotMatchedByTarget)
                throw new InvalidOperationException(
                    $"WHEN NOT MATCHED BY TARGET is not supported by {_dialect.GetType().Name}. " +
                    "Use WHEN NOT MATCHED instead.");

            var cols = _notMatchedInsertClauses.Select(c => _dialect.QuoteIdentifier(c.TargetCol));
            var vals = _notMatchedInsertClauses.Select(c => c.SourceExpr);
            sb.Append("WHEN NOT MATCHED BY TARGET THEN\n");
            sb.Append($"    INSERT ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)})\n");
        }

        // WHEN NOT MATCHED BY SOURCE
        if (_whenNotMatchedBySourceDelete)
            sb.Append("WHEN NOT MATCHED BY SOURCE THEN DELETE\n");

        // Trim trailing whitespace + add semicolon
        string sql = sb.ToString().TrimEnd() + ";";
        string finalSql = _tag != null ? $"-- {_tag}\n{sql}" : sql;

        return new SqlQueryResult(finalSql, parameters, _dialect.ParameterPrefix);
    }

    private string BuildTargetTableSql()
    {
        string quoted = _dialect.QuoteTable(_targetTable ?? typeof(TTarget).Name);
        return !string.IsNullOrEmpty(_targetSchema)
            ? $"{_dialect.QuoteTable(_targetSchema)}.{quoted}"
            : quoted;
    }
}
