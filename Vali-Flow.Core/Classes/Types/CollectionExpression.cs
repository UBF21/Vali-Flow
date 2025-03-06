using System.Linq.Expressions;
using Vali_Flow.Core.Classes.Base;
using Vali_Flow.Core.Interfaces.Types;

namespace Vali_Flow.Core.Classes.Types;

public class CollectionExpression<TBuilder, T> : ICollectionExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, ICollectionExpression<TBuilder, T>, new()
{
     private readonly BaseExpression<TBuilder, T> _builder;

    public CollectionExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public TBuilder NotEmpty<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector)
    {
        Expression<Func<IEnumerable<TValue?>, bool>> predicate = val => val.Any() == true;
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

    public TBuilder Count<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector, int count)
    {
        Expression<Func<IEnumerable<TValue?>, bool>> predicate = val => val.Count() == count;
        return _builder.Add(selector, predicate);
    }

    public TBuilder CountBetween<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, int min, int max)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(IEnumerable<TValue>), "val");
        
        ParameterExpression countVar = Expression.Variable(typeof(int), "count");
        
        BinaryExpression countAssign = Expression.Assign(countVar, Expression.Call(
            typeof(Enumerable), "Count", new[] { typeof(TValue) }, parameter));
        
        BinaryExpression condition = Expression.AndAlso(
            Expression.GreaterThanOrEqual(countVar, Expression.Constant(min)),
            Expression.LessThanOrEqual(countVar, Expression.Constant(max))
        );

        BlockExpression block = Expression.Block(
            new[] { countVar },
            countAssign,
            condition
        );

        Expression<Func<IEnumerable<TValue>, bool>> predicate = Expression.Lambda<Func<IEnumerable<TValue>, bool>>(block, parameter);
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

    public TBuilder DistinctCount<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector, int count)
    {
        Expression<Func<IEnumerable<TValue?>, bool>> predicate = val => val.Distinct().Count() == count;
        return _builder.Add(selector, predicate);
    }

    public TBuilder None<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
    {
        Expression<Func<IEnumerable<TValue>, bool>> nonePredicate = collection => !collection.Any(predicate.Compile());
        return _builder.Add(selector, nonePredicate);
    }
}