using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

public sealed partial class ValiFlowEvaluator<T>
{
    /// <summary>
    /// Adds a single entity to the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and optionally persists it immediately.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tracked entity after it has been added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public async Task<T> AddAsync(
        T entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);

        var addedEntity = await ExecuteWithExceptionHandlingAsync(
            async () => (await _dbContext.Set<T>().AddAsync(entity, cancellationToken)).Entity,
            nameof(AddAsync));

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(AddAsync));
        return addedEntity;
    }

    /// <summary>
    /// Adds a collection of entities to the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and optionally persists them immediately.
    /// </summary>
    /// <param name="entities">The entities to add. Must be non-null and non-empty.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection of added entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task<IEnumerable<T>> AddRangeAsync(
        IEnumerable<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.Set<T>().AddRangeAsync(entityList, cancellationToken);
                return entityList;
            }, nameof(AddRangeAsync));

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(AddRangeAsync));
        return entityList;
    }

    /// <summary>
    /// Marks a single entity as modified in the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and optionally persists the changes immediately.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public async Task<T> UpdateAsync(
        T entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);
        _dbContext.Set<T>().Update(entity);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpdateAsync));
        return entity;
    }

    /// <summary>
    /// Marks a collection of entities as modified in the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and optionally persists the changes immediately.
    /// </summary>
    /// <param name="entities">The entities to update. Must be non-null and non-empty.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection of updated entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task<IEnumerable<T>> UpdateRangeAsync(
        IEnumerable<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        _dbContext.Set<T>().UpdateRange(entityList);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpdateRangeAsync));
        return entityList;
    }

    /// <summary>
    /// Marks a single entity for removal in the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and optionally persists the deletion immediately.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public async Task DeleteAsync(
        T entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);
        _dbContext.Set<T>().Remove(entity);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(DeleteAsync));
    }

    /// <summary>
    /// Marks a collection of entities for removal in the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and optionally persists the deletions immediately.
    /// </summary>
    /// <param name="entities">The entities to delete. Must be non-null and non-empty.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task DeleteRangeAsync(
        IEnumerable<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        _dbContext.Set<T>().RemoveRange(entityList);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(DeleteRangeAsync));
    }

    /// <summary>
    /// Persists all pending changes tracked by the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> to the database.
    /// Use this method when batching multiple write operations with <c>saveChanges: false</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteWithExceptionHandlingAsync(() => _dbContext.SaveChangesAsync(cancellationToken),
            nameof(SaveChangesAsync));
    }

    /// <summary>
    /// Inserts <paramref name="entity"/> if no existing entity satisfies <paramref name="matchCondition"/>;
    /// otherwise updates the matched entity's scalar values in place.
    /// </summary>
    /// <param name="entity">The entity to insert or use as the value source for an update.</param>
    /// <param name="matchCondition">Predicate used to locate an existing entity to update.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted or updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="matchCondition"/> is <c>null</c>.</exception>
    public async Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);
        if (matchCondition == null) throw new ArgumentNullException(nameof(matchCondition));
        T? existingEntity = await _dbContext.Set<T>().FirstOrDefaultAsync(matchCondition, cancellationToken);

        if (existingEntity == null)
        {
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        }

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpsertAsync));
        return entity;
    }

    /// <summary>
    /// Inserts or updates a collection of entities based on a key property.
    /// Entities whose key already exists in the database are updated in place; the rest are inserted.
    /// A single database round-trip loads all existing keys via an EF-translatable <c>Contains</c> predicate.
    /// </summary>
    /// <typeparam name="TProperty">Type of the key property used for matching.</typeparam>
    /// <param name="entities">The entities to upsert. Must be non-null and non-empty.</param>
    /// <param name="keySelector">Expression that projects the key property used to identify existing entities.</param>
    /// <param name="saveChanges">When <c>true</c>, calls <c>SaveChangesAsync</c> before returning. Default is <c>true</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full collection of upserted entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> or <paramref name="keySelector"/> is <c>null</c>.</exception>
    public async Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Expression<Func<T, TProperty>> keySelector,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        // Compile keySelector once for in-memory use
        var keySelectorFn = keySelector.Compile();

        // Materialize keys as a concrete List<TProperty> — EF can translate .Contains() on a local collection
        List<TProperty> keys = entityList.Select(keySelectorFn).ToList();

        // Build a predicate that EF can translate: e => keys.Contains(e.Prop)
        var param = keySelector.Parameters[0];
        var body = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TProperty)],
            Expression.Constant(keys),
            keySelector.Body
        );
        var predicate = Expression.Lambda<Func<T, bool>>(body, param);

        IEnumerable<T> existingEntities = await _dbContext.Set<T>()
            .Where(predicate)
            .ToListAsync(cancellationToken);

        Dictionary<TProperty, T> existingEntityDict = existingEntities.ToDictionary(keySelectorFn, e => e);

        var newEntities = new List<T>();

        foreach (T entity in entityList)
        {
            TProperty key = keySelectorFn(entity);
            if (existingEntityDict.TryGetValue(key, out var existingEntity))
            {
                _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                newEntities.Add(entity);
            }
        }

        if (newEntities.Any())
            await _dbContext.Set<T>().AddRangeAsync(newEntities, cancellationToken);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpsertRangeAsync));
        return entityList;
    }

    /// <summary>
    /// Deletes all entities that satisfy <paramref name="condition"/> without loading them into memory.
    /// Uses <c>ExecuteDeleteAsync</c> for relational providers and falls back to a load-then-remove pattern
    /// for the EF Core in-memory provider.
    /// </summary>
    /// <param name="condition">Predicate that selects the entities to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="condition"/> is <c>null</c>.</exception>
    public async Task DeleteByConditionAsync(
        Expression<Func<T, bool>> condition,
        CancellationToken cancellationToken = default
    )
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        await ExecuteWithExceptionHandlingAsync(async () =>
            {
                bool isInMemory = _dbContext.Database.ProviderName
                    ?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;

                if (!isInMemory)
                {
                    // ExecuteDeleteAsync translates directly to DELETE FROM … WHERE — no round-trip to load entities
                    await _dbContext.Set<T>().Where(condition).ExecuteDeleteAsync(cancellationToken);
                }
                else
                {
                    // Fallback for providers that do not support ExecuteDeleteAsync (e.g., InMemory)
                    var entities = await _dbContext.Set<T>().Where(condition).ToListAsync(cancellationToken);
                    if (entities.Count > 0)
                    {
                        _dbContext.Set<T>().RemoveRange(entities);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                return 0;
            },
            nameof(DeleteByConditionAsync));
    }

    /// <summary>
    /// Updates all entities that satisfy <paramref name="condition"/> directly in the database using
    /// EF Core's <c>ExecuteUpdateAsync</c>, without loading any entities into memory.
    /// </summary>
    /// <param name="condition">Predicate that selects the entities to update.</param>
    /// <param name="setPropertyCalls">Expression that defines the property assignments to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="condition"/> or <paramref name="setPropertyCalls"/> is <c>null</c>.</exception>
    public async Task<int> ExecuteUpdateAsync(
        Expression<Func<T, bool>> condition,
        Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>>> setPropertyCalls,
        CancellationToken cancellationToken = default)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (setPropertyCalls == null) throw new ArgumentNullException(nameof(setPropertyCalls));
        return await ExecuteWithExceptionHandlingAsync(
            () => _dbContext.Set<T>().Where(condition).ExecuteUpdateAsync(setPropertyCalls, cancellationToken),
            nameof(ExecuteUpdateAsync));
    }

    /// <summary>
    /// Executes <paramref name="operations"/> inside a database transaction.
    /// If a transaction is already active on the current context, the operations are run within it without creating a new one.
    /// The transaction is rolled back automatically on failure.
    /// </summary>
    /// <param name="operations">Async delegate containing the write operations to execute transactionally.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operations"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operations fail and the transaction is rolled back successfully.</exception>
    /// <exception cref="AggregateException">Thrown when the operations fail and the rollback also fails.</exception>
    public async Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)
    {
        if (operations == null) throw new ArgumentNullException(nameof(operations));
        if (_dbContext.Database.CurrentTransaction != null)
        {
            await operations();
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operations();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                throw new AggregateException(
                    $"Transaction failed and rollback also failed in {nameof(ExecuteTransactionAsync)}.",
                    ex,
                    rollbackEx);
            }

            throw new InvalidOperationException(
                $"Error executing transaction in {nameof(ExecuteTransactionAsync)}.",
                ex);
        }
    }

    /// <summary>
    /// Inserts a large collection of entities in bulk using <c>EFCore.BulkExtensions</c>.
    /// Prefer this over <see cref="AddRangeAsync"/> for high-volume inserts.
    /// </summary>
    /// <param name="entities">The entities to insert. Must be non-null and non-empty.</param>
    /// <param name="bulkConfig">Optional EFCore.BulkExtensions configuration (batch size, identity output, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task BulkInsertAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkInsertAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return 0;
            },
            nameof(BulkInsertAsync));
    }

    /// <summary>
    /// Updates a large collection of entities in bulk using <c>EFCore.BulkExtensions</c>.
    /// Prefer this over <see cref="UpdateRangeAsync"/> for high-volume updates.
    /// </summary>
    /// <param name="entities">The entities to update. Must be non-null and non-empty.</param>
    /// <param name="bulkConfig">Optional EFCore.BulkExtensions configuration (batch size, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task BulkUpdateAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return 0;
            },
            nameof(BulkUpdateAsync));
    }

    /// <summary>
    /// Deletes a large collection of entities in bulk using <c>EFCore.BulkExtensions</c>.
    /// Prefer this over <see cref="DeleteRangeAsync"/> for high-volume deletions.
    /// </summary>
    /// <param name="entities">The entities to delete. Must be non-null and non-empty.</param>
    /// <param name="bulkConfig">Optional EFCore.BulkExtensions configuration (batch size, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task BulkDeleteAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkDeleteAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return 0;
            },
            nameof(BulkDeleteAsync));
    }

    /// <summary>
    /// Inserts or updates a large collection of entities in bulk using <c>EFCore.BulkExtensions</c>.
    /// Each entity is inserted if it does not exist, or updated if it does, based on the configured primary key or <see cref="BulkConfig"/> properties.
    /// </summary>
    /// <param name="entities">The entities to insert or update. Must be non-null and non-empty.</param>
    /// <param name="bulkConfig">Optional EFCore.BulkExtensions configuration (batch size, update-by properties, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is <c>null</c>.</exception>
    public async Task BulkInsertOrUpdateAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkInsertOrUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return 0;
            },
            nameof(BulkInsertOrUpdateAsync));
    }
}
