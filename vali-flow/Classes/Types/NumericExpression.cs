using System.Linq.Expressions;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Types;

namespace vali_flow.Classes.Types;

public class NumericExpression<TBuilder, T> : INumericExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, INumericExpression<TBuilder, T>, new()
{
    private readonly BaseExpression<TBuilder, T> _builder;

    public NumericExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public TBuilder Zero(Expression<Func<T, int>> selector)
    {
        Expression<Func<int, bool>> predicate = val => val == 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Zero(Expression<Func<T, long>> selector)
    {
        Expression<Func<long, bool>> predicate = val => val == 0L;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Zero(Expression<Func<T, float>> selector)
    {
        Expression<Func<float, bool>> predicate = val => val == 0f;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Zero(Expression<Func<T, double>> selector)
    {
        Expression<Func<double, bool>> predicate = val => val == 0.0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Zero(Expression<Func<T, decimal>> selector)
    {
        Expression<Func<decimal, bool>> predicate = val => val == 0m;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Zero(Expression<Func<T, short>> selector)
    {
        Expression<Func<short, bool>> predicate = val => val == 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotZero(Expression<Func<T, int>> selector)
    {
        Expression<Func<int, bool>> predicate = val => val != 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotZero(Expression<Func<T, long>> selector)
    {
        Expression<Func<long, bool>> predicate = val => val != 0L;
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotZero(Expression<Func<T, float>> selector)
    {
        Expression<Func<float, bool>> predicate = val => val != 0f;
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotZero(Expression<Func<T, double>> selector)
    {
        Expression<Func<double, bool>> predicate = val => val != 0.0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotZero(Expression<Func<T, decimal>> selector)
    {
        Expression<Func<decimal, bool>> predicate = val => val != 0m;
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotZero(Expression<Func<T, short>> selector)
    {
        Expression<Func<short, bool>> predicate = val => val != 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, int>> selector, int min, int max)
    {
        Expression<Func<int, bool>> predicate = val => val >= min && val <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, long>> selector, long min, long max)
    {
        Expression<Func<long, bool>> predicate = val => val >= min && val <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, float>> selector, float min, float max)
    {
        Expression<Func<float, bool>> predicate = val => val >= min && val <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, double>> selector, double min, double max)
    {
        Expression<Func<double, bool>> predicate = val => val >= min && val <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, decimal>> selector, decimal min, decimal max)
    {
        Expression<Func<decimal, bool>> predicate = val => val >= min && val <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, short>> selector, short min, short max)
    {
        Expression<Func<short, bool>> predicate = val => val >= min && val <= max;
        return _builder.Add(selector, predicate);
    }

    public TBuilder InRange(Expression<Func<T, int>> selector, Expression<Func<T, int>> minSelector,
        Expression<Func<T, int>> maxSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (minSelector == null) throw new ArgumentNullException(nameof(minSelector));
        if (maxSelector == null) throw new ArgumentNullException(nameof(maxSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression minSelectorBody = Expression.Invoke(minSelector, parameter);
        InvocationExpression maxSelectorBody = Expression.Invoke(maxSelector, parameter);

        Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(selectorBody, minSelectorBody);
        Expression lessThanOrEqual = Expression.LessThanOrEqual(selectorBody, maxSelectorBody);
        Expression finalExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(finalExpression, parameter);

        return _builder.Add(filter);
    }

    public TBuilder InRange(Expression<Func<T, long>> selector, Expression<Func<T, long>> minSelector,
        Expression<Func<T, long>> maxSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (minSelector == null) throw new ArgumentNullException(nameof(minSelector));
        if (maxSelector == null) throw new ArgumentNullException(nameof(maxSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression minSelectorBody = Expression.Invoke(minSelector, parameter);
        InvocationExpression maxSelectorBody = Expression.Invoke(maxSelector, parameter);

        Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(selectorBody, minSelectorBody);
        Expression lessThanOrEqual = Expression.LessThanOrEqual(selectorBody, maxSelectorBody);
        Expression finalExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(finalExpression, parameter);

        return _builder.Add(filter);
    }

    public TBuilder InRange(Expression<Func<T, float>> selector, Expression<Func<T, float>> minSelector,
        Expression<Func<T, float>> maxSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (minSelector == null) throw new ArgumentNullException(nameof(minSelector));
        if (maxSelector == null) throw new ArgumentNullException(nameof(maxSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression minSelectorBody = Expression.Invoke(minSelector, parameter);
        InvocationExpression maxSelectorBody = Expression.Invoke(maxSelector, parameter);

        Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(selectorBody, minSelectorBody);
        Expression lessThanOrEqual = Expression.LessThanOrEqual(selectorBody, maxSelectorBody);
        Expression finalExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(finalExpression, parameter);

        return _builder.Add(filter);
    }

    public TBuilder InRange(Expression<Func<T, double>> selector, Expression<Func<T, double>> minSelector,
        Expression<Func<T, double>> maxSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (minSelector == null) throw new ArgumentNullException(nameof(minSelector));
        if (maxSelector == null) throw new ArgumentNullException(nameof(maxSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression minSelectorBody = Expression.Invoke(minSelector, parameter);
        InvocationExpression maxSelectorBody = Expression.Invoke(maxSelector, parameter);

        Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(selectorBody, minSelectorBody);
        Expression lessThanOrEqual = Expression.LessThanOrEqual(selectorBody, maxSelectorBody);
        Expression finalExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(finalExpression, parameter);

        return _builder.Add(filter);
    }

    public TBuilder InRange(Expression<Func<T, decimal>> selector, Expression<Func<T, decimal>> minSelector,
        Expression<Func<T, decimal>> maxSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (minSelector == null) throw new ArgumentNullException(nameof(minSelector));
        if (maxSelector == null) throw new ArgumentNullException(nameof(maxSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression minSelectorBody = Expression.Invoke(minSelector, parameter);
        InvocationExpression maxSelectorBody = Expression.Invoke(maxSelector, parameter);

        Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(selectorBody, minSelectorBody);
        Expression lessThanOrEqual = Expression.LessThanOrEqual(selectorBody, maxSelectorBody);
        Expression finalExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(finalExpression, parameter);

        return _builder.Add(filter);
    }

    public TBuilder InRange(Expression<Func<T, short>> selector, Expression<Func<T, short>> minSelector,
        Expression<Func<T, short>> maxSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (minSelector == null) throw new ArgumentNullException(nameof(minSelector));
        if (maxSelector == null) throw new ArgumentNullException(nameof(maxSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression minSelectorBody = Expression.Invoke(minSelector, parameter);
        InvocationExpression maxSelectorBody = Expression.Invoke(maxSelector, parameter);

        Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(selectorBody, minSelectorBody);
        Expression lessThanOrEqual = Expression.LessThanOrEqual(selectorBody, maxSelectorBody);
        Expression finalExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(finalExpression, parameter);

        return _builder.Add(filter);
    }

    public TBuilder GreaterThan(Expression<Func<T, int>> selector, int value)
    {
        Expression<Func<int, bool>> predicate = val => val > value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThan(Expression<Func<T, long>> selector, long value)
    {
        Expression<Func<long, bool>> predicate = val => val > value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThan(Expression<Func<T, float>> selector, float value)
    {
        Expression<Func<float, bool>> predicate = val => val > value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThan(Expression<Func<T, double>> selector, double value)
    {
        Expression<Func<double, bool>> predicate = val => val > value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThan(Expression<Func<T, decimal>> selector, decimal value)
    {
        Expression<Func<decimal, bool>> predicate = val => val > value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThan(Expression<Func<T, short>> selector, short value)
    {
        Expression<Func<short, bool>> predicate = val => val > value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThanOrEqualTo(Expression<Func<T, int>> selector, int value)
    {
        Expression<Func<int, bool>> predicate = val => val >= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThanOrEqualTo(Expression<Func<T, long>> selector, long value)
    {
        Expression<Func<long, bool>> predicate = val => val >= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThanOrEqualTo(Expression<Func<T, float>> selector, float value)
    {
        Expression<Func<float, bool>> predicate = val => val >= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThanOrEqualTo(Expression<Func<T, double>> selector, double value)
    {
        Expression<Func<double, bool>> predicate = val => val >= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value)
    {
        Expression<Func<decimal, bool>> predicate = val => val >= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder GreaterThanOrEqualTo(Expression<Func<T, short>> selector, short value)
    {
        Expression<Func<short, bool>> predicate = val => val >= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThan(Expression<Func<T, int>> selector, int value)
    {
        Expression<Func<int, bool>> predicate = val => val < value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThan(Expression<Func<T, long>> selector, long value)
    {
        Expression<Func<long, bool>> predicate = val => val < value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThan(Expression<Func<T, float>> selector, float value)
    {
        Expression<Func<float, bool>> predicate = val => val < value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThan(Expression<Func<T, double>> selector, double value)
    {
        Expression<Func<double, bool>> predicate = val => val < value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThan(Expression<Func<T, decimal>> selector, decimal value)
    {
        Expression<Func<decimal, bool>> predicate = val => val < value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThan(Expression<Func<T, short>> selector, short value)
    {
        Expression<Func<short, bool>> predicate = val => val < value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThanOrEqualTo(Expression<Func<T, int>> selector, int value)
    {
        Expression<Func<int, bool>> predicate = val => val <= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThanOrEqualTo(Expression<Func<T, long>> selector, long value)
    {
        Expression<Func<long, bool>> predicate = val => val <= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThanOrEqualTo(Expression<Func<T, float>> selector, float value)
    {
        Expression<Func<float, bool>> predicate = val => val <= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThanOrEqualTo(Expression<Func<T, double>> selector, double value)
    {
        Expression<Func<double, bool>> predicate = val => val <= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value)
    {
        Expression<Func<decimal, bool>> predicate = val => val <= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder LessThanOrEqualTo(Expression<Func<T, short>> selector, short value)
    {
        Expression<Func<short, bool>> predicate = val => val <= value;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Positive(Expression<Func<T, int>> selector)
    {
        Expression<Func<int, bool>> predicate = val => val > 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Positive(Expression<Func<T, long>> selector)
    {
        Expression<Func<long, bool>> predicate = val => val > 0L;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Positive(Expression<Func<T, float>> selector)
    {
        Expression<Func<float, bool>> predicate = val => val > 0f;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Positive(Expression<Func<T, double>> selector)
    {
        Expression<Func<double, bool>> predicate = val => val > 0.0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Positive(Expression<Func<T, decimal>> selector)
    {
        Expression<Func<decimal, bool>> predicate = val => val > 0m;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Positive(Expression<Func<T, short>> selector)
    {
        Expression<Func<short, bool>> predicate = val => val > 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Negative(Expression<Func<T, int>> selector)
    {
        Expression<Func<int, bool>> predicate = val => val < 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Negative(Expression<Func<T, long>> selector)
    {
        Expression<Func<long, bool>> predicate = val => val < 0L;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Negative(Expression<Func<T, float>> selector)
    {
        Expression<Func<float, bool>> predicate = val => val < 0f;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Negative(Expression<Func<T, double>> selector)
    {
        Expression<Func<double, bool>> predicate = val => val < 0.0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Negative(Expression<Func<T, decimal>> selector)
    {
        Expression<Func<decimal, bool>> predicate = val => val < 0m;
        return _builder.Add(selector, predicate);
    }

    public TBuilder Negative(Expression<Func<T, short>> selector)
    {
        Expression<Func<short, bool>> predicate = val => val < 0;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MinValue(Expression<Func<T, int>> selector, int minValue)
    {
        Expression<Func<int, bool>> predicate = val => val >= minValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MinValue(Expression<Func<T, long>> selector, long minValue)
    {
        Expression<Func<long, bool>> predicate = val => val >= minValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MinValue(Expression<Func<T, float>> selector, float minValue)
    {
        Expression<Func<float, bool>> predicate = val => val >= minValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MinValue(Expression<Func<T, double>> selector, double minValue)
    {
        Expression<Func<double, bool>> predicate = val => val >= minValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MinValue(Expression<Func<T, decimal>> selector, decimal minValue)
    {
        Expression<Func<decimal, bool>> predicate = val => val >= minValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MinValue(Expression<Func<T, short>> selector, short minValue)
    {
        Expression<Func<short, bool>> predicate = val => val >= minValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxValue(Expression<Func<T, int>> selector, int maxValue)
    {
        Expression<Func<int, bool>> predicate = val => val <= maxValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxValue(Expression<Func<T, long>> selector, long maxValue)
    {
        Expression<Func<long, bool>> predicate = val => val <= maxValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxValue(Expression<Func<T, float>> selector, float maxValue)
    {
        Expression<Func<float, bool>> predicate = val => val <= maxValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxValue(Expression<Func<T, double>> selector, double maxValue)
    {
        Expression<Func<double, bool>> predicate = val => val <= maxValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxValue(Expression<Func<T, decimal>> selector, decimal maxValue)
    {
        Expression<Func<decimal, bool>> predicate = val => val <= maxValue;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxValue(Expression<Func<T, short>> selector, short maxValue)
    {
        Expression<Func<short, bool>> predicate = val => val <= maxValue;
        return _builder.Add(selector, predicate);
    }
}