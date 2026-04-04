using System.Linq.Expressions;

namespace Vali_Flow.Abstractions.Helpers;

/// <summary>
/// Shared expression-tree inspection utilities used by all Vali-Flow translators.
/// Centralizing these helpers ensures that bug fixes propagate to every provider.
/// </summary>
public static class ExpressionInspector
{
    /// <summary>
    /// Returns <c>true</c> when the expression directly accesses a member on the lambda parameter,
    /// stripping any <c>Convert</c> / <c>ConvertChecked</c> wrappers at any depth.
    /// </summary>
    public static bool IsColumnExpression(Expression expr)
    {
        while (expr is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
            expr = u.Operand;

        return expr is MemberExpression { Expression: ParameterExpression };
    }

    /// <summary>
    /// Returns <c>true</c> when the expression is a null literal,
    /// including null wrapped in <c>Convert</c> wrappers.
    /// </summary>
    public static bool IsNullConstant(Expression expr)
        => expr is ConstantExpression { Value: null }
           || (expr is UnaryExpression { NodeType: ExpressionType.Convert } u && IsNullConstant(u.Operand));

    /// <summary>
    /// Evaluates a non-column expression (closure capture, constant, or computed value)
    /// to its runtime value.
    /// </summary>
    /// <returns>The evaluated value, or <c>null</c> for null literals.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the expression cannot be evaluated to a constant value
    /// (e.g. it references a non-captured variable).
    /// </exception>
    public static object? EvaluateExpression(Expression expr)
    {
        if (expr is ConstantExpression constant)
            return constant.Value;

        try
        {
            return Expression.Lambda<Func<object?>>(Expression.Convert(expr, typeof(object))).Compile()();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot evaluate expression '{expr}' to a constant value. " +
                $"Ensure the expression only captures constant or closed-over values.", ex);
        }
    }

    /// <summary>
    /// Flips a comparison operator for constant-on-left cases.
    /// Example: <c>18 &lt; x.Age</c> → flip to <c>x.Age &gt; 18</c>.
    /// Equal and NotEqual are symmetric — they are returned unchanged.
    /// </summary>
    public static ExpressionType FlipComparisonOperator(ExpressionType op) => op switch
    {
        ExpressionType.GreaterThan        => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan           => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual    => ExpressionType.GreaterThanOrEqual,
        _ => op  // Equal, NotEqual, and anything else are symmetric or unsupported
    };
}
