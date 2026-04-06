namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a comparison condition: <c>field &gt; value</c>, <c>field &lt;= value</c>, etc.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Value">The value to compare against. Must not be null.</param>
/// <param name="Op">The comparison operator.</param>
public sealed record ComparisonNode(string Field, object Value, ComparisonOp Op) : IConditionNode
{
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitComparison(this);
}
