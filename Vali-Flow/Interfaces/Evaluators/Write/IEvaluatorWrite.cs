using System.Linq.Expressions;
using EFCore.BulkExtensions;

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
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The added entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    public Task<T> AddAsync(T entity,bool saveChanges = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a collection of entities to the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The collection of added entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities,
        bool saveChanges = true,CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a single entity in the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<T> UpdateAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a collection of entities in the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The collection of updated entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<IEnumerable<T>> UpdateRangeAsync(
        IEnumerable<T> entities, 
        bool saveChanges = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a single entity from the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a collection of entities from the database asynchronously.
    /// </summary>
    /// <param name="entities">The collection of entities to delete.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task DeleteRangeAsync(
        IEnumerable<T> entities, 
        bool saveChanges = true,
        CancellationToken cancellationToken = default);

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
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The inserted or updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entity"/> or <paramref name="matchCondition"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        bool saveChanges = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a collection of entities based on a key selector asynchronously.
    /// </summary>
    /// <typeparam name="TProperty">The type of the key property. Must not be null.</typeparam>
    /// <param name="entities">The collection of entities to insert or update.</param>
    /// <param name="keySelector">A function to select the key property for matching entities.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The collection of inserted or updated entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entities"/> or <paramref name="keySelector"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Func<T, TProperty> keySelector,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull;

    /// <summary>
    /// Deletes entities that match a specified condition from the database asynchronously.
    /// </summary>
    /// <param name="condition">An expression to identify entities to delete.</param>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, defers saving.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="condition"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
    Task DeleteByConditionAsync(
        Expression<Func<T, bool>> condition,
        bool saveChanges = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a set of database operations within a transaction asynchronously.
    /// </summary>
    /// <param name="operations">The operations to execute within the transaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the transaction fails or rollback fails.</exception>
    Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a collection of entities into the database in a single bulk operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert. Cannot be null or empty.</param>
    /// <param name="bulkConfig">Optional configuration for the bulk operation, such as batch size or identity handling. If null, default settings are used.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses EFCore.BulkExtensions to perform a high-performance bulk insert, significantly reducing database round-trips and memory usage 
    /// compared to <c>AddRange</c> and <c>SaveChanges</c>. The <paramref name="bulkConfig"/> parameter allows customization of options such as 
    /// <c>BatchSize</c>, <c>IncludeGraph</c> for related entities, or <c>SetOutputIdentity</c> for retrieving generated keys. 
    /// Use this method for inserting large datasets efficiently. Ensure that the entities are valid and compatible with the database schema to avoid errors.
    /// </remarks>
    /// <example>
    /// <code>
    /// var products = new List&lt;Product&gt;
    /// {
    ///     new Product { Name = "Product1", Price = 100 },
    ///     new Product { Name = "Product2", Price = 200 }
    /// };
    /// var bulkConfig = new BulkConfig { BatchSize = 1000, SetOutputIdentity = true };
    /// await evaluator.BulkInsertAsync(products, bulkConfig);
    /// // Inserts products in a single bulk operation with a batch size of 1000, retrieving generated IDs.
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs during the bulk insert operation.</exception>ƒ
    public Task BulkInsertAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a collection of entities in the database in a single bulk operation.
    /// </summary>
    /// <param name="entities">The collection of entities to update. Cannot be null or empty.</param>
    /// <param name="bulkConfig">Optional configuration for the bulk operation, such as batch size or properties to update. If null, default settings are used.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses EFCore.BulkExtensions to perform a high-performance bulk update, significantly reducing database round-trips and memory usage 
    /// compared to <c>UpdateRange</c> and <c>SaveChanges</c>. The <paramref name="bulkConfig"/> parameter allows customization of options such as 
    /// <c>BatchSize</c> or <c>PropertiesToInclude</c> to specify which properties to update. 
    /// Use this method for updating large datasets efficiently. Ensure that the entities have valid primary keys and are compatible with the database schema.
    /// </remarks>
    /// <example>
    /// <code>
    /// var products = await evaluator.ListAsync(new QuerySpecification&lt;Product&gt;(new ValiFlow&lt;Product&gt;().AddRule(p => p.Price > 100)));
    /// foreach (var product in products) { product.Price += 10; }
    /// var bulkConfig = new BulkConfig { BatchSize = 1000, PropertiesToInclude = new List&lt;string&gt; { nameof(Product.Price) } };
    /// await evaluator.BulkUpdateAsync(products, bulkConfig);
    /// // Updates the Price of products in a single bulk operation with a batch size of 1000.
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs during the bulk update operation.</exception>
    public Task BulkUpdateAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a collection of entities from the database in a single bulk operation.
    /// </summary>
    /// <param name="entities">The collection of entities to delete. Cannot be null or empty.</param>
    /// <param name="bulkConfig">Optional configuration for the bulk operation, such as batch size. If null, default settings are used.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses EFCore.BulkExtensions to perform a high-performance bulk delete, significantly reducing database round-trips and memory usage 
    /// compared to <c>RemoveRange</c> and <c>SaveChanges</c>. The <paramref name="bulkConfig"/> parameter allows customization of options such as 
    /// <c>BatchSize</c>. Use this method for deleting large datasets efficiently. 
    /// Ensure that the entities have valid primary keys to avoid errors during deletion.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs during the bulk delete operation.</exception>
    public Task BulkDeleteAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a collection of entities in the database in a single bulk operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert or update. Cannot be null or empty.</param>
    /// <param name="bulkConfig">Optional configuration for the bulk operation, such as batch size or properties to compare. If null, default settings are used.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses EFCore.BulkExtensions to perform a high-performance bulk upsert (insert if the entity does not exist, update if it does), 
    /// significantly reducing database round-trips and memory usage compared to individual <c>Add</c> or <c>Update</c> operations with <c>SaveChanges</c>. 
    /// The <paramref name="bulkConfig"/> parameter allows customization of options such as <c>BatchSize</c>, <c>IncludeGraph</c> for related entities, 
    /// or <c>PropertiesToIncludeOnCompare</c> to define which properties determine if an entity exists. 
    /// Use this method for synchronizing large datasets efficiently. Ensure that the entities are valid and compatible with the database schema.
    /// </remarks>
    /// <example>
    /// <code>
    /// var products = new List&lt;Product&gt;
    /// {
    ///     new Product { Id = 1, Name = "UpdatedProduct", Price = 150 },
    ///     new Product { Name = "NewProduct", Price = 200 }
    /// };
    /// var bulkConfig = new BulkConfig { BatchSize = 1000, SetOutputIdentity = true, PropertiesToIncludeOnCompare = new List&lt;string&gt; { nameof(Product.Id) } };
    /// await evaluator.BulkInsertOrUpdateAsync(products, bulkConfig);
    /// // Inserts new products or updates existing ones based on Id, with a batch size of 1000, retrieving generated IDs.
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entities"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs during the bulk insert or update operation.</exception>
    public Task BulkInsertOrUpdateAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default);
}