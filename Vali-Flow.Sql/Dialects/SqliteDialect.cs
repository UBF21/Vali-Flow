using Vali_Flow.Sql.Builder;

namespace Vali_Flow.Sql.Dialects;

/// <summary>SQLite dialect — uses @param, "column" quoting, case-insensitive LIKE for ASCII.</summary>
public sealed class SqliteDialect : ISqlDialect
{
    /// <summary>
    /// Controls the INSERT conflict prefix.
    /// Pass <c>"OR IGNORE"</c> or <c>"OR REPLACE"</c> at construction time (default: empty).
    /// </summary>
    private readonly string _insertConflictPrefix;

    /// <summary>Creates a SQLite dialect with an optional fixed INSERT conflict prefix.</summary>
    /// <param name="insertConflictPrefix">
    /// One of <c>"OR IGNORE"</c>, <c>"OR REPLACE"</c>, or empty string (default).
    /// </param>
    public SqliteDialect(string insertConflictPrefix = "")
    {
        _insertConflictPrefix = insertConflictPrefix;
    }

    /// <inheritdoc/>
    public string ParameterPrefix => "@";
    /// <inheritdoc/>
    public string LikeOperator => "LIKE";
    /// <inheritdoc/>
    public string TrueValue => "1";
    /// <inheritdoc/>
    public string FalseValue => "0";
    /// <inheritdoc/>
    public string DialectName => "SQLite";
    /// <summary>SQLite does not support TRUNCATE TABLE; use DELETE FROM instead.</summary>
    public bool SupportsTruncate => false;

    /// <summary>SQLite supports the RETURNING clause on INSERT/UPDATE/DELETE.</summary>
    public bool SupportsReturning => true;
    /// <summary>SQLite supports ON CONFLICT (...) DO NOTHING / DO UPDATE SET syntax.</summary>
    public bool SupportsOnConflict => true;
    /// <inheritdoc/>
    public string InsertConflictPrefix => _insertConflictPrefix;

    /// <inheritdoc/>
    public string QuoteIdentifier(string name) => $"\"{name}\"";

    /// <inheritdoc/>
    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    /// <inheritdoc/>
    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    /// <inheritdoc/>
    public string ILikeExpression(string columnSql, string parameterName)
        => $"LOWER({columnSql}) LIKE LOWER({parameterName})";

    /// <summary>SQLite LIMIT/OFFSET clause. Supports LIMIT-only, OFFSET-only, or both.</summary>
    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        if (take.HasValue && offset > 0) return $"LIMIT {take.Value} OFFSET {offset}";
        if (take.HasValue) return $"LIMIT {take.Value}";
        if (offset > 0) return $"LIMIT -1 OFFSET {offset}";
        return string.Empty;
    }

    /// <inheritdoc/>
    public string CurrentTimestamp => "CURRENT_TIMESTAMP";

    /// <summary>SQLite natively supports NULLS FIRST / NULLS LAST in ORDER BY.</summary>
    public bool SupportsNullsOrdering => true;

    /// <summary>Returns "NULLS FIRST", "NULLS LAST", or empty string for Default.</summary>
    public string NullsOrderClause(NullsOrder nulls) => nulls switch
    {
        NullsOrder.First => "NULLS FIRST",
        NullsOrder.Last => "NULLS LAST",
        _ => string.Empty
    };

    /// <summary>SQLite uses <c>strftime()</c> for date part extraction.</summary>
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

    /// <summary>SQLite natively supports IS DISTINCT FROM / IS NOT DISTINCT FROM.</summary>
    public bool SupportsIsDistinctFrom => true;

    /// <summary>Returns the native <c>col IS DISTINCT FROM param</c> expression.</summary>
    public string IsDistinctFromExpression(string colSql, string paramSql)
        => $"{colSql} IS DISTINCT FROM {paramSql}";

    /// <summary>Returns the native <c>col IS NOT DISTINCT FROM param</c> expression.</summary>
    public string IsNotDistinctFromExpression(string colSql, string paramSql)
        => $"{colSql} IS NOT DISTINCT FROM {paramSql}";

    /// <summary>SQLite requires the RECURSIVE keyword for recursive CTEs.</summary>
    public string RecursiveCteKeyword => "RECURSIVE";

    /// <summary>SQLite uses SUBSTR instead of SUBSTRING.</summary>
    public string SubstringExpression(string column, string start, string? length = null)
        => length != null ? $"SUBSTR({column}, {start}, {length})" : $"SUBSTR({column}, {start})";

    /// <summary>SQLite uses TRIM() for whitespace trimming.</summary>
    public string TrimExpression(string column) => $"TRIM({column})";

    /// <summary>SQLite uses LENGTH().</summary>
    public string StringLengthExpression(string column) => $"LENGTH({column})";

    /// <summary>SQLite uses INSTR(column, search) - 1 for 0-based index-of.</summary>
    public string IndexOfExpression(string searchParam, string column)
        => $"INSTR({column}, {searchParam}) - 1";

    /// <summary>SQLite date arithmetic: date(column, 'N part').</summary>
    public string DateAddExpression(string datePart, string amount, string column)
        => datePart.ToUpperInvariant() switch
        {
            "DAY"   => $"date({column}, '{amount} day')",
            "MONTH" => $"date({column}, '{amount} month')",
            "YEAR"  => $"date({column}, '{amount} year')",
            _       => $"date({column}, '{amount} {datePart.ToLowerInvariant()}')"
        };

    /// <summary>SQLite uses CEIL().</summary>
    public string CeilingExpression(string column) => $"CEIL({column})";
}
