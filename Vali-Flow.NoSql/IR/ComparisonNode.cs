namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a comparison condition: <c>field &gt; value</c>, <c>field &lt;= value</c>, etc.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Value">The value to compare against. Must not be null.</param>
/// <param name="Op">The comparison operator.</param>
public sealed record ComparisonNode(string Field, object Value, ComparisonOp Op) : IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitComparison(this);
}
