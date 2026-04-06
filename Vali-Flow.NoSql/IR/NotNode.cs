namespace Vali_Flow.NoSql.IR;

/// <summary>Represents a logical NOT of a condition.</summary>
/// <param name="Inner">The condition to negate.</param>
public sealed record NotNode(IConditionNode Inner) : IConditionNode
{
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitNot(this);
}
