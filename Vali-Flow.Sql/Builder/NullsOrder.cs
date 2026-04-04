namespace Vali_Flow.Sql.Builder;

/// <summary>Controls NULL ordering in ORDER BY clauses.</summary>
public enum NullsOrder
{
    /// <summary>Database default — no explicit NULLS clause emitted.</summary>
    Default,
    /// <summary>NULLs sort before non-null values (NULLS FIRST).</summary>
    First,
    /// <summary>NULLs sort after non-null values (NULLS LAST).</summary>
    Last
}
