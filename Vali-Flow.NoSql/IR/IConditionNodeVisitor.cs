namespace Vali_Flow.NoSql.IR;

/// <summary>
/// Visitor contract for translating an IR condition tree into a provider-specific output.
/// Implement this interface to add a new NoSql backend without modifying any existing node or translator.
/// </summary>
public interface IConditionNodeVisitor<out TResult>
{
    TResult VisitAnd(AndNode node);
    TResult VisitOr(OrNode node);
    TResult VisitNot(NotNode node);
    TResult VisitEqual(EqualNode node);
    TResult VisitComparison(ComparisonNode node);
    TResult VisitLike(LikeNode node);
    TResult VisitIn(InNode node);
    TResult VisitNull(NullNode node);
}
