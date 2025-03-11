using System.Linq.Expressions;

namespace Vali_Flow.Interfaces.Evaluators.Write;

/// <summary>
/// Defines asynchronous methods for performing write operations (add, update, delete, upsert) on entities using ValiFlow with Entity Framework support.
/// </summary>
/// <typeparam name="T">The type of the entities to manipulate.</typeparam>
public interface IEvaluatorWrite<T>
{
    /// <summary>
    /// Inserts a single entity into the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the inserted entity.</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple entities into the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the collection of inserted entities.</returns>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update. The entity must exist in the database for the update to succeed.</param>
    /// <returns>A task that represents the asynchronous operation, returning the updated entity.</returns>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Updates multiple existing entities in the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to update. Entities must exist in the database for updates to succeed.</param>
    /// <returns>A task that represents the asynchronous operation, returning the collection of updated entities.</returns>
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Deletes a single entity from the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to delete. The entity must exist in the database for the deletion to succeed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Deletes multiple entities from the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to delete. Entities must exist in the database for deletions to succeed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Saves all pending changes to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an upsert operation (insert or update) on a single entity based on a matching condition.
    /// </summary>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="matchCondition">An expression defining the condition to identify an existing entity for updating. If no match is found, the entity is inserted.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the upserted entity.</returns>
    Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an upsert operation (insert or update) on multiple entities based on a key selector.
    /// </summary>
    /// <typeparam name="TProperty">The type of the key used for matching entities.</typeparam>
    /// <param name="entities">The collection of entities to upsert.</param>
    /// <param name="keySelector">A function to extract the key for identifying existing entities. If no match is found for an entity, it is inserted.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the collection of upsert entities.</returns>
    Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Func<T, TProperty> keySelector,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull;

    /// <summary>
    /// Deletes entities from the database that match the specified condition asynchronously.
    /// </summary>
    /// <param name="condition">An expression defining the condition for entities to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteByConditionAsync(Expression<Func<T, bool>> condition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a set of database operations within a transaction asynchronously.
    /// If any operation fails, the transaction is rolled back.
    /// </summary>
    /// <param name="operations">A function containing the operations to execute within the transaction.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default);
}