using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Classes.Stores;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;
using Vali_Flow.InMemory.Models;
using Vali_Flow.InMemory.Utils;

namespace Vali_Flow.InMemory.Classes.Evaluators;

public sealed class ValiFlowEvaluator<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
    where T : class
    where TProperty : notnull
{
    private readonly InMemoryWriteStore<T, TProperty> _store;
    private readonly object _stateLock = new();
    private ValiFlow<T>? _valiFlow;
    private Func<T, bool>? _cachedNegatedCondition;
    private AsyncInMemoryAdapter<T, TProperty>? _asyncAdapter;
    private AsyncInMemoryAdapter<T, TProperty> AsyncAdapter => _asyncAdapter ??= new(this);

    public ValiFlowEvaluator(IEnumerable<T>? initialData = null, ValiFlow<T>? valiFlow = null,
        Func<T, TProperty>? getId = null)
    {
        _store = new InMemoryWriteStore<T, TProperty>(initialData, getId);
        _valiFlow = valiFlow;
    }

    #region Methods Read

    public void SetValiFlow(ValiFlow<T> valiFlow)
    {
        if (valiFlow == null) throw new ArgumentNullException(nameof(valiFlow));
        lock (_stateLock)
        {
            _valiFlow = valiFlow;
            _cachedNegatedCondition = null;
        }
    }

    private Func<T, bool> GetDefaultCondition(ValiFlow<T>? valiFlow = null, bool negated = false)
    {
        ValiFlow<T> selectedValiFlow = valiFlow ?? _valiFlow ?? new ValiFlow<T>();
        if (!negated) return selectedValiFlow.BuildCached();
        // Only cache when using the instance-level _valiFlow
        if (valiFlow == null)
        {
            lock (_stateLock)
            {
                return _cachedNegatedCondition ??= selectedValiFlow.BuildNegated().Compile();
            }
        }
        return selectedValiFlow.BuildNegated().Compile();
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

    public bool EvaluateAny(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Any(GetDefaultCondition(valiFlow, negateCondition));
    }

    public int EvaluateCount(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Count(GetDefaultCondition(valiFlow, negateCondition));
    }

    public T? GetFirstFailed(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.FirstOrDefault(t => GetDefaultCondition(valiFlow, negated: true)(t));
    }

    public T? GetFirst(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.FirstOrDefault(GetDefaultCondition(valiFlow, negateCondition));
    }

    public IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
    )
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negated: true));
        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T>? entities = null, Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition));
        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T>? entities = null,
        int page = 1,
        int pageSize = 10,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null, bool negateCondition = false
    )
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        IEnumerable<T> dataSource = entities ?? _store.Items;
        IEnumerable<T> query = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    public PagedResult<T> EvaluatePagedResult<TKey>(
        IEnumerable<T>? entities = null,
        int page = 1,
        int pageSize = 10,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        IEnumerable<T> dataSource = entities ?? _store.Items;
        IEnumerable<T> filtered = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
        var list = filtered.ToList();
        int totalCount = list.Count;
        IEnumerable<T> pageItems = list.Skip((page - 1) * pageSize).Take(pageSize);
        return new PagedResult<T>(pageItems, totalCount, page, pageSize);
    }

    public IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T>? entities = null,
        int count = 10,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        IEnumerable<T> dataSource = entities ?? _store.Items;
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IEnumerable<T> dataSource = entities ?? _store.Items;
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IEnumerable<T> dataSource = entities ?? _store.Items;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(selector)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g);

        return ApplyOrdering(query, orderBy, ascending, thenBys);
    }

    public int GetFirstMatchIndex<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        List<T> ordered = ApplyOrdering(dataSource, orderBy, ascending, thenBys).ToList();
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        return ordered.FindIndex(item => condition(item));
    }

    public int GetLastMatchIndex<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        List<T> ordered = ApplyOrdering(dataSource, orderBy, ascending, thenBys).ToList();
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        return ordered.FindLastIndex(item => condition(item));
    }

    public T? GetLastFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
    )
    {
        return EvaluateAllFailed(entities, orderBy, ascending, thenBys, valiFlow).LastOrDefault();
    }

    public T? GetLast<TKey>(
        IEnumerable<T>? entities = null,
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
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector).Min() ?? TResult.Zero;
    }

    public TResult EvaluateMax<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector).Max() ?? TResult.Zero;
    }

    public decimal EvaluateAverage<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        return ComputeAverage(dataSource.Where(condition).Select(selector));
    }

    public TResult EvaluateSum<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource
            .Where(GetDefaultCondition(valiFlow, negateCondition))
            .Aggregate(TResult.Zero, (acc, item) => acc + selector(item));
    }

    public TResult EvaluateAggregate<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource
            .Where(GetDefaultCondition(valiFlow, negateCondition))
            .Select(selector)
            .Aggregate(TResult.Zero, aggregator);
    }

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        var result = new Dictionary<TKey, List<T>>();
        foreach (T item in dataSource)
        {
            if (!condition(item)) continue;
            TKey key = keySelector(item);
            if (!result.TryGetValue(key, out List<T>? group))
                result[key] = group = new List<T>();
            group.Add(item);
        }
        return result;
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
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
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g =>
                g.Aggregate(TResult.Zero, (acc, item) => acc + selector(item)));
    }

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
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
        IEnumerable<T> dataSource = entities ?? _store.Items;
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
        IEnumerable<T> dataSource = entities ?? _store.Items;
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        return dataSource.Where(condition)
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => ComputeAverage(g.Select(selector)));
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .Where(g => g.Skip(1).Any())
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey, TOrderKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, TOrderKey>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition));
        if (orderBy != null) query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        return query.GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Take(count).ToList());
    }

    #endregion

    #region Methods Private

    private static decimal ComputeAverage<TResult>(IEnumerable<TResult> values)
        where TResult : INumber<TResult>
    {
        TResult sum = TResult.Zero;
        int count = 0;
        foreach (TResult v in values)
        {
            sum += v;
            count++;
        }
        return count == 0 ? 0m : decimal.CreateChecked(sum) / count;
    }

    private IEnumerable<T> ApplyOrdering<TKey>(
        IEnumerable<T>? query,
        Func<T, TKey>? orderBy,
        bool ascending,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys
    )
    {
        IEnumerable<T> dataSource = query ?? _store.Items;

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

    public bool Add(T entity, IEnumerable<T>? entities = null) => _store.Add(entity, entities);

    public T? Update(T entity, IEnumerable<T>? entities = null) => _store.Update(entity, entities);

    public bool Delete(T entity, IEnumerable<T>? entities = null) => _store.Delete(entity, entities);

    public void AddRange(IEnumerable<T> entitiesToAdd, IEnumerable<T>? entities = null) =>
        _store.AddRange(entitiesToAdd, entities);

    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null) =>
        _store.UpdateRange(entitiesToUpdate, entities);

    public int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null) =>
        _store.DeleteRange(entitiesToDelete, entities);

    public T Upsert(T entity, IEnumerable<T>? entities = null) => _store.Upsert(entity, entities);

    public IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null) =>
        _store.UpsertRange(entitiesToUpsert, entities);

    public int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null) =>
        _store.DeleteByCondition(predicate, entities);

    public void SaveChanges(IEnumerable<T>? entities = null) => _store.SaveChanges(entities);

    #endregion

    #region IQueryReader<T> + IQueryAggregator<T> — provider-agnostic methods

    Task<bool> IQueryReader<T>.EvaluateAnyAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateAnyAsync(filter, cancellationToken);

    Task<int> IQueryReader<T>.EvaluateCountAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateCountAsync(filter, cancellationToken);

    Task<T?> IQueryReader<T>.EvaluateGetFirstAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateGetFirstAsync(filter, cancellationToken);

    Task<T?> IQueryReader<T>.EvaluateGetLastAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateGetLastAsync(filter, cancellationToken);

    Task<TResult> IQueryAggregator<T>.EvaluateMinAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateMinAsync(selector, filter, cancellationToken);

    Task<TResult> IQueryAggregator<T>.EvaluateMaxAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateMaxAsync(selector, filter, cancellationToken);

    Task<decimal> IQueryAggregator<T>.EvaluateAverageAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateAverageAsync(selector, filter, cancellationToken);

    Task<TResult> IQueryAggregator<T>.EvaluateSumAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => AsyncAdapter.EvaluateSumAsync(selector, filter, cancellationToken);

    #endregion
}
