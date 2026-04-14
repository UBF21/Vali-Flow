namespace Vali_Flow.InMemory.Classes.Stores;

internal class InMemoryWriteStore<T, TProperty> where T : class where TProperty : notnull
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

    internal bool Add(T entity, List<T>? entities = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        _ = entities; // Add always queues to the deferred add list; entities is unused (see AddRange)
        lock (_lock)
        {
            _addedEntities.Add(entity);
        }
        return true;
    }

    internal T? Update(T entity, List<T>? entities = null)
    {
        lock (_lock)
        {
            // Check if entity is pending as add (not yet saved)
            var pendingIdx = _addedEntities.FindIndex(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (pendingIdx >= 0)
            {
                _addedEntities[pendingIdx] = entity; // replace pending add with updated version
                return entity;
            }

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

    internal bool Delete(T entity, List<T>? entities = null)
    {
        lock (_lock)
        {
            // Cancel pending add if entity was added but not yet saved
            var pendingIdx = _addedEntities.FindIndex(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (pendingIdx >= 0)
            {
                _addedEntities.RemoveAt(pendingIdx);
                return true;
            }

            IEnumerable<T> dataSource = entities ?? _items;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _deletedEntities.Add(existing);
                return true;
            }

            return false;
        }
    }

    internal void AddRange(IEnumerable<T> entitiesToAdd, List<T>? entities = null)
    {
        _ = entities; // AddRange always queues to the deferred add list; entities is intentionally unused
        lock (_lock)
        {
            foreach (T entity in entitiesToAdd)
                _addedEntities.Add(entity);
        }
    }

    internal IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, List<T>? entities = null)
    {
        lock (_lock)
        {
            List<T> source = entities ?? _items;
            var indexById = new Dictionary<TProperty, int>(source.Count);
            for (int i = 0; i < source.Count; i++)
                indexById[_getId(source[i])!] = i;

            var updated = new List<T>();
            foreach (T entity in entitiesToUpdate)
            {
                TProperty id = _getId(entity)!;

                // Check if entity is pending as add (not yet saved)
                var pendingIdx = _addedEntities.FindIndex(e =>
                    EqualityComparer<TProperty>.Default.Equals(_getId(e), id));
                if (pendingIdx >= 0)
                {
                    _addedEntities[pendingIdx] = entity; // replace pending add with updated version
                    updated.Add(entity);
                    continue;
                }

                if (!indexById.TryGetValue(id, out _)) continue;
                _updatedEntities.Add(entity);
                updated.Add(entity);
            }
            return updated;
        }
    }

    /// <returns>
    /// The number of entities queued for deletion.
    /// Changes are applied to the store when <see cref="SaveChanges"/> is called.
    /// </returns>
    internal int DeleteRange(IEnumerable<T> entitiesToDelete, List<T>? entities = null)
    {
        lock (_lock)
        {
            // Cancel pending adds first (mirrors single-entity Delete behaviour)
            var remainingToDelete = new List<T>();
            int cancelledCount = 0;
            foreach (T entity in entitiesToDelete)
            {
                TProperty id = _getId(entity)!;
                var pendingIdx = _addedEntities.FindIndex(e =>
                    EqualityComparer<TProperty>.Default.Equals(_getId(e), id));
                if (pendingIdx >= 0)
                {
                    _addedEntities.RemoveAt(pendingIdx);
                    cancelledCount++;
                }
                else
                {
                    remainingToDelete.Add(entity);
                }
            }

            // Then process remaining against committed store
            List<T> source = entities ?? _items;
            var sourceById = new Dictionary<TProperty, T>(source.Count);
            foreach (T item in source)
                sourceById[_getId(item)!] = item;

            var removeIds = new HashSet<TProperty>();
            foreach (T entity in remainingToDelete)
            {
                TProperty id = _getId(entity)!;
                if (!sourceById.TryGetValue(id, out T? existing) || !removeIds.Add(id)) continue;
                _deletedEntities.Add(existing);
            }

            return cancelledCount + removeIds.Count;
        }
    }

    private T UpsertCore(T entity, List<T>? entities)
    {
        // Must be called within _lock
        // Both paths are deferred: mutations are applied in SaveChanges via ApplyPendingChanges.
        List<T> source = entities ?? _items;

        // Check if already pending as add (not yet saved)
        var pendingIdx = _addedEntities.FindIndex(e =>
            EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
        if (pendingIdx >= 0)
        {
            _addedEntities[pendingIdx] = entity; // replace pending add
            return entity;
        }

        var index = source.FindIndex(e =>
            EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
        if (index >= 0)
            _updatedEntities.Add(entity);
        else
            _addedEntities.Add(entity);

        return entity;
    }

    internal T Upsert(T entity, List<T>? entities = null)
    {
        lock (_lock)
        {
            return UpsertCore(entity, entities);
        }
    }

    internal IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, List<T>? entities = null)
    {
        var input = entitiesToUpsert.ToList(); // snapshot before lock to avoid holding lock during enumeration
        var deduplicated = input
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

    internal int DeleteByCondition(Func<T, bool> predicate, List<T>? entities = null)
    {
        lock (_lock)
        {
            // Cancel matching pending adds so they are not persisted on SaveChanges
            var pendingToDelete = _addedEntities.Where(predicate).ToList();
            foreach (var e in pendingToDelete)
                _addedEntities.Remove(e);

            IEnumerable<T> dataSource = entities ?? _items;
            var toDelete = dataSource.Where(predicate).ToList();
            foreach (var entity in toDelete)
                _deletedEntities.Add(entity);

            return pendingToDelete.Count + toDelete.Count;
        }
    }

    internal void SaveChanges(List<T>? entities = null)
    {
        lock (_lock)
        {
            if (entities != null)
            {
                ApplyPendingChanges(entities);
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
