using System.Linq.Expressions;

namespace Vali_Flow.Core.Interfaces.Types;

/// <summary>
/// Defines fluent builder methods for constructing ValiFlow conditions based on numeric property evaluations.
/// </summary>
/// <typeparam name="T">The type of the entity being evaluated.</typeparam>
/// <typeparam name="TBuilder">The type of the builder to enable method chaining.</typeparam>
public interface INumericExpression<out TBuilder, T>
{
    /// <summary>
    /// Adds a condition to check if the selected integer property is zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Zero(Expression<Func<T, int>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Zero(Expression<Func<T, long>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Zero(Expression<Func<T, float>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Zero(Expression<Func<T, double>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Zero(Expression<Func<T, decimal>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Zero(Expression<Func<T, short>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is not zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder NotZero(Expression<Func<T, int>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is not zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder NotZero(Expression<Func<T, long>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is not zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder NotZero(Expression<Func<T, float>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is not zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder NotZero(Expression<Func<T, double>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is not zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder NotZero(Expression<Func<T, decimal>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is not zero.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder NotZero(Expression<Func<T, short>> selector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the specified range (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, int>> selector, int min, int max);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the specified range (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, long>> selector, long min, long max);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the specified range (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, float>> selector, float min, float max);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the specified range (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, double>> selector, double min, double max);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the specified range (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, decimal>> selector, decimal min, decimal max);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the specified range (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, short>> selector, short min, short max);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the range defined by the minimum and maximum values of other properties (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="minSelector">An expression to select the property providing the minimum value of the range.</param>
    /// <param name="maxSelector">An expression to select the property providing the maximum value of the range.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, int>> selector, Expression<Func<T, int>> minSelector,
        Expression<Func<T, int>> maxSelector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the range defined by the minimum and maximum values of other properties (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="minSelector">An expression to select the property providing the minimum value of the range.</param>
    /// <param name="maxSelector">An expression to select the property providing the maximum value of the range.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, long>> selector, Expression<Func<T, long>> minSelector,
        Expression<Func<T, long>> maxSelector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the range defined by the minimum and maximum values of other properties (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="minSelector">An expression to select the property providing the minimum value of the range.</param>
    /// <param name="maxSelector">An expression to select the property providing the maximum value of the range.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, float>> selector, Expression<Func<T, float>> minSelector,
        Expression<Func<T, float>> maxSelector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the range defined by the minimum and maximum values of other properties (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="minSelector">An expression to select the property providing the minimum value of the range.</param>
    /// <param name="maxSelector">An expression to select the property providing the maximum value of the range.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, double>> selector, Expression<Func<T, double>> minSelector,
        Expression<Func<T, double>> maxSelector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the range defined by the minimum and maximum values of other properties (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="minSelector">An expression to select the property providing the minimum value of the range.</param>
    /// <param name="maxSelector">An expression to select the property providing the maximum value of the range.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, decimal>> selector, Expression<Func<T, decimal>> minSelector,
        Expression<Func<T, decimal>> maxSelector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is within the range defined by the minimum and maximum values of other properties (inclusive).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="minSelector">An expression to select the property providing the minimum value of the range.</param>
    /// <param name="maxSelector">An expression to select the property providing the maximum value of the range.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder InRange(Expression<Func<T, short>> selector, Expression<Func<T, short>> minSelector,
        Expression<Func<T, short>> maxSelector);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThan(Expression<Func<T, int>> selector, int value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThan(Expression<Func<T, long>> selector, long value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThan(Expression<Func<T, float>> selector, float value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThan(Expression<Func<T, double>> selector, double value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThan(Expression<Func<T, decimal>> selector, decimal value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThan(Expression<Func<T, short>> selector, short value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, int>> selector, int value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, long>> selector, long value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, float>> selector, float value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, double>> selector, double value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder GreaterThanOrEqualTo(Expression<Func<T, short>> selector, short value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is less than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThan(Expression<Func<T, int>> selector, int value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThan(Expression<Func<T, long>> selector, long value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThan(Expression<Func<T, float>> selector, float value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThan(Expression<Func<T, double>> selector, double value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThan(Expression<Func<T, decimal>> selector, decimal value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThan(Expression<Func<T, short>> selector, short value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThanOrEqualTo(Expression<Func<T, int>> selector, int value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThanOrEqualTo(Expression<Func<T, long>> selector, long value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThanOrEqualTo(Expression<Func<T, float>> selector, float value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThanOrEqualTo(Expression<Func<T, double>> selector, double value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder LessThanOrEqualTo(Expression<Func<T, short>> selector, short value);

    /// <summary>
    /// Adds a condition to check if the selected integer property is positive (greater than zero).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Positive(Expression<Func<T, int>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is positive (greater than zero).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Positive(Expression<Func<T, long>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is positive (greater than zero).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Positive(Expression<Func<T, float>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is positive (greater than zero).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Positive(Expression<Func<T, double>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is positive (greater than zero).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Positive(Expression<Func<T, decimal>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is positive (greater than zero).
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Positive(Expression<Func<T, short>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected decimal property is negative (less than zero).
    /// </summary>
    /// <param name="selector">An expression to select the decimal property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Negative(Expression<Func<T, int>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected decimal property is negative (less than zero).
    /// </summary>
    /// <param name="selector">An expression to select the decimal property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Negative(Expression<Func<T, long>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected decimal property is negative (less than zero).
    /// </summary>
    /// <param name="selector">An expression to select the decimal property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Negative(Expression<Func<T, float>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected decimal property is negative (less than zero).
    /// </summary>
    /// <param name="selector">An expression to select the decimal property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Negative(Expression<Func<T, double>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected decimal property is negative (less than zero).
    /// </summary>
    /// <param name="selector">An expression to select the decimal property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Negative(Expression<Func<T, decimal>> selector);
    
    /// <summary>
    /// Adds a condition to check if the selected decimal property is negative (less than zero).
    /// </summary>
    /// <param name="selector">An expression to select the decimal property to evaluate.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder Negative(Expression<Func<T, short>> selector);

    /// <summary>
    /// Adds a condition to check if the selected float property is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="selector">An expression to select the float property to evaluate.</param>
    /// <param name="minValue">The minimum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MinValue(Expression<Func<T, int>> selector, int minValue);
    
    /// <summary>
    /// Adds a condition to check if the selected float property is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="selector">An expression to select the float property to evaluate.</param>
    /// <param name="minValue">The minimum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MinValue(Expression<Func<T, long>> selector, long minValue);
    
    /// <summary>
    /// Adds a condition to check if the selected float property is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="selector">An expression to select the float property to evaluate.</param>
    /// <param name="minValue">The minimum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MinValue(Expression<Func<T, float>> selector, float minValue);
    
    /// <summary>
    /// Adds a condition to check if the selected float property is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="selector">An expression to select the float property to evaluate.</param>
    /// <param name="minValue">The minimum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MinValue(Expression<Func<T, double>> selector, double minValue);
    
    /// <summary>
    /// Adds a condition to check if the selected float property is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="selector">An expression to select the float property to evaluate.</param>
    /// <param name="minValue">The minimum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MinValue(Expression<Func<T, decimal>> selector, decimal minValue);
    
    /// <summary>
    /// Adds a condition to check if the selected float property is greater than or equal to the specified minimum value.
    /// </summary>
    /// <param name="selector">An expression to select the float property to evaluate.</param>
    /// <param name="minValue">The minimum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MinValue(Expression<Func<T, short>> selector, short minValue);

    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="maxValue">The maximum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MaxValue(Expression<Func<T, int>> selector, int maxValue);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="maxValue">The maximum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MaxValue(Expression<Func<T, long>> selector, long maxValue);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="maxValue">The maximum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MaxValue(Expression<Func<T, float>> selector, float maxValue);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="maxValue">The maximum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MaxValue(Expression<Func<T, double>> selector, double maxValue);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="maxValue">The maximum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MaxValue(Expression<Func<T, decimal>> selector, decimal maxValue);
    
    /// <summary>
    /// Adds a condition to check if the selected integer property is less than or equal to the specified maximum value.
    /// </summary>
    /// <param name="selector">An expression to select the integer property to evaluate.</param>
    /// <param name="maxValue">The maximum value to compare against.</param>
    /// <returns>The builder instance to enable method chaining.</returns>
    TBuilder MaxValue(Expression<Func<T, short>> selector, short maxValue);
}