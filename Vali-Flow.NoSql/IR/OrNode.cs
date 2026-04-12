namespace Vali_Flow.NoSql.IR;

/// <summary>Represents a logical OR of two conditions.</summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record OrNode(IConditionNode Left, IConditionNode Right) : IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitOr(this);
}
