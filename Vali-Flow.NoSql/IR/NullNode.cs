namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a null-check condition: <c>field == null</c> / <c>field != null</c>.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Check">Whether the condition checks for null or non-null.</param>
public sealed record NullNode(string Field, NullCheckOp Check) : IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitNull(this);
}
