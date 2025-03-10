using System.Numerics;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;

namespace Vali_Flow.InMemory.Interfaces.Evaluators.Read;

/// <summary>
/// Defines methods for evaluating and querying entities in memory using ValiFlow conditions.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IInMemoryEvaluatorRead<T>
{
    /// <summary>
    /// Evaluates whether a single entity satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates to true.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>True if the entity satisfies the condition; otherwise, false.</returns>
    bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false);
    
    /// <summary>
    /// Determines whether any entity in the collection satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates to true for non-empty collections.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>True if at least one entity satisfies the condition; otherwise, false.</returns>
    bool EvaluateAny(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false);
    
    /// <summary>
    /// Counts the number of entities in the collection that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, counts all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The number of entities that satisfy the condition.</returns>
    int EvaluateCount(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false);
    
    /// <summary>
    /// Retrieves the first entity that does not satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the first entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The first entity that fails the condition, or null if none fail.</returns>
    T? GetFirstFailed(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false);
    
    /// <summary>
    /// Retrieves the first entity that satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The ValiFlow condition to apply. If null, returns the first entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The first entity that satisfies the condition, or null if none satisfy.</returns>
    T? GetFirst(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false);

    /// <summary>
    /// Retrieves all entities that do not satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities as failing.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>An enumerable of entities that fail the condition, ordered as specified.</returns>
    IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves all entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>An enumerable of entities that satisfy the condition, ordered as specified.</returns>
    IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves a paginated subset of entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of entities per page.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>An enumerable of entities that satisfy the condition, paginated and ordered as specified.</returns>
    IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T> entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves the top N entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="count">The maximum number of entities to return.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the top N entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>An enumerable of the top N entities that satisfy the condition, ordered as specified.</returns>
    IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T> entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves distinct entities based on a selector, with optional ordering, that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for selection and ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the key for determining distinctness.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>An enumerable of distinct entities that satisfy the condition, ordered as specified.</returns>
    IEnumerable<T> EvaluateDistinct<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves entities that have duplicate values based on a selector, with optional ordering, that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for selection and ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the key for determining duplicates.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>An enumerable of entities with duplicate keys that satisfy the condition, ordered as specified.</returns>
    IEnumerable<T> EvaluateDuplicates<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves the index of the first entity that satisfies the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the index of the first entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The index of the first entity that satisfies the condition, or -1 if none satisfy.</returns>
    int GetFirstMatchIndex<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves the index of the last entity that satisfies the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the index of the last entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The index of the last entity that satisfies the condition, or -1 if none satisfy.</returns>
    int GetLastMatchIndex<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves the last entity that does not satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the last entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The last entity that fails the condition, or null if none fail.</returns>
    T? GetLastFailed<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves the last entity that satisfies the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the last entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The last entity that satisfies the condition, or null if none satisfy.</returns>
    T? GetLast<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Computes the minimum value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The minimum value of the selected property for entities that satisfy the condition.</returns>
    TResult EvaluateMin<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Computes the maximum value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The maximum value of the selected property for entities that satisfy the condition.</returns>
    TResult EvaluateMax<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Computes the average value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The average value of the selected property for entities that satisfy the condition, as a decimal.</returns>
    decimal EvaluateAverage<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Computes the sum of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to sum.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The sum of the selected property values for entities that satisfy the condition.</returns>
    TResult EvaluateSum<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Applies a custom aggregation to a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to aggregate.</param>
    /// <param name="aggregator">A function that defines how to aggregate two values into a single result.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The aggregated result of the selected property values for entities that satisfy the condition.</returns>
    TResult EvaluateAggregate<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;
    
    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is a list of entities in that group.</returns>
    Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and counts the entities in each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is the count of entities in that group.</returns>
    Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull;
    
    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and computes the sum of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="selector">A function to extract the property value to sum.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is the sum of the selected property for that group.</returns>
    Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and computes the minimum value of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is the minimum of the selected property for that group.</returns>
    Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult> where TKey : notnull;
    
    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and computes the maximum value of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is the maximum of the selected property for that group.</returns>
    Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult> where TKey : notnull;
    
    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and computes the average value of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="selector">A function to extract the property value to average.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is the average of the selected property for that group, as a decimal.</returns>
    Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult> where TKey : notnull;
    
    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and returns groups with more than one element (duplicates).
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is a list of entities with duplicate keys.</returns>
    Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and returns groups with exactly one element (uniques).
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is the single entity in that group.</returns>
    Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities that satisfy the specified Vali-Flow condition by a key and returns the top N entities for each group, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="keySelector">A function to extract the grouping key from each entity.</param>
    /// <param name="count">The maximum number of entities to return per group.</param>
    /// <param name="orderBy">A function to extract the key for ordering within each group. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A dictionary where each key is a grouping key and the value is a list of the top N entities in that group, ordered as specified.</returns>
    Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull;
}