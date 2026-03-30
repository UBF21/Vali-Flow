using System.Linq.Expressions;

namespace Vali_Flow.Sql.Builder;

/// <summary>Internal helpers for extracting metadata from lambda expressions.</summary>
internal static class ExpressionHelper
{
    /// <summary>
    /// Extracts the property name from a lambda like <c>x => x.Name</c> or <c>x => (object)x.Name</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the expression does not point to a direct property access.</exception>
    public static string GetMemberName<T>(Expression<Func<T, object>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var body = expression.Body;

        // Handle boxing conversions: x => (object)x.Prop
        if (body is UnaryExpression unary &&
            (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
            return member.Member.Name;

        throw new ArgumentException(
            $"Expression '{expression}' does not represent a direct property access (e.g. x => x.Name).",
            nameof(expression));
    }

    /// <summary>
    /// Extracts the property name from a typed lambda like <c>x => x.Age</c> or <c>x => x.Name</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the expression does not point to a direct property access.</exception>
    public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var body = expression.Body;

        if (body is UnaryExpression unary &&
            (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
            return member.Member.Name;

        throw new ArgumentException(
            $"Expression '{expression}' does not represent a direct property access (e.g. x => x.Name).",
            nameof(expression));
    }
}
