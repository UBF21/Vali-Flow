namespace Vali_Flow.InMemory.Classes.Stores;

internal sealed class InMemoryWriteStore<T, TProperty> where T : class where TProperty : notnull
{
    private readonly object _lock = new();

    private readonly List<T> _items;
    private readonly Func<T, TProperty> _getId;

    private readonly List<T> _addedEntities = new();
    private readonly List<T> _updatedEntities = new();
    private readonly List<T> _deletedEntities = new();

    internal IReadOnlyList<T> Items
    {
        get { lock (_lock) { return _items.ToList(); } }
    }

    internal TProperty GetEntityId(T entity) => _getId(entity);

    internal InMemoryWriteStore(IEnumerable<T>? initialData, Func<T, TProperty>? getId)
    {
        _items = initialData?.ToList() ?? new List<T>();
        if (getId != null)
        {
            _getId = getId;
        }
        else
        {
            var property = typeof(T).GetProperty("Id")
                ?? throw new InvalidOperationException(
                    $"Entity '{typeof(T).Name}' has no 'Id' property. Provide a getId function.");
            _getId = entity => (TProperty)Convert.ChangeType(property.GetValue(entity)!, typeof(TProperty))!;
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
            IEnumerable<T> dataSource = entities ?? _items;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _updatedEntities.Add(entity);
                if (entities is List<T> list)
                {
                    var index = list.FindIndex(e =>
                        EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
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
            IEnumerable<T> dataSource = entities ?? _items;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
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
            List<T> source = entities is List<T> l ? l : (entities?.ToList() ?? _items);
            var indexById = new Dictionary<TProperty, int>(source.Count);
            for (int i = 0; i < source.Count; i++)
                indexById[_getId(source[i])!] = i;

            var updated = new List<T>();
            foreach (T entity in entitiesToUpdate)
            {
                TProperty id = _getId(entity)!;
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
            List<T> source = entities is List<T> l ? l : (entities?.ToList() ?? _items);
            var entityById = new Dictionary<TProperty, T>(source.Count);
            foreach (T item in source)
                entityById[_getId(item)!] = item;

            int count = 0;
            var toRemove = new List<T>();
            foreach (T entity in entitiesToDelete)
            {
                TProperty id = _getId(entity)!;
                if (!entityById.TryGetValue(id, out T? existing)) continue;
                _deletedEntities.Add(existing);
                toRemove.Add(existing);
                entityById.Remove(id);
                count++;
            }
            if (toRemove.Count > 0)
            {
                var removeSet = new HashSet<TProperty>(toRemove.Select(e => _getId(e)!));
                source.RemoveAll(e => removeSet.Contains(_getId(e)!));
            }
            return count;
        }
    }

    private T UpsertCore(T entity, IEnumerable<T>? entities)
    {
        // Must be called within _lock
        if (entities is List<T> externalList)
        {
            var index = externalList.FindIndex(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (index >= 0)
                externalList[index] = entity;
            else
                externalList.Add(entity);

            if (index >= 0)
                _updatedEntities.Add(entity);
            else
                _addedEntities.Add(entity);
        }
        else
        {
            var index = _items.FindIndex(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (index >= 0)
                _updatedEntities.Add(entity);
            else
                _addedEntities.Add(entity);
        }

        return entity;
    }

    internal T Upsert(T entity, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        lock (_lock)
        {
            return UpsertCore(entity, entities);
        }
    }

    internal IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        var deduplicated = entitiesToUpsert
            .GroupBy(e => _getId(e))
            .Select(g => g.Last())
            .ToList();
        var results = new List<T>();
        lock (_lock)
        {
            foreach (var entity in deduplicated)
                results.Add(UpsertCore(entity, entities));
        }
        return results;
    }

    internal int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null)
    {
        lock (_lock)
        {
            IEnumerable<T> dataSource = entities ?? _items;
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
                ApplyPendingChanges(externalList);
                return;
            }

            ApplyPendingChanges(_items);
        }
    }

    private void ApplyPendingChanges(List<T> target)
    {
        // adds — skip duplicates by ID
        var existingIds = new HashSet<TProperty>(target.Select(e => _getId(e)!));
        foreach (var entity in _addedEntities)
            if (existingIds.Add(_getId(entity)!))
                target.Add(entity);
        _addedEntities.Clear();

        // updates
        var indexMap = new Dictionary<TProperty, int>(target.Count);
        for (int i = 0; i < target.Count; i++)
            indexMap[_getId(target[i])!] = i;
        foreach (var entity in _updatedEntities)
            if (indexMap.TryGetValue(_getId(entity)!, out int idx))
                target[idx] = entity;
        _updatedEntities.Clear();

        // deletes
        if (_deletedEntities.Count > 0)
        {
            var deleteIds = new HashSet<TProperty>(_deletedEntities.Select(e => _getId(e)!));
            target.RemoveAll(e => deleteIds.Contains(_getId(e)!));
        }
        _deletedEntities.Clear();
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
