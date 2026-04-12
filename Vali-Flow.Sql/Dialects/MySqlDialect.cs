namespace Vali_Flow.Sql.Dialects;

/// <summary>MySQL dialect — uses @param, `column` quoting, case-insensitive LIKE by default collation.</summary>
public sealed class MySqlDialect : ISqlDialect
{
    /// <inheritdoc/>
    public string ParameterPrefix => "@";
    /// <inheritdoc/>
    public string LikeOperator => "LIKE";
    /// <inheritdoc/>
    public string TrueValue => "TRUE";
    /// <inheritdoc/>
    public string FalseValue => "FALSE";
    /// <inheritdoc/>
    public string DialectName => "MySQL";
    /// <summary>MySQL does not support RETURNING for UPDATE/DELETE (only MariaDB does). INSERT uses ON DUPLICATE KEY UPDATE instead.</summary>
    public bool SupportsReturning => false;
    /// <summary>MySQL supports ON DUPLICATE KEY UPDATE syntax.</summary>
    public bool SupportsOnDuplicateKey => true;
    /// <summary>Returns "IGNORE" for INSERT IGNORE INTO syntax.</summary>
    public string InsertConflictPrefix => "IGNORE";

    /// <inheritdoc/>
    public string QuoteIdentifier(string name) => $"`{name}`";

    /// <inheritdoc/>
    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    /// <inheritdoc/>
    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    /// <inheritdoc/>
    /// <remarks>
    /// MySQL LIKE is case-insensitive for <c>_ci</c> (case-insensitive) collations, which is the default
    /// for most MySQL installations. For <c>_cs</c> (case-sensitive) collations, an explicit
    /// <c>COLLATE</c> clause is required to achieve true case-insensitive matching
    /// (e.g. <c>col LIKE @p COLLATE utf8mb4_general_ci</c>). This builder does not emit COLLATE;
    /// callers on case-sensitive collations must apply it manually via raw SQL.
    /// </remarks>
    public string ILikeExpression(string columnSql, string parameterName)
        => $"{columnSql} LIKE {parameterName}";

    // MySQL max BIGINT UNSIGNED — used as unbounded LIMIT when only OFFSET is specified
    private const ulong MySqlMaxRows = 18446744073709551615UL;

    /// <summary>MySQL LIMIT/OFFSET clause. When only OFFSET is specified, uses max BIGINT as the LIMIT workaround.</summary>
    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        // MySQL requires a LIMIT when using OFFSET; use max bigint as workaround for offset-only
        if (take.HasValue && offset > 0) return $"LIMIT {take.Value} OFFSET {offset}";
        if (take.HasValue) return $"LIMIT {take.Value}";
        if (offset > 0) return $"LIMIT {MySqlMaxRows} OFFSET {offset}";
        return string.Empty;
    }

    /// <inheritdoc/>
    public string DatePartExpression(string columnSql, string part)
        => $"{part.ToUpperInvariant()}({columnSql})";

    /// <summary>MySQL uses the <c>CONCAT()</c> function for string concatenation.</summary>
    public string ConcatExpression(params string[] parts) => $"CONCAT({string.Join(", ", parts)})";

    /// <inheritdoc/>
    public string CurrentTimestamp => "NOW()";

    /// <summary>MySQL supports <c>FOR UPDATE</c> for exclusive row locking.</summary>
    public string ForUpdateClause => "FOR UPDATE";
    /// <summary>MySQL supports <c>FOR SHARE</c> for shared row locking.</summary>
    public string ForShareClause => "FOR SHARE";

    /// <summary>MySQL 8.0+ supports recursive CTEs via the RECURSIVE keyword.</summary>
    public string RecursiveCteKeyword => "RECURSIVE";

    /// <summary>MySQL supports UPDATE…JOIN syntax (JOIN before SET).</summary>
    public bool SupportsUpdateFrom => true;

    /// <summary>MySQL places the JOIN clause before SET, not after SET.</summary>
    public bool UpdateJoinBeforeSet => true;

    /// <summary>MySQL handles JOIN before SET — FROM fragment is not used.</summary>
    public string UpdateFromClause(string tableSql) => string.Empty;

    /// <summary>MySQL uses TRIM() for whitespace trimming.</summary>
    public string TrimExpression(string column) => $"TRIM({column})";

    /// <summary>MySQL uses LENGTH().</summary>
    public string StringLengthExpression(string column) => $"LENGTH({column})";

    /// <summary>MySQL uses LOCATE(search, column) - 1 for 0-based index-of.</summary>
    public string IndexOfExpression(string searchParam, string column)
        => $"LOCATE({searchParam}, {column}) - 1";

    /// <summary>MySQL uses DATE_ADD(column, INTERVAL amount PART).</summary>
    public string DateAddExpression(string datePart, string amount, string column)
        => $"DATE_ADD({column}, INTERVAL {amount} {datePart.ToUpperInvariant()})";

    /// <summary>MySQL uses CEIL().</summary>
    public string CeilingExpression(string column) => $"CEIL({column})";

    /// <inheritdoc/>
    /// <remarks>Explicit ESCAPE clause ensures MySQL honors backslash escaping regardless of the NO_BACKSLASH_ESCAPES SQL mode.</remarks>
    public string LikeEscapeClause() => " ESCAPE '\\'";

    /// <summary>MySQL uses TRIM() instead of LTRIM(RTRIM()).</summary>
    public string IsNullOrWhitespaceExpression(string column)
        => $"({column} IS NULL OR TRIM({column}) = '')";
}
