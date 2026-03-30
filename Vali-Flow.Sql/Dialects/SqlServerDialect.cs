namespace Vali_Flow.Sql.Dialects;

/// <summary>SQL Server dialect — uses [@param], [column] quoting, case-insensitive via collation.</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public string LikeOperator => "LIKE";
    public string TrueValue => "1";
    public string FalseValue => "0";

    public string QuoteIdentifier(string name) => $"[{name}]";

    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

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

    public string DatePartExpression(string columnSql, string part)
        => $"{part.ToUpperInvariant()}({columnSql})";
}
