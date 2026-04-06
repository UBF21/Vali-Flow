using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Models;

namespace Vali_Flow.InMemory.Interfaces.Evaluators.Read;

/// <summary>
/// Defines query methods that return collections of entities, with support for ordering, pagination, and projection.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IInMemoryQueryable<T> where T : class
{
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
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves all entities that do not satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities as failing.</param>
    /// <returns>An enumerable of entities that fail the condition, ordered as specified.</returns>
    IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
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
        IEnumerable<T>? entities = null,
        int page = 1,
        int pageSize = 10,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    );

    /// <summary>
    /// Retrieves a paginated result including metadata (total count, page info) for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of entities per page.</param>
    /// <param name="orderBy">A function to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">A collection of secondary ordering specifications.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>A <see cref="PagedResult{T}"/> containing the page items and metadata.</returns>
    PagedResult<T> EvaluatePagedResult<TKey>(
        IEnumerable<T>? entities,
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
        IEnumerable<T>? entities = null,
        int count = 10,
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
        IEnumerable<T>? entities = null,
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
        IEnumerable<T>? entities = null,
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
        IEnumerable<T>? entities = null,
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
    /// <returns>The last entity that fails the condition, or null if none fail.</returns>
    T? GetLastFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
    );
}
