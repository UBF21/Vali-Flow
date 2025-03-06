namespace Vali_Flow.Core.Interfaces.Types;

/// <summary>
/// Defines boolean expression operations for validation.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder implementing the interface.</typeparam>
/// <typeparam name="T">The type of the object being validated.</typeparam>
public interface IBooleanExpression<out TBuilder,T>
{
    /// <summary>
    /// Validates that the specified condition evaluates to true.
    /// </summary>
    /// <param name="selector">A function that selects a boolean value from the object.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsTrue(Func<T, bool> selector);
    
    /// <summary>
    /// Validates that the specified condition evaluates to false.
    /// </summary>
    /// <param name="selector">A function that selects a boolean value from the object.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsFalse(Func<T, bool> selector);
}