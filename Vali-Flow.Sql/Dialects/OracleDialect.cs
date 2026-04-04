using Vali_Flow.Sql.Builder;

namespace Vali_Flow.Sql.Dialects;

/// <summary>SQL dialect for Oracle Database 12c+.</summary>
public sealed class OracleDialect : ISqlDialect
{
    /// <inheritdoc/>
    public string ParameterPrefix => ":";
    /// <inheritdoc/>
    public string LikeOperator => "LIKE";
    /// <inheritdoc/>
    public string TrueValue => "1";
    /// <inheritdoc/>
    public string FalseValue => "0";
    /// <inheritdoc/>
    public string DialectName => "Oracle";
    /// <summary>Oracle supports the MERGE statement.</summary>
    public bool SupportsMerge => true;
    /// <summary>Oracle supports the RETURNING INTO clause on DML statements.</summary>
    public bool SupportsReturning => true;
    /// <summary>Oracle natively supports NULLS FIRST / NULLS LAST in ORDER BY.</summary>
    public bool SupportsNullsOrdering => true;
    /// <summary>Oracle supports <c>FOR UPDATE</c> for exclusive row locking.</summary>
    public string ForUpdateClause => "FOR UPDATE";
    /// <inheritdoc/>
    public string CurrentTimestamp => "SYSDATE";
    /// <summary>Oracle does not require the RECURSIVE keyword for recursive CTEs.</summary>
    public string RecursiveCteKeyword => string.Empty;

    /// <inheritdoc/>
    public string QuoteIdentifier(string name) => $"\"{name}\"";

    /// <inheritdoc/>
    public string NullCheck(string columnSql) => $"{columnSql} IS NULL";

    /// <inheritdoc/>
    public string NotNullCheck(string columnSql) => $"{columnSql} IS NOT NULL";

    /// <inheritdoc/>
    public string ILikeExpression(string columnSql, string parameterName)
        => $"UPPER({columnSql}) LIKE UPPER({parameterName})";

    /// <summary>Oracle does not use TOP; uses FETCH NEXT instead.</summary>
    public string SelectTop(int? take) => string.Empty;

    /// <summary>Oracle 12c+ pagination using OFFSET ... ROWS FETCH NEXT ... ROWS ONLY.</summary>
    public string LimitOffset(int? take, int? skip)
    {
        int offset = skip ?? 0;
        if (take.HasValue && offset > 0)
            return $"OFFSET {offset} ROWS FETCH NEXT {take} ROWS ONLY";
        if (take.HasValue)
            return $"OFFSET 0 ROWS FETCH NEXT {take} ROWS ONLY";
        if (offset > 0)
            return $"OFFSET {offset} ROWS";
        return string.Empty;
    }

    /// <summary>Oracle uses <c>EXTRACT(part FROM column)</c> for date part extraction.</summary>
    public string DatePartExpression(string columnSql, string part)
        => $"EXTRACT({part.ToUpperInvariant()} FROM {columnSql})";

    /// <summary>Returns "NULLS FIRST", "NULLS LAST", or empty string for Default.</summary>
    public string NullsOrderClause(NullsOrder nulls) => nulls switch
    {
        NullsOrder.First => "NULLS FIRST",
        NullsOrder.Last  => "NULLS LAST",
        _                => string.Empty
    };

    /// <summary>Oracle uses <c>||</c> for string concatenation.</summary>
    public string ConcatExpression(params string[] parts)
        => string.Join(" || ", parts);
}
