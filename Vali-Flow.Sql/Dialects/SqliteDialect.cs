namespace Vali_Flow.Sql.Dialects;

/// <summary>SQLite dialect — uses @param, "column" quoting, case-insensitive LIKE for ASCII.</summary>
public sealed class SqliteDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public string LikeOperator => "LIKE";
    public string TrueValue => "1";
    public string FalseValue => "0";

    public string QuoteIdentifier(string name) => $"\"{name}\"";

    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    public string ILikeExpression(string columnSql, string parameterName)
        => $"LOWER({columnSql}) LIKE LOWER({parameterName})";

    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        if (take.HasValue && offset > 0) return $"LIMIT {take.Value} OFFSET {offset}";
        if (take.HasValue) return $"LIMIT {take.Value}";
        if (offset > 0) return $"OFFSET {offset}";
        return string.Empty;
    }

    public string DatePartExpression(string columnSql, string part) => part.ToUpperInvariant() switch
    {
        "YEAR" => $"strftime('%Y', {columnSql})",
        "MONTH" => $"strftime('%m', {columnSql})",
        "DAY" => $"strftime('%d', {columnSql})",
        "HOUR" => $"strftime('%H', {columnSql})",
        "MINUTE" => $"strftime('%M', {columnSql})",
        "SECOND" => $"strftime('%S', {columnSql})",
        _ => $"strftime('%Y', {columnSql})"
    };
}
