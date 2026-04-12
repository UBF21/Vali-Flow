namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Visitor contract for translating an IR condition tree into a provider-specific output.
/// Implement this interface to add a new NoSql backend without modifying any existing node or translator.
/// </summary>
public interface IConditionNodeVisitor<out TResult>
{
    /// <summary>Visit a logical AND node.</summary>
    /// <param name="node">The AND node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitAnd(AndNode node);

    /// <summary>Visit a logical OR node.</summary>
    /// <param name="node">The OR node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitOr(OrNode node);

    /// <summary>Visit a logical NOT node.</summary>
    /// <param name="node">The NOT node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitNot(NotNode node);

    /// <summary>Visit an equality comparison node.</summary>
    /// <param name="node">The equality node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitEqual(EqualNode node);

    /// <summary>Visit a comparison (&gt;, &lt;, &gt;=, &lt;=) node.</summary>
    /// <param name="node">The comparison node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitComparison(ComparisonNode node);

    /// <summary>Visit a string pattern-match (Contains, StartsWith, EndsWith) node.</summary>
    /// <param name="node">The pattern-match node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitLike(LikeNode node);

    /// <summary>Visit a membership (IN) node.</summary>
    /// <param name="node">The IN node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitIn(InNode node);

    /// <summary>Visit a null-check node.</summary>
    /// <param name="node">The null-check node.</param>
    /// <returns>The result of visiting this node.</returns>
    TResult VisitNull(NullNode node);
}
