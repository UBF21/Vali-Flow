using System.Linq.Expressions;

namespace vali_flow.Interfaces.Types;

public interface INumericExpression<out TBuilder, T>
{
    TBuilder Zero(Expression<Func<T, int>> selector);
    TBuilder Zero(Expression<Func<T, long>> selector);
    TBuilder Zero(Expression<Func<T, float>> selector);
    TBuilder Zero(Expression<Func<T, double>> selector);
    TBuilder Zero(Expression<Func<T, decimal>> selector);
    TBuilder Zero(Expression<Func<T, short>> selector);

    TBuilder NotZero(Expression<Func<T, int>> selector);
    TBuilder NotZero(Expression<Func<T, long>> selector);
    TBuilder NotZero(Expression<Func<T, float>> selector);
    TBuilder NotZero(Expression<Func<T, double>> selector);
    TBuilder NotZero(Expression<Func<T, decimal>> selector);
    TBuilder NotZero(Expression<Func<T, short>> selector);


    TBuilder InRange(Expression<Func<T, int>> selector, int min, int max);
    TBuilder InRange(Expression<Func<T, long>> selector, long min, long max);
    TBuilder InRange(Expression<Func<T, float>> selector, float min, float max);
    TBuilder InRange(Expression<Func<T, double>> selector, double min, double max);
    TBuilder InRange(Expression<Func<T, decimal>> selector, decimal min, decimal max);
    TBuilder InRange(Expression<Func<T, short>> selector, short min, short max);

    TBuilder InRange(Expression<Func<T, int>> selector, Expression<Func<T, int>> minSelector,
        Expression<Func<T, int>> maxSelector);

    TBuilder InRange(Expression<Func<T, long>> selector, Expression<Func<T, long>> minSelector,
        Expression<Func<T, long>> maxSelector);

    TBuilder InRange(Expression<Func<T, float>> selector, Expression<Func<T, float>> minSelector,
        Expression<Func<T, float>> maxSelector);

    TBuilder InRange(Expression<Func<T, double>> selector, Expression<Func<T, double>> minSelector,
        Expression<Func<T, double>> maxSelector);

    TBuilder InRange(Expression<Func<T, decimal>> selector, Expression<Func<T, decimal>> minSelector,
        Expression<Func<T, decimal>> maxSelector);

    TBuilder InRange(Expression<Func<T, short>> selector, Expression<Func<T, short>> minSelector,
        Expression<Func<T, short>> maxSelector);


    TBuilder GreaterThan(Expression<Func<T, int>> selector, int value);
    TBuilder GreaterThan(Expression<Func<T, long>> selector, long value);
    TBuilder GreaterThan(Expression<Func<T, float>> selector, float value);
    TBuilder GreaterThan(Expression<Func<T, double>> selector, double value);
    TBuilder GreaterThan(Expression<Func<T, decimal>> selector, decimal value);
    TBuilder GreaterThan(Expression<Func<T, short>> selector, short value);

    TBuilder GreaterThanOrEqualTo(Expression<Func<T, int>> selector, int value);
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, long>> selector, long value);
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, float>> selector, float value);
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, double>> selector, double value);
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value);
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, short>> selector, short value);

    TBuilder LessThan(Expression<Func<T, int>> selector, int value);
    TBuilder LessThan(Expression<Func<T, long>> selector, long value);
    TBuilder LessThan(Expression<Func<T, float>> selector, float value);
    TBuilder LessThan(Expression<Func<T, double>> selector, double value);
    TBuilder LessThan(Expression<Func<T, decimal>> selector, decimal value);
    TBuilder LessThan(Expression<Func<T, short>> selector, short value);

    TBuilder LessThanOrEqualTo(Expression<Func<T, int>> selector, int value);
    TBuilder LessThanOrEqualTo(Expression<Func<T, long>> selector, long value);
    TBuilder LessThanOrEqualTo(Expression<Func<T, float>> selector, float value);
    TBuilder LessThanOrEqualTo(Expression<Func<T, double>> selector, double value);
    TBuilder LessThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value);
    TBuilder LessThanOrEqualTo(Expression<Func<T, short>> selector, short value);

    TBuilder Positive(Expression<Func<T, int>> selector);
    TBuilder Positive(Expression<Func<T, long>> selector);
    TBuilder Positive(Expression<Func<T, float>> selector);
    TBuilder Positive(Expression<Func<T, double>> selector);
    TBuilder Positive(Expression<Func<T, decimal>> selector);
    TBuilder Positive(Expression<Func<T, short>> selector);

    TBuilder Negative(Expression<Func<T, int>> selector);
    TBuilder Negative(Expression<Func<T, long>> selector);
    TBuilder Negative(Expression<Func<T, float>> selector);
    TBuilder Negative(Expression<Func<T, double>> selector);
    TBuilder Negative(Expression<Func<T, decimal>> selector);
    TBuilder Negative(Expression<Func<T, short>> selector);

    TBuilder MinValue(Expression<Func<T, int>> selector, int minValue);
    TBuilder MinValue(Expression<Func<T, long>> selector, long minValue);
    TBuilder MinValue(Expression<Func<T, float>> selector, float minValue);
    TBuilder MinValue(Expression<Func<T, double>> selector, double minValue);
    TBuilder MinValue(Expression<Func<T, decimal>> selector, decimal minValue);
    TBuilder MinValue(Expression<Func<T, short>> selector, short minValue);

    TBuilder MaxValue(Expression<Func<T, int>> selector, int maxValue);
    TBuilder MaxValue(Expression<Func<T, long>> selector, long maxValue);
    TBuilder MaxValue(Expression<Func<T, float>> selector, float maxValue);
    TBuilder MaxValue(Expression<Func<T, double>> selector, double maxValue);
    TBuilder MaxValue(Expression<Func<T, decimal>> selector, decimal maxValue);
    TBuilder MaxValue(Expression<Func<T, short>> selector, short maxValue);
}