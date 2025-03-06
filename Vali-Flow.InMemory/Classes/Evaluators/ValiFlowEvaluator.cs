using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;

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

    public void SetValiFlow(ValiFlow<T> valiFlow)
    {
        _valiFlow = valiFlow ?? throw new ArgumentNullException(nameof(valiFlow));
    }

    private Func<T, bool> GetCondition(bool negated = false)
    {
        return negated ? BuildNegated().Compile() : Build().Compile();
    }

    private Expression<Func<T, bool>> Build()
    {
        return _valiFlow?.Build() ?? (x => true);
    }

    private Expression<Func<T, bool>> BuildNegated()
    {
        return _valiFlow?.BuildNegated() ?? (x => false);
    }

    public bool Evaluate(T entity)
    {
        return GetCondition()(entity);
    }

    public bool EvaluateAny(IEnumerable<T> entities)
    {
        return entities.Any(GetCondition());
    }

    public int EvaluateCount(IEnumerable<T> entities)
    {
        return entities.Count(GetCondition());
    }

    public T? GetFirstFailed(IEnumerable<T> entities)
    {
        return entities.FirstOrDefault(t => GetCondition(true)(t));
    }

    public T? GetFirst(IEnumerable<T> entities)
    {
        return entities.FirstOrDefault(GetCondition());
    }

    public IEnumerable<T> EvaluateAllFailed<TKey>(IEnumerable<T>? entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var query = (entities ?? _inMemoryStore).Where(GetCondition(true));
        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateAll<TKey>(IEnumerable<T>? entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var query = (entities ?? _inMemoryStore).Where(GetCondition());
        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluatePaged<TKey>(IEnumerable<T>? entities, int page, int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true, IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var query = EvaluateAll(entities, orderBy, ascending, thenBys);
        return query.Skip((page - ConstantHelper.One) * pageSize).Take(pageSize);
    }

    public IEnumerable<T> EvaluateTop<TKey>(IEnumerable<T>? entities, int count, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var query = EvaluateAll(entities, orderBy, ascending, thenBys);
        return query.Take(count);
    }

    public IEnumerable<T> EvaluateDistinct<TKey>(IEnumerable<T> entities, Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var query = entities.Where(GetCondition())
            .GroupBy(selector)
            .Select(g => g.First());

        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateDuplicates<TKey>(IEnumerable<T> entities, Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var query = entities.Where(GetCondition())
            .GroupBy(selector)
            .Where(g => g.Count() > ConstantHelper.One)
            .SelectMany(g => g);

        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public int GetFirstMatchIndex<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var ordered = ApplyOrdering(entities.Where(GetCondition()), orderBy, ascending, thenBys).ToList();
        return ordered.FindIndex(item => GetCondition()(item));
    }

    public int GetLastMatchIndex<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        var ordered = ApplyOrdering(entities.Where(GetCondition()), orderBy, ascending, thenBys).ToList();
        return ordered.FindLastIndex(item => GetCondition()(item));
    }

    public T? GetLastFailed<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return EvaluateAllFailed(entities, orderBy, ascending, thenBys).LastOrDefault();
    }

    public T? GetLast<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return EvaluateAll(entities, orderBy, ascending, thenBys).LastOrDefault();
    }

    public TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition()).Select(selector).Min() ?? TResult.Zero;
    }

    public TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition()).Select(selector).Max() ?? TResult.Zero;
    }

    public decimal EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition()).Select(selector).Average(x => Convert.ToDecimal(x));
    }

    public TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition()).Select(selector).Aggregate((acc, x) => acc + x);
    }

    public TResult EvaluateAggregate<TResult>(IEnumerable<T> entities, Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator) where TResult : INumber<TResult>
    {
        IEnumerable<TResult> values = entities.Where(GetCondition()).Select(selector).ToList();
        return values.Any() ? values.Aggregate(aggregator) : TResult.Zero;
    }

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(IEnumerable<T> entities,
        Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Aggregate((acc, x) => acc + x));
    }

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(IEnumerable<T> entities,
        Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Min() ?? TResult.Zero);
    }

    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(IEnumerable<T> entities,
        Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Max() ?? TResult.Zero);
    }

    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(IEnumerable<T> entities,
        Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Select(selector).Average(x => Convert.ToDecimal(x)));
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(IEnumerable<T> entities,
        Func<T, TKey> keySelector)
        where TKey : notnull
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .Where(g => g.Count() > ConstantHelper.One)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        return entities.Where(GetCondition())
            .GroupBy(keySelector)
            .Where(g => g.Count() == ConstantHelper.One)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector,
        int count, Func<T, object>? orderBy = null,
        bool ascending = true) where TKey : notnull
    {
        var query = entities.Where(GetCondition());
        if (orderBy != null) query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        return query.GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Take(count).ToList());
    }

    private IEnumerable<T> ApplyOrdering<TKey>(
        IEnumerable<T> query,
        Func<T, TKey>? orderBy,
        bool ascending,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys)
    {
        var queryList = query.ToList();

        if (orderBy == null)
        {
            return queryList;
        }

        IOrderedEnumerable<T> orderedQuery = ascending
            ? queryList.OrderBy(orderBy)
            : queryList.OrderByDescending(orderBy);

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

    public void Add(T entity)
    {
        if (GetCondition()(entity)) _addedEntities.Add(entity);
    }

    public T? Update(T entity)
    {
        if (GetCondition()(entity))
        {
            var existing = _inMemoryStore.FirstOrDefault(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _updatedEntities.Add(entity);
                return entity;
            }
        }

        return null;
    }

    public bool Delete(T entity)
    {
        if (GetCondition()(entity))
        {
            var existing = _inMemoryStore.FirstOrDefault(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _deletedEntities.Add(existing);
                return true;
            }
        }

        return false;
    }

    public void AddRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities.Where(GetCondition())) _addedEntities.Add(entity);
    }

    public IEnumerable<T> UpdateRange(IEnumerable<T> entities)
    {
        var updated = new List<T>();
        foreach (var entity in entities.Where(GetCondition()))
        {
            var existing = _inMemoryStore.FirstOrDefault(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _updatedEntities.Add(entity);
                updated.Add(entity);
            }
        }

        return updated;
    }

    public int DeleteRange(IEnumerable<T> entities)
    {
        int count = 0;
        foreach (var entity in entities.Where(GetCondition()))
        {
            var existing = _inMemoryStore.FirstOrDefault(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (existing != null)
            {
                _deletedEntities.Add(existing);
                count++;
            }
        }

        return count;
    }

    public void SaveChanges()
    {
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