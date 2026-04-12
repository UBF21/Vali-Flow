namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Traverses a condition node tree and throws <see cref="NotSupportedException"/>
/// when a node of the specified type is encountered.
/// Reusable across all NoSql backends that have partial node support.
/// </summary>
public sealed class UnsupportedNodeDetector : IConditionNodeVisitor<bool>
{
    private readonly Func<IConditionNode, bool> _isUnsupported;
    private readonly Func<IConditionNode, string> _buildMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedNodeDetector"/> class.
    /// </summary>
    /// <param name="isUnsupported">Predicate to determine if a node is unsupported.</param>
    /// <param name="buildMessage">Function to build an error message for an unsupported node.</param>
    public UnsupportedNodeDetector(
        Func<IConditionNode, bool> isUnsupported,
        Func<IConditionNode, string> buildMessage)
    {
        _isUnsupported = isUnsupported;
        _buildMessage  = buildMessage;
    }

    /// <summary>Visit a logical AND node.</summary>
    /// <param name="node">The AND node.</param>
    /// <returns>Always true after visiting children.</returns>
    public bool VisitAnd(AndNode node)        { node.Left.Accept(this); node.Right.Accept(this); return true; }

    /// <summary>Visit a logical OR node.</summary>
    /// <param name="node">The OR node.</param>
    /// <returns>Always true after visiting children.</returns>
    public bool VisitOr(OrNode node)          { node.Left.Accept(this); node.Right.Accept(this); return true; }

    /// <summary>Visit a logical NOT node.</summary>
    /// <param name="node">The NOT node.</param>
    /// <returns>Always true after visiting child.</returns>
    public bool VisitNot(NotNode node)        { node.Inner.Accept(this); return true; }

    /// <summary>Visit an equality comparison node.</summary>
    /// <param name="node">The equality node.</param>
    /// <returns>True if the node is supported.</returns>
    public bool VisitEqual(EqualNode node)    => Check(node);

    /// <summary>Visit a comparison (&gt;, &lt;, &gt;=, &lt;=) node.</summary>
    /// <param name="node">The comparison node.</param>
    /// <returns>True if the node is supported.</returns>
    public bool VisitComparison(ComparisonNode node) => Check(node);

    /// <summary>Visit a string pattern-match node.</summary>
    /// <param name="node">The pattern-match node.</param>
    /// <returns>True if the node is supported.</returns>
    public bool VisitLike(LikeNode node)      => Check(node);

    /// <summary>Visit a membership (IN) node.</summary>
    /// <param name="node">The IN node.</param>
    /// <returns>True if the node is supported.</returns>
    public bool VisitIn(InNode node)          => Check(node);

    /// <summary>Visit a null-check node.</summary>
    /// <param name="node">The null-check node.</param>
    /// <returns>True if the node is supported.</returns>
    public bool VisitNull(NullNode node)      => Check(node);

    private bool Check(IConditionNode node)
    {
        if (_isUnsupported(node))
            throw new NotSupportedException(_buildMessage(node));
        return true;
    }
}
