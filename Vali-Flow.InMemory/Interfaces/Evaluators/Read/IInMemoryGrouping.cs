using System.Numerics;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.InMemory.Interfaces.Evaluators.Read;

/// <summary>
/// Defines grouping and group-aggregation methods over in-memory entity collections.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IInMemoryGrouping<T> where T : class
{
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
    Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey, TOrderKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, TOrderKey>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull;
}
