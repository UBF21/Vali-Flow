using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;
using Vali_Flow.InMemory.Models;
using Vali_Flow.InMemory.Utils;

namespace Vali_Flow.InMemory.Classes.Evaluators;

public sealed class ValiFlowEvaluator<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
    where T : class
{
    private readonly List<T> _inMemoryStore;
    private readonly List<T> _addedEntities = new();
    private readonly List<T> _updatedEntities = new();
    private readonly List<T> _deletedEntities = new();
    private ValiFlow<T>? _valiFlow;
    private readonly Func<T, TProperty> _getId;
    private Func<T, bool>? _cachedNegatedCondition;

    public ValiFlowEvaluator(IEnumerable<T>? initialData = null, ValiFlow<T>? valiFlow = null,
        Func<T, TProperty>? getId = null)
    {
        _inMemoryStore = initialData?.ToList() ?? new List<T>();
        _valiFlow = valiFlow;
        if (getId != null)
        {
            _getId = getId;
        }
        else
        {
            // Cache the PropertyInfo once at construction time — NOT inside the lambda — to avoid
            // repeated reflection calls on every read/write operation.
            var property = typeof(T).GetProperty("Id")
                ?? throw new InvalidOperationException(
                    $"Entity '{typeof(T).Name}' has no 'Id' property. Provide a getId function.");
            _getId = entity => (TProperty)Convert.ChangeType(property.GetValue(entity)!, typeof(TProperty))!;
        }
    }

    #region Methods Read

    public void SetValiFlow(ValiFlow<T> valiFlow)
    {
        _valiFlow = valiFlow ?? throw new ArgumentNullException(nameof(valiFlow));
        _cachedNegatedCondition = null;
    }

    private Func<T, bool> GetDefaultCondition(ValiFlow<T>? valiFlow = null, bool negated = false)
    {
        ValiFlow<T> selectedValiFlow = valiFlow ?? _valiFlow ?? new ValiFlow<T>();
        if (!negated) return selectedValiFlow.BuildCached();
        // Only cache when using the instance-level _valiFlow
        if (valiFlow == null)
            return _cachedNegatedCondition ??= selectedValiFlow.BuildNegated().Compile();
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

    /// <remarks>
    /// When <paramref name="negateCondition"/> is <see langword="false"/> (default), returns entities that do not satisfy
    /// the Vali-Flow condition. When <see langword="true"/>, returns entities that do satisfy the condition
    /// (effectively equivalent to the non-Failed variant).
    /// <para>
    /// In detail: when <paramref name="negateCondition"/> is <c>false</c> (default), returns the first entity
    /// that does <b>not</b> satisfy the filter — i.e., the first "failed" entity.
    /// When <paramref name="negateCondition"/> is <c>true</c>, the logic is inverted and returns
    /// the first entity that <b>does</b> satisfy the filter.
    /// </para>
    /// </remarks>
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

    /// <remarks>
    /// When <paramref name="negateCondition"/> is <see langword="false"/> (default), returns entities that do not satisfy
    /// the Vali-Flow condition. When <see langword="true"/>, returns entities that do satisfy the condition
    /// (effectively equivalent to the non-Failed variant).
    /// <para>
    /// In detail: when <paramref name="negateCondition"/> is <c>false</c> (default), returns all entities
    /// that do <b>not</b> satisfy the filter — i.e., the "failed" entities.
    /// When <paramref name="negateCondition"/> is <c>true</c>, the logic is inverted and returns
    /// all entities that <b>do</b> satisfy the filter.
    /// </para>
    /// </remarks>
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
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> query = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
        return query.Skip((page - ConstantHelper.One) * pageSize).Take(pageSize);
    }

    public PagedResult<T> EvaluatePagedResult<TKey>(
        IEnumerable<T>? entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    )
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        IEnumerable<T> filtered = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
        var list = filtered.ToList();
        int totalCount = list.Count;
        IEnumerable<T> pageItems = list.Skip((page - ConstantHelper.One) * pageSize).Take(pageSize);
        return new PagedResult<T>(pageItems, totalCount, page, pageSize);
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
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
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
        List<T> ordered = ApplyOrdering(dataSource, orderBy, ascending, thenBys).ToList();
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        return ordered.FindIndex(item => condition(item));
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
        List<T> ordered = ApplyOrdering(dataSource, orderBy, ascending, thenBys).ToList();
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        return ordered.FindLastIndex(item => condition(item));
    }

    /// <remarks>
    /// When <paramref name="negateCondition"/> is <c>false</c> (default), returns the last entity
    /// that does <b>not</b> satisfy the filter — i.e., the last "failed" entity.
    /// When <paramref name="negateCondition"/> is <c>true</c>, the logic is inverted and returns
    /// the last entity that <b>does</b> satisfy the filter.
    /// </remarks>
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        TResult sum = TResult.Zero;
        int count = 0;
        foreach (T item in dataSource)
        {
            if (!condition(item)) continue;
            sum += selector(item);
            count++;
        }
        return count == 0 ? 0m : decimal.CreateChecked(sum) / count;
    }

    public TResult EvaluateSum<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
        var groups = new Dictionary<TKey, (TResult Sum, int Count)>();
        foreach (T item in dataSource)
        {
            if (!condition(item)) continue;
            TKey key = keySelector(item);
            TResult val = selector(item);
            if (groups.TryGetValue(key, out var acc))
                groups[key] = (acc.Sum + val, acc.Count + 1);
            else
                groups[key] = (val, 1);
        }
        return groups.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count == 0 ? 0m : decimal.CreateChecked(kv.Value.Sum) / kv.Value.Count);
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
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
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
            .GroupBy(keySelector)
            .Where(g => g.Count() == ConstantHelper.One)
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
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        _addedEntities.Add(entity);
        (entities as List<T>)?.Add(entity);
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
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        var list = entities as List<T>;
        foreach (T entity in entitiesToAdd)
        {
            _addedEntities.Add(entity);
            list?.Add(entity);
        }
    }

    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null)
    {
        List<T> source = entities is List<T> l ? l : (entities?.ToList() ?? _inMemoryStore);
        // Build index O(N) once
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
            indexById[id] = idx; // keep index valid (position unchanged)
        }
        return updated;
    }

    public int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null)
    {
        List<T> source = entities is List<T> l ? l : (entities?.ToList() ?? _inMemoryStore);
        // Build id→entity map O(N) once
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

    public T Upsert(T entity, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        var existing = dataSource.FirstOrDefault(e =>
            EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
        if (existing != null)
        {
            _updatedEntities.Add(entity);
            if (entities is List<T> list)
            {
                var index = list.FindIndex(e =>
                    EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
                if (index >= 0) list[index] = entity;
            }
        }
        else
        {
            _addedEntities.Add(entity);
            (entities as List<T>)?.Add(entity);
        }

        return entity;
    }

    public IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null)
    {
        if (entities != null && entities is not List<T>)
            throw new ArgumentException(
                "External store must be a List<T> to support mutations. Pass null to use the internal store.",
                nameof(entities));
        // Deduplicate: last occurrence of each ID wins
        var deduplicated = entitiesToUpsert
            .GroupBy(e => _getId(e))
            .Select(g => g.Last())
            .ToList();
        var results = new List<T>();
        foreach (var entity in deduplicated)
            results.Add(Upsert(entity, entities));
        return results;
    }

    public int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null)
    {
        IEnumerable<T> dataSource = entities ?? _inMemoryStore;
        var toDelete = dataSource.Where(predicate).ToList();
        foreach (var entity in toDelete)
        {
            _deletedEntities.Add(entity);
            if (entities is List<T> list)
                list.Remove(entity);
        }

        return toDelete.Count;
    }

    public void SaveChanges(IEnumerable<T>? entities = null)
    {
        if (entities is List<T> externalList)
        {
            // Apply pending adds
            var existingIds = new HashSet<TProperty>(externalList.Select(e => _getId(e)!));
            foreach (var entity in _addedEntities)
                if (existingIds.Add(_getId(entity)!))
                    externalList.Add(entity);

            // Apply pending updates using index map O(N+M)
            var indexMap = new Dictionary<TProperty, int>(externalList.Count);
            for (int i = 0; i < externalList.Count; i++)
                indexMap[_getId(externalList[i])!] = i;
            foreach (var entity in _updatedEntities)
                if (indexMap.TryGetValue(_getId(entity)!, out int idx))
                    externalList[idx] = entity;

            // Apply pending deletes with HashSet + RemoveAll
            if (_deletedEntities.Count > 0)
            {
                var deleteIds = new HashSet<TProperty>(_deletedEntities.Select(e => _getId(e)!));
                externalList.RemoveAll(e => deleteIds.Contains(_getId(e)!));
            }

            _addedEntities.Clear();
            _updatedEntities.Clear();
            _deletedEntities.Clear();
            return;
        }
        else if (entities != null)
        {
            // Non-list enumerable passed: just clear pending changes (can't mutate non-List)
            _addedEntities.Clear();
            _updatedEntities.Clear();
            _deletedEntities.Clear();
            return;
        }

        // Apply to internal store (existing behavior)
        foreach (var entity in _addedEntities)
        {
            if (!_inMemoryStore.Contains(entity, new EntityEqualityComparer<T, TProperty>(_getId)))
                _inMemoryStore.Add(entity);
        }

        foreach (var entity in _updatedEntities)
        {
            var index = _inMemoryStore.FindIndex(e =>
                EqualityComparer<TProperty>.Default.Equals(_getId(e), _getId(entity)));
            if (index >= 0)
                _inMemoryStore[index] = entity;
        }

        if (_deletedEntities.Count > 0)
        {
            var deleteIds = new HashSet<TProperty>(_deletedEntities.Select(e => _getId(e)!));
            _inMemoryStore.RemoveAll(e => deleteIds.Contains(_getId(e)!));
        }

        _addedEntities.Clear();
        _updatedEntities.Clear();
        _deletedEntities.Clear();
    }

    #endregion

    #region IQueryReader<T> + IQueryAggregator<T> — provider-agnostic methods

    Task<bool> IQueryReader<T>.EvaluateAnyAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(EvaluateAny(null, filter));

    Task<int> IQueryReader<T>.EvaluateCountAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(EvaluateCount(null, filter));

    Task<T?> IQueryReader<T>.EvaluateGetFirstAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(GetFirst(null, filter));

    Task<T?> IQueryReader<T>.EvaluateGetLastAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(GetLast<object>(null, null, true, null, filter));

    Task<TResult> IQueryAggregator<T>.EvaluateMinAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(EvaluateMin<TResult>(null, selector.Compile(), filter));

    Task<TResult> IQueryAggregator<T>.EvaluateMaxAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(EvaluateMax<TResult>(null, selector.Compile(), filter));

    Task<decimal> IQueryAggregator<T>.EvaluateAverageAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(EvaluateAverage<TResult>(null, selector.Compile(), filter));

    Task<TResult> IQueryAggregator<T>.EvaluateSumAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(EvaluateSum<TResult>(null, selector.Compile(), filter));

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