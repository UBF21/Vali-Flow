using System.Linq.Expressions;

namespace ValiFlow.Core.Interfaces.Types;

/// <summary>
/// Defines a set of methods for building expressions that evaluate conditions on collections.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder implementing this interface, allowing method chaining.</typeparam>
/// <typeparam name="T">The type of the entity being evaluated.</typeparam>
public interface ICollectionExpression<out TBuilder, T>
{
     /// <summary>
    /// Ensures that the selected collection is not empty.
    /// </summary>
    /// <param name="selector">An expression that selects the collection from the entity.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.NotEmpty(x => x.Orders);
    /// </code>
    /// </example>
    TBuilder NotEmpty<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector);
    
    /// <summary>
    /// Checks if the selected property value is within the specified set of values.
    /// </summary>
    /// <typeparam name="TValue">The type of the property being evaluated.</typeparam>
    /// <param name="selector">An expression selecting the property from the entity.</param>
    /// <param name="values">The collection of values to compare against.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.In(x => x.Status, new[] { "Active", "Pending" });
    /// </code>
    /// </example>
    TBuilder In<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values);
    
    /// <summary>
    /// Checks if the selected property value is not within the specified set of values.
    /// </summary>
    /// <typeparam name="TValue">The type of the property being evaluated.</typeparam>
    /// <param name="selector">An expression selecting the property from the entity.</param>
    /// <param name="values">The collection of values to exclude.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.NotIn(x => x.Status, new[] { "Inactive", "Deleted" });
    /// </code>
    /// </example>
    TBuilder NotIn<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values);
    
    /// <summary>
    /// Ensures that the selected collection contains exactly the specified number of elements.
    /// </summary>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="count">The exact number of elements required.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Count(x => x.Orders, 3);
    /// </code>
    /// </example>
    TBuilder Count<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector, int count);
    
    /// <summary>
    /// Ensures that the selected collection contains a number of elements within the specified range.
    /// </summary>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="min">The minimum number of elements required.</param>
    /// <param name="max">The maximum number of elements allowed.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.CountBetween(x => x.Orders, 1, 5);
    /// </code>
    /// </example>
    TBuilder CountBetween<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, int min, int max);
    
    /// <summary>
    /// Ensures that all elements in the selected collection satisfy the given condition.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the collection.</typeparam>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="predicate">A condition that each element must satisfy.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.All(x => x.Orders, order => order.Amount > 100);
    /// </code>
    /// </example>
    TBuilder All<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, Expression<Func<TValue, bool>> predicate);
    
    /// <summary>
    /// Ensures that at least one element in the selected collection satisfies the given condition.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the collection.</typeparam>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="predicate">A condition that at least one element must satisfy.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Any(x => x.Orders, order => order.Status == "Pending");
    /// </code>
    /// </example>
    TBuilder Any<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, Expression<Func<TValue, bool>> predicate);
    
    /// <summary>
    /// Ensures that the selected collection contains the specified value.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the collection.</typeparam>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="value">The value that must be present in the collection.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Contains(x => x.Tags, "Important");
    /// </code>
    /// </example>
    TBuilder Contains<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, TValue value);
    
    /// <summary>
    /// Ensures that the selected collection contains a distinct number of elements equal to the specified count.
    /// </summary>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="count">The exact number of distinct elements required.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.DistinctCount(x => x.Categories, 3);
    /// </code>
    /// </example>
    TBuilder DistinctCount<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector, int count);
    
    /// <summary>
    /// Ensures that none of the elements in the selected collection satisfy the given condition.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the collection.</typeparam>
    /// <param name="selector">An expression selecting the collection from the entity.</param>
    /// <param name="predicate">A condition that no element must satisfy.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.None(x => x.Orders, order => order.Status == "Cancelled");
    /// </code>
    /// </example>
    TBuilder None<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, Expression<Func<TValue, bool>> predicate);
    
}