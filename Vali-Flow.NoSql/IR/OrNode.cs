namespace Vali_Flow.NoSql.IR;

/// <summary>Represents a logical OR of two conditions.</summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record OrNode(IConditionNode Left, IConditionNode Right) : IConditionNode;
