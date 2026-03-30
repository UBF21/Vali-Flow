namespace Vali_Flow.Sql.Dialects;

/// <summary>PostgreSQL dialect — uses @param, "column" quoting, native ILIKE operator.</summary>
public sealed class PostgreSqlDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public string LikeOperator => "LIKE";
    public string TrueValue => "TRUE";
    public string FalseValue => "FALSE";

    public string QuoteIdentifier(string name) => $"\"{name}\"";

    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    public string ILikeExpression(string columnSql, string parameterName)
        => $"{columnSql} ILIKE {parameterName}";

    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        if (take.HasValue && offset > 0) return $"LIMIT {take.Value} OFFSET {offset}";
        if (take.HasValue) return $"LIMIT {take.Value}";
        if (offset > 0) return $"OFFSET {offset}";
        return string.Empty;
    }

    public string DatePartExpression(string columnSql, string part)
        => $"EXTRACT({part.ToUpperInvariant()} FROM {columnSql})";
}
