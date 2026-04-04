using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.IR;
using Vali_Flow.NoSql.Translators;

namespace Vali_Flow.NoSql.Extensions;

/// <summary>
/// Extension methods that add NoSql IR translation capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
public static class ValiFlowNoSqlExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a provider-agnostic <see cref="IConditionNode"/> tree.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <returns>
    /// The root <see cref="IConditionNode"/> of the IR tree.
    /// Pass this to a provider-specific translator (e.g. <c>MongoFilterTranslator.Translate&lt;T&gt;</c>).
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;User&gt;()
    ///     .EqualTo(x => x.IsActive, true)
    ///     .GreaterThan(x => x.Age, 18);
    ///
    /// IConditionNode ir = filter.ToNoSqlIR();
    /// // ir → AndNode(EqualNode("IsActive", true), ComparisonNode("Age", 18, GreaterThan))
    /// </code>
    /// </example>
    public static IConditionNode ToNoSqlIR<T>(this ValiFlow<T> flow) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        Expression<Func<T, bool>> expression = flow.Build();
        return ExpressionToIRVisitor.Translate(expression);
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into a NoSql IR tree.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static IConditionNode ToNoSqlIR<T>(this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return ExpressionToIRVisitor.Translate(expression);
    }
}
