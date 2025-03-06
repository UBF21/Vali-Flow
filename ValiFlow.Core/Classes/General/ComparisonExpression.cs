using System.Linq.Expressions;
using ValiFlow.Core.Classes.Base;
using ValiFlow.Core.Interfaces.General;

namespace ValiFlow.Core.Classes.General;

public class ComparisonExpression<TBuilder,T> : IComparisonExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>,IComparisonExpression<TBuilder, T>,new()
{
    private readonly BaseExpression<TBuilder, T> _builder;

    public ComparisonExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }
    
    public TBuilder NotNull<TValue>(Expression<Func<T, TValue?>> selector)
    {
        Expression<Func<TValue?, bool>> predicate = value => value != null;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Null<TValue>(Expression<Func<T, TValue?>> selector)
    {
        Expression<Func<TValue?, bool>> predicate = value => value == null;
        return _builder.Add(selector, predicate);
    }

    public TBuilder EqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value) where TValue : IEquatable<TValue>
    {
        Expression<Func<TValue, bool>> predicate = v => v.Equals(value);
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value) where TValue : IEquatable<TValue>
    {
        Expression<Func<TValue, bool>> predicate = val => !val.Equals(value);
        return _builder.Add(selector, predicate);
    }
}