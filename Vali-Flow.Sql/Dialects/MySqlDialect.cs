namespace Vali_Flow.Sql.Dialects;

/// <summary>MySQL dialect — uses @param, `column` quoting, case-insensitive LIKE by default collation.</summary>
public sealed class MySqlDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public string LikeOperator => "LIKE";
    public string TrueValue => "TRUE";
    public string FalseValue => "FALSE";

    public string QuoteIdentifier(string name) => $"`{name}`";

    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    public string ILikeExpression(string columnSql, string parameterName)
        => $"{columnSql} LIKE {parameterName}";

    // MySQL max BIGINT UNSIGNED — used as unbounded LIMIT when only OFFSET is specified
    private const ulong MySqlMaxRows = 18446744073709551615UL;

    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        // MySQL requires a LIMIT when using OFFSET; use max bigint as workaround for offset-only
        if (take.HasValue && offset > 0) return $"LIMIT {take.Value} OFFSET {offset}";
        if (take.HasValue) return $"LIMIT {take.Value}";
        if (offset > 0) return $"LIMIT {MySqlMaxRows} OFFSET {offset}";
        return string.Empty;
    }

    public string DatePartExpression(string columnSql, string part)
        => $"{part.ToUpperInvariant()}({columnSql})";
}
