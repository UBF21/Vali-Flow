using System.Linq.Expressions;

namespace Vali_Flow.Interfaces.Evaluators.Write;

/// <summary>
/// Defines asynchronous methods for performing write operations (add, update, delete, upsert) on entities using ValiFlow with Entity Framework support.
/// </summary>
/// <typeparam name="T">The type of the entities to manipulate.</typeparam>
public interface IEvaluatorWrite<T>
{
    /// <summary>
    /// Adds a single entity to the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>The added entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default, bool saveChanges = true);

    /// <summary>
    /// Adds a collection of entities to the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>The collection of added entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default,
        bool saveChanges = true);

    /// <summary>
    /// Updates a single entity in the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<T> UpdateAsync(T entity, bool saveChanges = true);

    /// <summary>
    /// Updates a collection of entities in the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>The collection of updated entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities, bool saveChanges = true);

    /// <summary>
    /// Deletes a single entity from the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task DeleteAsync(T entity, bool saveChanges = true);

    /// <summary>
    /// Deletes a collection of entities from the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to delete.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task DeleteRangeAsync(IEnumerable<T> entities,bool saveChanges = true);

    /// <summary>
    /// Saves all changes made in the context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a single entity based on a matching condition asynchronously.
    /// </summary>
    /// <param name="entity">The entity to insert or update.</param>
    /// <param name="matchCondition">An expression to identify an existing entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>The inserted or updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> or <paramref name="matchCondition"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        CancellationToken cancellationToken = default,
        bool saveChanges = true);

    /// <summary>
    /// Inserts or updates a collection of entities based on a key selector asynchronously.
    /// </summary>
    /// <typeparam name="TProperty">The type of the key property. Must not be null.</typeparam>
    /// <param name="entities">The collection of entities to insert or update.</param>
    /// <param name="keySelector">A function to select the key property for matching entities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>The collection of inserted or updated entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> or <paramref name="keySelector"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Func<T, TProperty> keySelector,
        CancellationToken cancellationToken = default,
        bool saveChanges = true
    ) where TProperty : notnull;

    /// <summary>
    /// Deletes entities that match a specified condition from the database asynchronously.
    /// </summary>
    /// <param name="condition">An expression to identify entities to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="condition"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task DeleteByConditionAsync(Expression<Func<T, bool>> condition, CancellationToken cancellationToken = default, bool saveChanges = true);

    /// <summary>
    /// Executes a set of database operations within a transaction asynchronously.
    /// </summary>
    /// <param name="operations">The operations to execute within the transaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the transaction fails or rollback fails.</exception>
    Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default);
}