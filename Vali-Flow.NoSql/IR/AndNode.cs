namespace Vali_Flow.NoSql.IR;

/// <summary>Represents a logical AND of two conditions.</summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record AndNode(IConditionNode Left, IConditionNode Right) : IConditionNode;
