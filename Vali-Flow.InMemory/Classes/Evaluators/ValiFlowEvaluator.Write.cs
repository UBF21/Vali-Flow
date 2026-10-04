using Vali_Flow.Abstractions.Diagnostics;

namespace Vali_Flow.InMemory.Classes.Evaluators;

public partial class ValiFlowEvaluator<T, TProperty>
{
    /// <summary>Adds a single entity to the store or the supplied list.</summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns><see langword="true"/> if the entity was added successfully.</returns>
    public bool Add(T entity, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.Add", tag, typeof(T).Name);
        try
        {
            return _store.Add(entity, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Updates a matching entity in the store or the supplied list.</summary>
    /// <param name="entity">Entity with updated values; matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>The updated entity, or <see langword="null"/> if no matching entity was found.</returns>
    public T? Update(T entity, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.Update", tag, typeof(T).Name);
        try
        {
            return _store.Update(entity, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Removes a matching entity from the store or the supplied list.</summary>
    /// <param name="entity">Entity to remove; matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns><see langword="true"/> if the entity was found and removed.</returns>
    public bool Delete(T entity, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.Delete", tag, typeof(T).Name);
        try
        {
            return _store.Delete(entity, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Adds multiple entities to the store or the supplied list.</summary>
    /// <param name="entitiesToAdd">Entities to add.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    public void AddRange(IEnumerable<T> entitiesToAdd, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.AddRange", tag, typeof(T).Name);
        try
        {
            _store.AddRange(entitiesToAdd, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Updates multiple matching entities in the store or the supplied list.</summary>
    /// <param name="entitiesToUpdate">Entities with updated values; each matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Sequence of successfully updated entities.</returns>
    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.UpdateRange", tag, typeof(T).Name);
        try
        {
            return _store.UpdateRange(entitiesToUpdate, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Removes multiple matching entities from the store or the supplied list.</summary>
    /// <param name="entitiesToDelete">Entities to remove; each matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Number of entities removed.</returns>
    public int DeleteRange(IEnumerable<T> entitiesToDelete, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.DeleteRange", tag, typeof(T).Name);
        try
        {
            return _store.DeleteRange(entitiesToDelete, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Inserts or updates a single entity in the store or the supplied list.</summary>
    /// <param name="entity">Entity to insert if not found, or update if already present (matched by key selector).</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>The inserted or updated entity.</returns>
    public T Upsert(T entity, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.Upsert", tag, typeof(T).Name);
        try
        {
            return _store.Upsert(entity, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Inserts or updates multiple entities in the store or the supplied list.</summary>
    /// <param name="entitiesToUpsert">Entities to insert or update; each matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Sequence of inserted or updated entities.</returns>
    public IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.UpsertRange", tag, typeof(T).Name);
        try
        {
            return _store.UpsertRange(entitiesToUpsert, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Removes all entities that satisfy an arbitrary predicate.</summary>
    /// <param name="predicate">Condition an entity must meet to be removed.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Number of entities removed.</returns>
    public int DeleteByCondition(Func<T, bool> predicate, List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.DeleteByCondition", tag, typeof(T).Name);
        try
        {
            return _store.DeleteByCondition(predicate, entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>Persists pending changes to the internal store or flushes the supplied list.</summary>
    /// <param name="entities">Optional external list whose changes should be committed.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    public void SaveChanges(List<T>? entities = null, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.SaveChanges", tag, typeof(T).Name);
        try
        {
            _store.SaveChanges(entities);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }
}
