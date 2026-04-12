using Vali_Flow.Sql.Builder;

namespace Vali_Flow.Sql.Dialects;

/// <summary>PostgreSQL dialect — uses @param, "column" quoting, native ILIKE operator.</summary>
public sealed class PostgreSqlDialect : ISqlDialect
{
    /// <inheritdoc/>
    public string ParameterPrefix => "@";
    /// <inheritdoc/>
    public string LikeOperator => "LIKE";
    /// <inheritdoc/>
    public string LikeEscapeClause() => " ESCAPE '\\'";

    /// <inheritdoc/>
    public string TrueValue => "TRUE";
    /// <inheritdoc/>
    public string FalseValue => "FALSE";
    /// <inheritdoc/>
    public string DialectName => "PostgreSQL";
    /// <summary>PostgreSQL supports the RETURNING clause on INSERT/UPDATE/DELETE.</summary>
    public bool SupportsReturning => true;
    /// <summary>PostgreSQL supports ON CONFLICT (...) DO NOTHING / DO UPDATE SET syntax.</summary>
    public bool SupportsOnConflict => true;

    /// <inheritdoc/>
    public string QuoteIdentifier(string name) => $"\"{name}\"";

    /// <inheritdoc/>
    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    /// <inheritdoc/>
    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    /// <inheritdoc/>
    public string ILikeExpression(string columnSql, string parameterName)
        => $"{columnSql} ILIKE {parameterName}";

    /// <summary>PostgreSQL LIMIT/OFFSET clause. Supports LIMIT-only, OFFSET-only, or both.</summary>
    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        if (take.HasValue && offset > 0) return $"LIMIT {take.Value} OFFSET {offset}";
        if (take.HasValue) return $"LIMIT {take.Value}";
        if (offset > 0) return $"OFFSET {offset}";
        return string.Empty;
    }

    /// <summary>PostgreSQL uses <c>EXTRACT(part FROM column)</c> for date part extraction.</summary>
    public string DatePartExpression(string columnSql, string part)
        => $"EXTRACT({part.ToUpperInvariant()} FROM {columnSql})";

    /// <inheritdoc/>
    public string CurrentTimestamp => "NOW()";

    /// <summary>PostgreSQL natively supports NULLS FIRST / NULLS LAST in ORDER BY.</summary>
    public bool SupportsNullsOrdering => true;

    /// <summary>Returns "NULLS FIRST", "NULLS LAST", or empty string for Default.</summary>
    public string NullsOrderClause(NullsOrder nulls) => nulls switch
    {
        NullsOrder.First => "NULLS FIRST",
        NullsOrder.Last => "NULLS LAST",
        _ => string.Empty
    };

    /// <summary>PostgreSQL supports <c>FOR UPDATE</c> for exclusive row locking.</summary>
    public string ForUpdateClause => "FOR UPDATE";
    /// <summary>PostgreSQL supports <c>FOR SHARE</c> for shared row locking.</summary>
    public string ForShareClause => "FOR SHARE";

    /// <summary>PostgreSQL natively supports IS DISTINCT FROM / IS NOT DISTINCT FROM.</summary>
    public bool SupportsIsDistinctFrom => true;

    /// <summary>Returns the native <c>col IS DISTINCT FROM param</c> expression.</summary>
    public string IsDistinctFromExpression(string colSql, string paramSql)
        => $"{colSql} IS DISTINCT FROM {paramSql}";

    /// <summary>Returns the native <c>col IS NOT DISTINCT FROM param</c> expression.</summary>
    public string IsNotDistinctFromExpression(string colSql, string paramSql)
        => $"{colSql} IS NOT DISTINCT FROM {paramSql}";

    /// <summary>PostgreSQL requires the RECURSIVE keyword for recursive CTEs.</summary>
    public string RecursiveCteKeyword => "RECURSIVE";

    /// <summary>PostgreSQL supports UPDATE…FROM syntax.</summary>
    public bool SupportsUpdateFrom => true;

    /// <summary>PostgreSQL uses TRIM() for whitespace trimming.</summary>
    public string TrimExpression(string column) => $"TRIM({column})";

    /// <summary>PostgreSQL uses LENGTH().</summary>
    public string StringLengthExpression(string column) => $"LENGTH({column})";

    /// <summary>PostgreSQL uses POSITION(search IN column) - 1 for 0-based index-of.</summary>
    public string IndexOfExpression(string searchParam, string column)
        => $"POSITION({searchParam} IN {column}) - 1";

    /// <summary>PostgreSQL date arithmetic: column + (amount * INTERVAL '1 part').</summary>
    public string DateAddExpression(string datePart, string amount, string column)
        => $"({column} + ({amount} * INTERVAL '1 {datePart.ToLowerInvariant()}'))";

    /// <summary>PostgreSQL uses CEIL().</summary>
    public string CeilingExpression(string column) => $"CEIL({column})";

    /// <summary>PostgreSQL uses TRIM() instead of LTRIM(RTRIM()).</summary>
    public string IsNullOrWhitespaceExpression(string column)
        => $"({column} IS NULL OR TRIM({column}) = '')";
}
