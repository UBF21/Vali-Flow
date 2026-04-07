namespace Vali_Flow.InMemory.Classes.Stores;

internal sealed class InMemoryWriteStore<T, TProperty> where T : class where TProperty : notnull
{
    private readonly object _lock = new();

    internal List<T> Items { get; }
    internal readonly Func<T, TProperty> GetId;

    private readonly List<T> _addedEntities = new();
    private readonly List<T> _updatedEntities = new();
    private readonly List<T> _deletedEntities = new();

    internal InMemoryWriteStore(IEnumerable<T>? initialData, Func<T, TProperty>? getId)
    {
        Items = initialData?.ToList() ?? new List<T>();
        if (getId != null)
        {
            GetId = getId;
        }
        else
        {
            var property = typeof(T).GetProperty("Id")
                ?? throw new InvalidOperationException(
                    $"Entity '{typeof(T).Name}' has no 'Id' property. Provide a getId function.");
            GetId = entity => (TProperty)Convert.ChangeType(property.GetValue(entity)!, typeof(TProperty))!;
        }
    }

    internal bool Add(T entity, IEnumerable<T>? entities = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        lock (_lock)
        {
            _addedEntities.Add(entity);
            (entities as List<T>)?.Add(entity);
        }
        return true;
    }

    internal T? Update(T entity, IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            IEnumerable<T> dataSource = entities ?? Items;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(GetId(e), GetId(entity)));
            if (existing != null)
            {
                _updatedEntities.Add(entity);
                if (entities is List<T> list)
                {
                    var index = list.FindIndex(e =>
                        EqualityComparer<TProperty>.Default.Equals(GetId(e), GetId(entity)));
                    if (index >= 0) list[index] = entity;
                }

                return entity;
            }

            return null;
        }
    }

    internal bool Delete(T entity, IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            IEnumerable<T> dataSource = entities ?? Items;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(GetId(e), GetId(entity)));
            if (existing != null)
            {
                _deletedEntities.Add(existing);
                if (entities is List<T> list) list.Remove(existing);
                return true;
            }

            return false;
        }
    }

    internal void AddRange(IEnumerable<T> entitiesToAdd, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        var list = entities as List<T>;
        lock (_lock)
        {
            foreach (T entity in entitiesToAdd)
            {
                _addedEntities.Add(entity);
                list?.Add(entity);
            }
        }
    }

    internal IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            List<T> source = entities is List<T> l ? l : (entities?.ToList() ?? Items);
            var indexById = new Dictionary<TProperty, int>(source.Count);
            for (int i = 0; i < source.Count; i++)
                indexById[GetId(source[i])!] = i;

            var updated = new List<T>();
            foreach (T entity in entitiesToUpdate)
            {
                TProperty id = GetId(entity)!;
                if (!indexById.TryGetValue(id, out int idx)) continue;
                _updatedEntities.Add(entity);
                updated.Add(entity);
                source[idx] = entity;
                indexById[id] = idx;
            }
            return updated;
        }
    }

    internal int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            List<T> source = entities is List<T> l ? l : (entities?.ToList() ?? Items);
            var entityById = new Dictionary<TProperty, T>(source.Count);
            foreach (T item in source)
                entityById[GetId(item)!] = item;

            int count = 0;
            var toRemove = new List<T>();
            foreach (T entity in entitiesToDelete)
            {
                TProperty id = GetId(entity)!;
                if (!entityById.TryGetValue(id, out T? existing)) continue;
                _deletedEntities.Add(existing);
                toRemove.Add(existing);
                entityById.Remove(id);
                count++;
            }
            if (toRemove.Count > 0)
            {
                var removeSet = new HashSet<TProperty>(toRemove.Select(e => GetId(e)!));
                source.RemoveAll(e => removeSet.Contains(GetId(e)!));
            }
            return count;
        }
    }

    internal T Upsert(T entity, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        lock (_lock)
        {
            IEnumerable<T> dataSource = entities ?? Items;
            var existing = dataSource.FirstOrDefault(e =>
                EqualityComparer<TProperty>.Default.Equals(GetId(e), GetId(entity)));
            if (existing != null)
            {
                _updatedEntities.Add(entity);
                if (entities is List<T> list)
                {
                    var index = list.FindIndex(e =>
                        EqualityComparer<TProperty>.Default.Equals(GetId(e), GetId(entity)));
                    if (index >= 0) list[index] = entity;
                }
            }
            else
            {
                _addedEntities.Add(entity);
                (entities as List<T>)?.Add(entity);
            }
        }

        return entity;
    }

    internal IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        var deduplicated = entitiesToUpsert
            .GroupBy(e => GetId(e))
            .Select(g => g.Last())
            .ToList();
        var results = new List<T>();
        foreach (var entity in deduplicated)
            results.Add(Upsert(entity, entities));
        return results;
    }

    internal int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            IEnumerable<T> dataSource = entities ?? Items;
            var toDelete = dataSource.Where(predicate).ToList();
            foreach (var entity in toDelete)
            {
                _deletedEntities.Add(entity);
                if (entities is List<T> list)
                    list.Remove(entity);
            }

            return toDelete.Count;
        }
    }

    internal void SaveChanges(IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            if (entities is List<T> externalList)
            {
                var existingIds = new HashSet<TProperty>(externalList.Select(e => GetId(e)!));
                foreach (var entity in _addedEntities)
                    if (existingIds.Add(GetId(entity)!))
                        externalList.Add(entity);

                var indexMap = new Dictionary<TProperty, int>(externalList.Count);
                for (int i = 0; i < externalList.Count; i++)
                    indexMap[GetId(externalList[i])!] = i;
                foreach (var entity in _updatedEntities)
                    if (indexMap.TryGetValue(GetId(entity)!, out int idx))
                        externalList[idx] = entity;

                if (_deletedEntities.Count > 0)
                {
                    var deleteIds = new HashSet<TProperty>(_deletedEntities.Select(e => GetId(e)!));
                    externalList.RemoveAll(e => deleteIds.Contains(GetId(e)!));
                }

                _addedEntities.Clear();
                _updatedEntities.Clear();
                _deletedEntities.Clear();
                return;
            }

            // Apply to internal store
            foreach (var entity in _addedEntities)
            {
                if (!Items.Contains(entity, new EntityEqualityComparer<T, TProperty>(GetId)))
                    Items.Add(entity);
            }

            foreach (var entity in _updatedEntities)
            {
                var index = Items.FindIndex(e =>
                    EqualityComparer<TProperty>.Default.Equals(GetId(e), GetId(entity)));
                if (index >= 0)
                    Items[index] = entity;
            }

            if (_deletedEntities.Count > 0)
            {
                var deleteIds = new HashSet<TProperty>(_deletedEntities.Select(e => GetId(e)!));
                Items.RemoveAll(e => deleteIds.Contains(GetId(e)!));
            }

            _addedEntities.Clear();
            _updatedEntities.Clear();
            _deletedEntities.Clear();
        }
    }
}

internal class EntityEqualityComparer<T, TProperty> : IEqualityComparer<T> where T : class
{
    private readonly Func<T, TProperty>? _getId;

    public EntityEqualityComparer(Func<T, TProperty>? getId)
    {
        _getId = getId;
    }

    public bool Equals(T? x, T? y)
    {
        if (x == null || y == null || _getId == null) return false;
        return EqualityComparer<TProperty>.Default.Equals(_getId(x), _getId(y));
    }

    public int GetHashCode(T obj)
    {
        return _getId?.Invoke(obj)?.GetHashCode() ?? 0;
    }
}
