namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Represents an equality (or inequality) condition: <c>field == value</c> / <c>field != value</c>.
/// For null checks use <see cref="NullNode"/> instead.
/// </summary>
public sealed record EqualNode : IConditionNode
{
    /// <summary>The document field name.</summary>
    public string Field { get; }

    /// <summary>The non-null value to compare against.</summary>
    public object Value { get; }

    /// <summary>When <c>true</c>, the condition is <c>!=</c> instead of <c>==</c>.</summary>
    public bool IsNegated { get; }

    /// <param name="field">The document field name.</param>
    /// <param name="value">
    /// The value to compare against. Must not be <c>null</c> —
    /// use <see cref="NullNode"/> for null comparisons.
    /// </param>
    /// <param name="isNegated">When <c>true</c>, the condition is <c>!=</c>.</param>
    public EqualNode(string field, object value, bool isNegated)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        Value = value ?? throw new ArgumentNullException(nameof(value),
            "EqualNode.Value cannot be null. Use NullNode for null checks.");
        IsNegated = isNegated;
    }

    /// <summary>Accepts a visitor for traversing the IR tree.</summary>
    /// <typeparam name="TResult">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor processing this node.</param>
    /// <returns>The result from the visitor.</returns>
    public TResult Accept<TResult>(IConditionNodeVisitor<TResult> visitor) => visitor.VisitEqual(this);
}
