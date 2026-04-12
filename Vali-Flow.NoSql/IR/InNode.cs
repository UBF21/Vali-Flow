namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents a membership condition: <c>field IN (v1, v2, ...)</c>.
/// Produced by <c>Enumerable.Contains(collection, x.Field)</c> and <c>collection.Contains(x.Field)</c>.
/// An empty <see cref="Values"/> list always evaluates to false.
/// </summary>
/// <param name="Field">The document field name.</param>
/// <param name="Values">
/// The set of values to match against. May be empty.
/// <remarks>
/// Note: The Redis translator does not support null values in this list
/// and will throw <see cref="InvalidOperationException"/> if any value is null.
/// </remarks>
/// </param>
public sealed record InNode(string Field, IReadOnlyList<object?> Values) : IConditionNode
{
    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitIn(this);
}
