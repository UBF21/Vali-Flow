namespace Vali_Flow.NoSql.IR;

/// <summary>Represents a logical NOT of a condition.</summary>
/// <param name="Inner">The condition to negate.</param>
public sealed record NotNode(IConditionNode Inner) : IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitNot(this);
}
