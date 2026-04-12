using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Classes.Stores;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;
using Vali_Flow.InMemory.Models;

namespace Vali_Flow.InMemory.Classes.Evaluators;

/// <summary>
/// Synchronous in-memory evaluator that applies <see cref="ValiFlow{T}"/> conditions to an <see cref="IEnumerable{T}"/> data source.
/// Implements both read and write operations without requiring a database context, making it suitable for unit testing and caching layers.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate. Must be a reference type.</typeparam>
/// <typeparam name="TProperty">The type of the property used as the entity identifier key. Must be non-nullable.</typeparam>
public sealed class ValiFlowEvaluator<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
    where T : class
    where TProperty : notnull
{
    private readonly InMemoryWriteStore<T, TProperty> _store;
    private readonly object _stateLock = new();
    private volatile ValiFlow<T>? _valiFlow;
    private volatile Func<T, bool>? _cachedNegatedCondition;
    private readonly AsyncInMemoryAdapter<T, TProperty> _asyncAdapter;
    private AsyncInMemoryAdapter<T, TProperty> AsyncAdapter => _asyncAdapter;

    /// <summary>
    /// Initializes a new instance of <see cref="ValiFlowEvaluator{T, TProperty}"/>.
    /// </summary>
    /// <param name="initialData">Optional seed collection to populate the internal store.</param>
    /// <param name="valiFlow">Optional default filter applied when no explicit filter is supplied to evaluation methods.</param>
    /// <param name="getId">Optional key selector used by the write store to identify entities.</param>
    public ValiFlowEvaluator(IEnumerable<T>? initialData = null, ValiFlow<T>? valiFlow = null,
        Func<T, TProperty>? getId = null)
    {
        _store = new InMemoryWriteStore<T, TProperty>(initialData, getId);
        _valiFlow = valiFlow;
        _asyncAdapter = new AsyncInMemoryAdapter<T, TProperty>(this);
    }

    #region Methods Read

    /// <summary>
    /// Replaces the default filter and invalidates the cached negated condition.
    /// </summary>
    /// <param name="valiFlow">The new filter to set as default.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="valiFlow"/> is <see langword="null"/>.</exception>
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
        if (!negated)
        {
            ValiFlow<T>? current;
            lock (_stateLock) { current = _valiFlow; }
            var flow = valiFlow ?? current ?? new ValiFlow<T>();
            return flow.BuildCached();
        }

        if (valiFlow != null)
            return valiFlow.BuildNegated().Compile();

        lock (_stateLock)
        {
            if (_cachedNegatedCondition != null) return _cachedNegatedCondition;
            var flow = _valiFlow ?? new ValiFlow<T>();
            _cachedNegatedCondition = flow.BuildNegated().Compile();
            return _cachedNegatedCondition;
        }
    }

    /// <summary>
    /// Evaluates whether a single entity satisfies the active filter.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <param name="valiFlow">Optional filter override; uses the instance default when <see langword="null"/>.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns><see langword="true"/> if the entity matches the (possibly negated) condition.</returns>
    public bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        return GetDefaultCondition(valiFlow, negateCondition)(entity);
    }

    /// <summary>
    /// Determines whether any entity in the collection satisfies the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns><see langword="true"/> if at least one entity matches.</returns>
    public bool EvaluateAny(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Any(GetDefaultCondition(valiFlow, negateCondition));
    }

    /// <summary>
    /// Returns the count of entities that satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Number of matching entities.</returns>
    public int EvaluateCount(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.Count(GetDefaultCondition(valiFlow, negateCondition));
    }

    /// <summary>
    /// Returns the first entity that does NOT satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <returns>The first non-matching entity, or <see langword="null"/> if none exists.</returns>
    public T? GetFirstFailed(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null)
        => GetFirst(entities, valiFlow, negateCondition: true);

    /// <summary>
    /// Returns the first entity that satisfies the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>The first matching entity, or <see langword="null"/> if none exists.</returns>
    public T? GetFirst(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
    {
        IEnumerable<T> dataSource = entities ?? _store.Items;
        return dataSource.FirstOrDefault(GetDefaultCondition(valiFlow, negateCondition));
    }

    /// <summary>
    /// Returns all entities that do NOT satisfy the filter, with optional ordering.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <returns>Ordered sequence of non-matching entities.</returns>
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

    /// <summary>
    /// Returns all entities that satisfy the filter, with optional ordering.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Ordered sequence of matching entities.</returns>
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

    /// <summary>
    /// Returns a paged subset of entities that satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Sequence of entities for the requested page.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="page"/> or <paramref name="pageSize"/> is less than 1.</exception>
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

    /// <summary>
    /// Returns a <see cref="PagedResult{T}"/> containing the paged data and total count.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>A <see cref="PagedResult{T}"/> with the current page items and the total filtered count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="page"/> or <paramref name="pageSize"/> is less than 1.</exception>
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
        var filtered = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).ToList();
        int totalCount = filtered.Count;
        IEnumerable<T> ordered = ApplyOrdering(filtered, orderBy, ascending, thenBys);
        List<T> pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>(pageItems, totalCount, page, pageSize);
    }

    /// <summary>
    /// Returns the top <paramref name="count"/> entities that satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="count">Maximum number of entities to return.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Sequence of up to <paramref name="count"/> matching entities.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than or equal to zero.</exception>
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

    /// <summary>
    /// Returns distinct entities based on a key selector, with optional ordering.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Key selector used to determine uniqueness.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Sequence of distinct matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Returns all entities whose key appears more than once in the filtered collection.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Key selector used to detect duplicates.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>All entities belonging to duplicate groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Returns the zero-based index of the first entity that satisfies the filter after ordering.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="orderBy">Primary key selector for ordering before index lookup.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Zero-based index of the first match, or <c>-1</c> if not found.</returns>
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

    /// <summary>
    /// Returns the zero-based index of the last entity that satisfies the filter after ordering.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="orderBy">Primary key selector for ordering before index lookup.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Zero-based index of the last match, or <c>-1</c> if not found.</returns>
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

    /// <summary>
    /// Returns the last entity that does NOT satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <returns>The last non-matching entity, or <see langword="null"/> if none exists.</returns>
    public T? GetLastFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
    ) => GetLast(entities, orderBy, ascending, thenBys, valiFlow, negateCondition: true);

    /// <summary>
    /// Returns the last entity that satisfies the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="orderBy">Primary key selector for ordering.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="thenBys">Optional secondary sort criteria.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>The last matching entity, or <see langword="null"/> if none exists.</returns>
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

    /// <summary>
    /// Returns the minimum value of a numeric property across all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Property selector for the numeric value.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Minimum value, or <c>TResult.Zero</c> when no entities match.</returns>
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

    /// <summary>
    /// Returns the maximum value of a numeric property across all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Property selector for the numeric value.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Maximum value, or <c>TResult.Zero</c> when no entities match.</returns>
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

    /// <summary>
    /// Returns the average value of a numeric property across all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Property selector for the numeric value.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Average as <see cref="decimal"/>, or <c>0</c> when no entities match.</returns>
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

    /// <summary>
    /// Returns the sum of a numeric property across all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Property selector for the numeric value.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Sum of the selected values, or <c>TResult.Zero</c> when no entities match.</returns>
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

    /// <summary>
    /// Applies a custom aggregation function over a numeric property of all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="selector">Property selector for the numeric value.</param>
    /// <param name="aggregator">Binary accumulator function applied to successive values.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Aggregated result, starting from <c>TResult.Zero</c>.</returns>
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

    /// <summary>
    /// Groups matching entities by a key selector and returns a dictionary of lists.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to its group of entities.</returns>
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

    /// <summary>
    /// Returns the count of matching entities per group key.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to its entity count.</returns>
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

    /// <summary>
    /// Returns the sum of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to sum.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to the sum of the selected property.</returns>
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

    /// <summary>
    /// Returns the minimum value of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to minimize.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to the minimum of the selected property, or <c>TResult.Zero</c> for empty groups.</returns>
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

    /// <summary>
    /// Returns the maximum value of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to maximize.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to the maximum of the selected property, or <c>TResult.Zero</c> for empty groups.</returns>
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

    /// <summary>
    /// Returns the average of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to average.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to the <see cref="decimal"/> average of the selected property.</returns>
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

    /// <summary>
    /// Returns groups where the key appears more than once among matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to detect duplicates.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary containing only groups with more than one element.</returns>
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

    /// <summary>
    /// Returns groups where the key appears exactly once among matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to identify unique groups.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary containing only singleton groups, mapping each key to its single entity.</returns>
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
            .Where(g => !g.Skip(1).Any())
            .ToDictionary(g => g.Key, g => g.First());
    }

    /// <summary>
    /// Returns the top <paramref name="count"/> entities per group key, with optional ordering within each group.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="count">Maximum number of entities to include per group.</param>
    /// <param name="orderBy">Optional ordering key applied before grouping.</param>
    /// <param name="ascending">When <see langword="true"/>, orders ascending; otherwise descending.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <returns>Dictionary mapping each key to its top entities.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than or equal to zero.</exception>
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
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
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
            if (inMemoryThenBys.Count > 0)
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

    /// <summary>Adds a single entity to the store or the supplied list.</summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns><see langword="true"/> if the entity was added successfully.</returns>
    public bool Add(T entity, List<T>? entities = null) => _store.Add(entity, entities);

    /// <summary>Updates a matching entity in the store or the supplied list.</summary>
    /// <param name="entity">Entity with updated values; matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns>The updated entity, or <see langword="null"/> if no matching entity was found.</returns>
    public T? Update(T entity, List<T>? entities = null) => _store.Update(entity, entities);

    /// <summary>Removes a matching entity from the store or the supplied list.</summary>
    /// <param name="entity">Entity to remove; matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns><see langword="true"/> if the entity was found and removed.</returns>
    public bool Delete(T entity, List<T>? entities = null) => _store.Delete(entity, entities);

    /// <summary>Adds multiple entities to the store or the supplied list.</summary>
    /// <param name="entitiesToAdd">Entities to add.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    public void AddRange(IEnumerable<T> entitiesToAdd, List<T>? entities = null) =>
        _store.AddRange(entitiesToAdd, entities);

    /// <summary>Updates multiple matching entities in the store or the supplied list.</summary>
    /// <param name="entitiesToUpdate">Entities with updated values; each matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns>Sequence of successfully updated entities.</returns>
    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, List<T>? entities = null) =>
        _store.UpdateRange(entitiesToUpdate, entities);

    /// <summary>Removes multiple matching entities from the store or the supplied list.</summary>
    /// <param name="entitiesToDelete">Entities to remove; each matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns>Number of entities removed.</returns>
    public int DeleteRange(IEnumerable<T> entitiesToDelete, List<T>? entities = null) =>
        _store.DeleteRange(entitiesToDelete, entities);

    /// <summary>Inserts or updates a single entity in the store or the supplied list.</summary>
    /// <param name="entity">Entity to insert if not found, or update if already present (matched by key selector).</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns>The inserted or updated entity.</returns>
    public T Upsert(T entity, List<T>? entities = null) => _store.Upsert(entity, entities);

    /// <summary>Inserts or updates multiple entities in the store or the supplied list.</summary>
    /// <param name="entitiesToUpsert">Entities to insert or update; each matched by the configured key selector.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns>Sequence of inserted or updated entities.</returns>
    public IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, List<T>? entities = null) =>
        _store.UpsertRange(entitiesToUpsert, entities);

    /// <summary>Removes all entities that satisfy an arbitrary predicate.</summary>
    /// <param name="predicate">Condition an entity must meet to be removed.</param>
    /// <param name="entities">Optional external list to operate on instead of the internal store.</param>
    /// <returns>Number of entities removed.</returns>
    public int DeleteByCondition(Func<T, bool> predicate, List<T>? entities = null) =>
        _store.DeleteByCondition(predicate, entities);

    /// <summary>Persists pending changes to the internal store or flushes the supplied list.</summary>
    /// <param name="entities">Optional external list whose changes should be committed.</param>
    public void SaveChanges(List<T>? entities = null) => _store.SaveChanges(entities);

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
