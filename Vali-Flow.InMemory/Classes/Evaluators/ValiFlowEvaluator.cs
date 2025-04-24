using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;
using Vali_Flow.InMemory.Utils;

namespace Vali_Flow.InMemory.Classes.Evaluators;

public class ValiFlowEvaluator<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
    where T : class
{
    private readonly List<T> _inMemoryStore;
    private readonly List<T> _addedEntities = new();
    private readonly List<T> _updatedEntities = new();
    private readonly List<T> _deletedEntities = new();
    private ValiFlow<T>? _valiFlow;
    private readonly Func<T, TProperty> _getId;

    public ValiFlowEvaluator(IEnumerable<T>? initialData = null, ValiFlow<T>? valiFlow = null,
        Func<T, TProperty>? getId = null)
    {
        _inMemoryStore = initialData?.ToList() ?? new List<T>();
        _valiFlow = valiFlow;
        _getId = getId ?? (entity =>
        {
            var property = typeof(T).GetProperty("Id");
            if (property == null)
                throw new InvalidOperationException(
                    "Entity must have an Id property, or a getId function must be provided.");
            return (TProperty)Convert.ChangeType(property.GetValue(entity), typeof(TProperty))!
                   ?? throw new InvalidOperationException("Unable to convert Id property to TProperty.");
        });
    }

    #region Methods Read

    public void SetValiFlow(ValiFlow<T> valiFlow)
    {
        _valiFlow = valiFlow ?? throw new ArgumentNullException(nameof(valiFlow));
    }

    private Func<T, bool> GetDefaultCondition(ValiFlow<T>? valiFlow = null, bool negated = false)
    {
        var selectedValiFlow = valiFlow ?? _valiFlow;
        if (selectedValiFlow == null) return negated ? _ => false : _ => true;

        return negated ? BuildNegated(selectedValiFlow).Compile() : Build(selectedValiFlow).Compile();
    }

    private Expression<Func<T, bool>> Build(ValiFlow<T>? valiFlow = null)
    {
        var selectedValiFlow = valiFlow ?? _valiFlow;
        return selectedValiFlow?.Build() ?? (x => true);
    }

    private Expression<Func<T, bool>> BuildNegated(ValiFlow<T>? valiFlow = null)
    {
        var selectedValiFlow = valiFlow ?? _valiFlow;
        return selectedValiFlow?.BuildNegated() ?? (x => false);
    }

    public bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        return GetDefaultCondition(valiFlow, negateCondition)(entity);
    }

    public bool EvaluateAny(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Any(GetDefaultCondition(valiFlow, negateCondition));
    }

    public int EvaluateCount(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Count(GetDefaultCondition(valiFlow, negateCondition));
    }

    public T? GetFirstFailed(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.FirstOrDefault(t => GetDefaultCondition(valiFlow, !negateCondition)(t));
    }

    public T? GetFirst(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.FirstOrDefault(GetDefaultCondition(valiFlow, negateCondition));
    }

    public IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T>? entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, !negateCondition));
        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T>? entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition));
        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T>? entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null, bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
        return query.Skip((page - ConstantHelper.One) * pageSize).Take(pageSize);
    }

    public IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T>? entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
        return query.Take(count);
    }

    public IEnumerable<T> EvaluateDistinct<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(selector)
            .Select(g => g.First());

        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateDuplicates<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(selector)
            .Where(g => g.Count() > ConstantHelper.One)
            .SelectMany(g => g);

        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public int GetFirstMatchIndex<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        List<T> ordered = ApplyOrdering(dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)), orderBy,
            ascending, thenBys).ToList();
        return ordered.FindIndex(item => GetDefaultCondition()(item));
    }

    public int GetLastMatchIndex<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        List<T> ordered = ApplyOrdering(dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)), orderBy,
            ascending, thenBys).ToList();
        return ordered.FindLastIndex(item => GetDefaultCondition()(item));
    }

    public T? GetLastFailed<TKey>(
        IEnumerable<T>? entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        return EvaluateAllFailed(entities, orderBy, ascending, thenBys, valiFlow, negateCondition).LastOrDefault();
    }

    public T? GetLast<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        return EvaluateAll(entities, orderBy, ascending, thenBys, valiFlow, negateCondition).LastOrDefault();
    }

    public TResult EvaluateMin<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector).Min() ?? TResult.Zero;
    }

    public TResult EvaluateMax<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector).Max() ?? TResult.Zero;
    }

    public decimal EvaluateAverage<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector)
            .Average(x => Convert.ToDecimal(x));
    }

    public TResult EvaluateSum<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector)
            .Aggregate((acc, x) => acc + x);
    }

    public TResult EvaluateAggregate<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<TResult> values = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector)
            .ToList();
        return values.Any() ? values.Aggregate(aggregator) : TResult.Zero;
    }

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Aggregate((acc, x) => acc + x));
    }

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Min() ?? TResult.Zero);
    }

    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Max() ?? TResult.Zero);
    }

    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Average(x => Convert.ToDecimal(x)));
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .Where(g => g.Count() > ConstantHelper.One)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .Where(g => g.Count() == ConstantHelper.One)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition));
        if (orderBy != null) query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        return query.GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Take(count).ToList());
    }

    #endregion

    #region Methods Private

    private IEnumerable<T> ApplyOrdering<TKey>(
        IEnumerable<T>? query,
        Func<T, TKey>? orderBy,
        bool ascending,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys
    )
    {
        IEnumerable<T> dataSource = query ?? _inMemoryStore;

        if (orderBy == null)
        {
            return dataSource;
        }

        IOrderedEnumerable<T> orderedQuery = ascending
            ? dataSource.OrderBy(orderBy)
            : dataSource.OrderByDescending(orderBy);

        if (thenBys != null)
        {
            var inMemoryThenBys = thenBys.ToList();
            if (inMemoryThenBys.Any())
            {
                orderedQuery = inMemoryThenBys.Aggregate(
                    orderedQuery,
                    (current, thenBy) => thenBy.Ascending
                        ? current.ThenBy(thenBy.ThenBy)
                        : current.ThenByDescending(thenBy.ThenBy));
            }
        }

        return orderedQuery;
    }

    #endregion

    #region Methods Write

    public bool Add(T entity, IEnumerable<T>? entities = null)
    {
        _addedEntities.Add(entity);

        if (entities is List<T> list)
        {
            list.Add(entity);
            return true;
        }

        return true;
    }

    public T? Update(T entity, IEnumerable<T>? entities = null)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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

    public bool Delete(T entity, IEnumerable<T>? entities = null)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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

    public void AddRange(IEnumerable<T> entitiesToAdd, IEnumerable<T>? entities = null)
    {
        foreach (T entity in entitiesToAdd)
        {
            _addedEntities.Add(entity);
            if (entities is List<T> list) list.Add(entity);
        }
    }

    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null)
    {
        var updated = new List<T>();
        foreach (T entity in entitiesToUpdate)
        {
            // Usa la colección proporcionada o _inMemoryStore como fuente para buscar la entidad existente
            IEnumerable<T> dataSource = entities ?? _inMemoryStore;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _updatedEntities.Add(entity);
                updated.Add(entity);
                if (entities is List<T> list)
                {
                    var index = list.FindIndex(e =>
                        EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
                    if (index >= 0)
                    {
                        list[index] = entity;
                    }
                }
            }
        }

        return updated;
    }

    public int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null)
    {
        int count = 0;
        foreach (T entity in entitiesToDelete)
        {
            IEnumerable<T> dataSource = entities ?? _inMemoryStore;
            var existing =
                dataSource.FirstOrDefault(e => EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _deletedEntities.Add(existing);
                if (entities is List<T> list)
                {
                    list.Remove(existing);
                }

                count++;
            }
        }

        return count;
    }

    public void SaveChanges(IEnumerable<T>? entities = null)
    {
        if (entities != null)
        {
            _addedEntities.Clear();
            _updatedEntities.Clear();
            _deletedEntities.Clear();
            return;
        }

        foreach (var entity in _addedEntities)
        {
            if (!_inMemoryStore.Contains(entity, new EntityEqualityComparer<T, TProperty>(_getId)))
            {
                _inMemoryStore.Add(entity);
            }
        }

        foreach (var entity in _updatedEntities)
        {
            var index = _inMemoryStore.FindIndex(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (index >= 0)
            {
                _inMemoryStore[index] = entity;
            }
        }

        foreach (var entity in _deletedEntities)
        {
            _inMemoryStore.Remove(entity);
        }

        _addedEntities.Clear();
        _updatedEntities.Clear();
        _deletedEntities.Clear();
    }

    #endregion
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