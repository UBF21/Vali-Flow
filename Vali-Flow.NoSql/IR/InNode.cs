namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a membership condition: <c>field IN (v1, v2, ...)</c>.
/// Produced by <c>Enumerable.Contains(collection, x.Field)</c> and <c>collection.Contains(x.Field)</c>.
/// An empty <see cref="Values"/> list always evaluates to false.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Values">The set of values to match against. May be empty.</param>
public sealed record InNode(string Field, IReadOnlyList<object?> Values) : IConditionNode;
