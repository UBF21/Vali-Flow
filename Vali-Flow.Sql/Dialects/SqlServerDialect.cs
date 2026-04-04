namespace Vali_Flow.Sql.Dialects;

/// <summary>SQL Server dialect — uses [@param], [column] quoting, case-insensitive via collation.</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    /// <inheritdoc/>
    public string ParameterPrefix => "@";
    /// <inheritdoc/>
    public string LikeOperator => "LIKE";
    /// <inheritdoc/>
    public string TrueValue => "1";
    /// <inheritdoc/>
    public string FalseValue => "0";
    /// <inheritdoc/>
    public string DialectName => "SqlServer";
    /// <summary>SQL Server supports OUTPUT clauses for DML statements.</summary>
    public bool SupportsInlineOutput => true;
    /// <summary>SQL Server supports the MERGE statement.</summary>
    public bool SupportsMerge => true;
    // SQL Server does NOT support RETURNING — use OUTPUT instead.

    /// <inheritdoc/>
    public string QuoteIdentifier(string name) => $"[{name}]";

    /// <inheritdoc/>
    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    /// <inheritdoc/>
    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    /// <inheritdoc/>
    public string ILikeExpression(string columnSql, string parameterName)
        => $"LOWER({columnSql}) LIKE LOWER({parameterName})";

    /// <summary>SQL Server uses TOP N in the SELECT when there is no OFFSET.</summary>
    public string SelectTop(int? take)
        => take.HasValue ? $"TOP {take.Value} " : string.Empty;

    /// <summary>SQL Server pagination with OFFSET-FETCH (requires ORDER BY).</summary>
    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        if (offset == 0 && take.HasValue)
            return string.Empty; // TOP is used in SELECT instead
        if (offset == 0 && !take.HasValue)
            return string.Empty; // nothing to add
        if (take.HasValue)
            return $"OFFSET {offset} ROWS FETCH NEXT {take.Value} ROWS ONLY";
        return $"OFFSET {offset} ROWS";
    }

    /// <inheritdoc/>
    public string DatePartExpression(string columnSql, string part)
        => $"{part.ToUpperInvariant()}({columnSql})";

    /// <summary>SQL Server uses <c>+</c> for string concatenation.</summary>
    public string ConcatExpression(params string[] parts) => string.Join(" + ", parts);

    /// <summary>SQL Server also escapes <c>[</c> in LIKE patterns to avoid bracket range expressions.</summary>
    public string EscapeLikeValue(string value)
        => value.Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    /// <inheritdoc/>
    public string CurrentTimestamp => "GETDATE()";

    /// <summary>SQL Server supports UPDATE…FROM syntax.</summary>
    public bool SupportsUpdateFrom => true;
}
