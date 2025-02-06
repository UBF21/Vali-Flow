using System.Linq.Expressions;
using vali_flow.Interfaces.Types;

namespace vali_flow.Interfaces.General;

public interface IComparisonExpression<out TBuilder, T>
{
    TBuilder NotNull(Expression<Func<T, object?>> selector);
    TBuilder Null(Expression<Func<T, object?>> selector);
    TBuilder EqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value) where TValue : IEquatable<TValue>;
    TBuilder NotEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value) where TValue : IEquatable<TValue>;
}