namespace Vali_Flow.InMemory.Interfaces.Evaluators.Write;

/// <summary>
/// Defines methods for performing write operations (add, update, delete) on entities in memory using Vali-Flow.
/// </summary>
/// <typeparam name="T">The type of the entities to manipulate.</typeparam>
public interface IInMemoryEvaluatorWrite<T>
{
    /// <summary>
    /// Adds a single entity to the specified collection or a default context if no collection is provided.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="entities">The optional collection of entities to add to. If null, uses a default context.</param>
    /// <returns>True if the entity was added successfully; otherwise, false.</returns>
    public bool Add(T entity,IEnumerable<T>? entities = null);
    
    /// <summary>
    /// Updates a single entity in the specified collection or a default context if no collection is provided.
    /// </summary>
    /// <param name="entity">The entity to update. The entity must exist in the collection for the update to succeed.</param>
    /// <param name="entities">The optional collection of entities to update within. If null, uses a default context.</param>
    /// <returns>The updated entity if successful, or null if the entity was not found or the update failed.</returns>
    public T? Update(T entity,IEnumerable<T>? entities = null);
    
    /// <summary>
    /// Deletes a single entity from the specified collection or a default context if no collection is provided.
    /// </summary>
    /// <param name="entity">The entity to delete. The entity must exist in the collection for the deletion to succeed.</param>
    /// <param name="entities">The optional collection of entities to delete from. If null, uses a default context.</param>
    /// <returns>True if the entity was deleted successfully; otherwise, false.</returns>
    public bool Delete(T entity,IEnumerable<T>? entities = null);
    
    /// <summary>
    /// Adds a collection of entities to the specified collection or a default context if no collection is provided.
    /// </summary>
    /// <param name="entitiesToAdd">The collection of entities to add.</param>
    /// <param name="entities">The optional collection of entities to add to. If null, uses a default context.</param>
    public void AddRange(IEnumerable<T> entitiesToAdd,IEnumerable<T>? entities = null);
    
    /// <summary>
    /// Updates a collection of entities in the specified collection or a default context if no collection is provided.
    /// </summary>
    /// <param name="entitiesToUpdate">The collection of entities to update. Entities must exist in the collection for updates to succeed.</param>
    /// <param name="entities">The optional collection of entities to update within. If null, uses a default context.</param>
    /// <returns>An enumerable of the updated entities.</returns>
    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate,IEnumerable<T>? entities = null);
    
    /// <summary>
    /// Deletes a collection of entities from the specified collection or a default context if no collection is provided.
    /// </summary>
    /// <param name="entitiesToDelete">The collection of entities to delete. Entities must exist in the collection for deletions to succeed.</param>
    /// <param name="entities">The optional collection of entities to delete from. If null, uses a default context.</param>
    /// <returns>The number of entities successfully deleted.</returns>
    public int DeleteRange(IEnumerable<T> entitiesToDelete,IEnumerable<T>? entities = null);
    
    /// <summary>
    /// Saves any pending changes to the specified collection or a default context if no collection is provided.
    /// This method ensures that all previous write operations are applied.
    /// </summary>
    /// <param name="entities">The optional collection of entities to save changes for. If null, uses a default context.</param>
    public void SaveChanges(IEnumerable<T>? entities = null);
}