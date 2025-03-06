using System.Linq.Expressions;

namespace Vali_Flow.Core.Interfaces.General;

/// <summary>
/// Defines comparison expressions for validating object properties.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder that supports method chaining.</typeparam>
/// <typeparam name="T">The type of the entity being validated.</typeparam>
public interface IComparisonExpression<out TBuilder, T>
{
    /// <summary>
    /// Ensures that the selected property is not null.
    /// </summary>
    /// <param name="selector">Expression to select the property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder NotNull<TProperty>(Expression<Func<T, TProperty?>> selector);
    
    /// <summary>
    /// Ensures that the selected property is null.
    /// </summary>
    /// <param name="selector">Expression to select the property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder Null<TProperty>(Expression<Func<T, TProperty?>> selector);
    
    /// <summary>
    /// Ensures that the selected property is equal to the specified value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to compare.</typeparam>
    /// <param name="selector">Expression to select the property.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder EqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value) where TValue : IEquatable<TValue>;
    
    /// <summary>
    /// Ensures that the selected property is not equal to the specified value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to compare.</typeparam>
    /// <param name="selector">Expression to select the property.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder NotEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value) where TValue : IEquatable<TValue>;
}