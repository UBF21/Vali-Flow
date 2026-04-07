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

    public UnsupportedNodeDetector(
        Func<IConditionNode, bool> isUnsupported,
        Func<IConditionNode, string> buildMessage)
    {
        _isUnsupported = isUnsupported;
        _buildMessage  = buildMessage;
    }

    public bool VisitAnd(AndNode node)        { node.Left.Accept(this); node.Right.Accept(this); return true; }
    public bool VisitOr(OrNode node)          { node.Left.Accept(this); node.Right.Accept(this); return true; }
    public bool VisitNot(NotNode node)        { node.Inner.Accept(this); return true; }
    public bool VisitEqual(EqualNode node)    => Check(node);
    public bool VisitComparison(ComparisonNode node) => Check(node);
    public bool VisitLike(LikeNode node)      => Check(node);
    public bool VisitIn(InNode node)          => Check(node);
    public bool VisitNull(NullNode node)      => Check(node);

    private bool Check(IConditionNode node)
    {
        if (_isUnsupported(node))
            throw new NotSupportedException(_buildMessage(node));
        return true;
    }
}
