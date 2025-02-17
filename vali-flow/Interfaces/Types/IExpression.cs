using System.Linq.Expressions;

namespace vali_flow.Interfaces.Types;

/// <summary>
/// Defines operations for building, adding conditions, and evaluating logical expressions for an entity of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="TBuilder">The specific builder type that implements this interface.</typeparam>
/// <typeparam name="T">The type of the entity being evaluated.</typeparam>
public interface IExpression<out TBuilder,T>
{
    /// <summary>
    /// Builds a boolean expression from the added conditions.
    /// </summary>
    /// <returns>A boolean expression representing the evaluation of the added conditions.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18).Add(x => x.Name == "John");
    /// var expression = builder.Build(); // The resulting expression represents the condition (Age > 18) AND (Name == "John")
    /// </code>
    /// </example>
    Expression<Func<T, bool>> Build();

    /// <summary>
    /// Adds a condition to the list of conditions based on a boolean expression.
    /// </summary>
    /// <param name="expression">The expression to add, represented as a boolean expression.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <exception cref="ArgumentNullException">Throws an exception if the condition is null.</exception>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18); // Adds a condition to check if the age is greater than 18
    /// </code>
    /// </example>
    TBuilder Add(Expression<Func<T, bool>> expression);

    /// <summary>
    /// Adds a condition to the list of conditions based on a specific property of the entity of type <typeparamref name="T"/>
    /// and a predicate that evaluates a value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to be evaluated.</typeparam>
    /// <param name="selector">Expression to select the attribute to work with</param>
    /// <param name="predicate">A boolean expression that evaluates the value selected by the <paramref name="predicate"/>.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <exception cref="ArgumentNullException">Throws an exception if any parameter is null.</exception>
    /// <example>
    /// <code>
    /// builder.Add(x => x.Age, age => age >= 18);
    /// </code>
    /// </example>
    TBuilder Add<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<TValue, bool>> predicate);

    /// <summary>
    /// Adds a group of conditions that will be evaluated as a sub-expression.
    /// This group can be combined with other logical conditions such as AND or OR.
    /// </summary>
    /// <param name="group">The action that builds the group of conditions.</param>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.AddSubGroup(group => group.Add(x => x.Age > 18).Add(x => x.Name == "John"));
    /// // This example adds a subgroup with conditions (Age > 18) AND (Name == "John")
    /// </code>
    /// </example>
    TBuilder AddSubGroup(Action<TBuilder> group);

    /// <summary>
    /// Evaluates a specific condition on an object of type <typeparamref name="T"/>.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entity">The object to evaluate.</param>
    /// <returns>The boolean result of evaluating the conditions.</returns>
    /// <exception cref="InvalidOperationException">Throws an exception if there is an error evaluating the conditions.</exception>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18);
    /// bool result = builder.Evaluate(new Person { Age = 25 }); // Returns true
    /// </code>
    /// </example>
    public bool Evaluate(T entity);
    
    /// <summary>
    /// Evaluates whether all entities in the collection satisfy the defined conditions.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <returns>True if all entities meet the conditions; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public bool Evaluate(IEnumerable<T> entities);
    
    /// <summary>
    /// Evaluates whether at least one entity in the collection satisfies the defined conditions.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <returns>True if any entity meets the conditions; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public bool EvaluateAny(IEnumerable<T> entities);
    
    /// <summary>
    /// Counts the number of entities in the collection that satisfy the defined conditions.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <returns>The number of entities that meet the conditions.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public int EvaluateCount(IEnumerable<T> entities);
    
    /// <summary>
    /// Retrieves the first entity in the collection that fails to meet the defined conditions.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <returns>The first entity that does not meet the conditions, or null if all satisfy them.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public T? GetFirstFailed(IEnumerable<T> entities);
    
    /// <summary>
    /// Retrieves the first entity in the collection that satisfies the defined conditions.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <returns>The first entity that meets the conditions, or null if none satisfy them.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public T? GetFirst(IEnumerable<T> entities);
    
    /// <summary>
    /// Retrieves all entities in the collection that fail to meet the defined conditions.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <returns>A collection of entities that do not meet the conditions.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is null or empty.</exception>
    public IEnumerable<T> EvaluateAllFailed(IEnumerable<T> entities);
    
    /// <summary>
    /// Evaluates a condition on each element of a collection of objects of type <typeparamref name="T"/>.
    /// This method is intended for in-memory use.
    /// </summary>
    /// <param name="entities">The collection of elements to evaluate.</param>
    /// <returns>A list of elements from the collection that meet the specified conditions.</returns>
    /// <exception cref="ArgumentException">Throws an exception if the collection is null or empty.</exception>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18);
    /// var result = builder.EvaluateAll(new List { new Person { Age = 25 }, new Person { Age = 15 } });
    /// // Returns a list containing only the person with age 25
    /// </code>
    /// </example>
    public IEnumerable<T> EvaluateAll(IEnumerable<T> entities);

    /// <summary>
    /// Evaluates the conditions on a collection of entities in memory and returns a paginated subset.
    /// </summary>
    /// <typeparam name="T">The type of the entities in the collection.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="page">The page number (1-based index).</param>
    /// <param name="pageSize">The number of entities per page.</param>
    /// <returns>A paginated collection of entities that satisfy the conditions.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the collection is null or empty, or if page or pageSize are less than 1.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is an error evaluating the conditions.
    /// </exception>
    public IEnumerable<T> EvaluatePaged(IEnumerable<T> entities, int page, int pageSize);
    
    /// <summary>
    /// Defines a logical "AND" operation between conditions.
    /// </summary>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18).And().Add(x => x.Name == "John");
    /// // The resulting expression will be (Age > 18) AND (Name == "John")
    /// </code>
    /// </example>  
    TBuilder And();

    /// <summary>
    /// Defines a logical "OR" operation between conditions.
    /// </summary>
    /// <returns>The current builder to allow method chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18).Or().Add(x => x.Name == "John");
    /// // The resulting expression will be (Age > 18) OR (Name == "John")
    /// </code>
    /// </example>
    TBuilder Or();
}