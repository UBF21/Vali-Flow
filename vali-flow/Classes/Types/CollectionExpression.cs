using System.Linq.Expressions;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Types;

namespace vali_flow.Classes.Types;

public class CollectionExpression<TBuilder, T> : ICollectionExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, ICollectionExpression<TBuilder, T>, new()
{
    private readonly BaseExpression<TBuilder, T> _builder;

    public CollectionExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public TBuilder NotEmpty(Expression<Func<T, IEnumerable<object?>>> selector)
    {
        Expression<Func<IEnumerable<object?>, bool>> predicate = val => val.Any() == true;
        return _builder.Add(selector, predicate);
    }

    public TBuilder In<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values)
    {
        HashSet<TValue> valueSet = values.ToHashSet();
        Expression<Func<TValue, bool>> predicate = val => valueSet.Contains(val);
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotIn<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values)
    {
        HashSet<TValue> valueSet = values.ToHashSet();
        Expression<Func<TValue, bool>> predicate = val => !valueSet.Contains(val);
        return _builder.Add(selector, predicate);
    }

    public TBuilder Count(Expression<Func<T, IEnumerable<object?>>> selector, int count)
    {
        Expression<Func<IEnumerable<object?>, bool>> predicate = val => val.Count() == count;
        return _builder.Add(selector, predicate);
    }

    public TBuilder CountBetween(Expression<Func<T, IEnumerable<object?>>> selector, int min, int max)
    {
        Expression<Func<IEnumerable<object?>, bool>> predicate = val => val.Count() >= min && val.Count() <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder All<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
    {
        Expression<Func<IEnumerable<TValue>, bool>> allPredicate = collection => collection.All(predicate.Compile());
        return _builder.Add(selector, allPredicate);
    }

    public TBuilder Any<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
    {
        Expression<Func<IEnumerable<TValue>, bool>> anyPredicate = collection => collection.Any(predicate.Compile());
        return _builder.Add(selector, anyPredicate);
    }

    public TBuilder Contains<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, TValue value)
    {
        Expression<Func<IEnumerable<TValue>, bool>> predicate = collection => collection.Contains(value);
        return _builder.Add(selector, predicate);
    }

    public TBuilder DistinctCount(Expression<Func<T, IEnumerable<object?>>> selector, int count)
    {
        Expression<Func<IEnumerable<object?>, bool>> predicate = val => val.Distinct().Count() == count;
        return _builder.Add(selector, predicate);
    }

    public TBuilder None<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
    {
        Expression<Func<IEnumerable<TValue>, bool>> nonePredicate = collection => !collection.Any(predicate.Compile());
        return _builder.Add(selector, nonePredicate);
    }
}