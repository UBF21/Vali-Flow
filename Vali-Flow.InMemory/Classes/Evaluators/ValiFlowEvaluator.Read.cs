using System.Numerics;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Models;

namespace Vali_Flow.InMemory.Classes.Evaluators;

public partial class ValiFlowEvaluator<T, TProperty>
{
    /// <summary>
    /// Evaluates whether a single entity satisfies the active filter.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <param name="valiFlow">Optional filter override; uses the instance default when <see langword="null"/>.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns><see langword="true"/> if the entity matches the (possibly negated) condition.</returns>
    public bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.Evaluate", tag, typeof(T).Name);
        try
        {
            return GetDefaultCondition(valiFlow, negateCondition)(entity);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Determines whether any entity in the collection satisfies the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns><see langword="true"/> if at least one entity matches.</returns>
    public bool EvaluateAny(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateAny", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Any(GetDefaultCondition(valiFlow, negateCondition));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the count of entities that satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Number of matching entities.</returns>
    public int EvaluateCount(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateCount", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Count(GetDefaultCondition(valiFlow, negateCondition));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the first entity that does NOT satisfy the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>The first non-matching entity, or <see langword="null"/> if none exists.</returns>
    public T? GetFirstFailed(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, string? tag = null)
        => GetFirst(entities, valiFlow, negateCondition: true, tag: tag);

    /// <summary>
    /// Returns the first entity that satisfies the filter.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>The first matching entity, or <see langword="null"/> if none exists.</returns>
    public T? GetFirst(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false, string? tag = null)
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.GetFirst", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.FirstOrDefault(GetDefaultCondition(valiFlow, negateCondition));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        ValiFlow<T>? valiFlow = null,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateAllFailed", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negated: true));
            return ApplyOrdering(query, orderBy, ascending, thenBys);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateAll", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition));
            return ApplyOrdering(query, orderBy, ascending, thenBys);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        ValiFlow<T>? valiFlow = null, bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluatePaged", tag, typeof(T).Name);
        try
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluatePagedResult", tag, typeof(T).Name);
        try
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
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateTop", tag, typeof(T).Name);
        try
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = EvaluateAll(dataSource, orderBy, ascending, thenBys, valiFlow, negateCondition);
            return query.Take(count);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateDistinct", tag, typeof(T).Name);
        try
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(selector)
                .Select(g => g.First());

            return ApplyOrdering(query, orderBy, ascending, thenBys);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateDuplicates", tag, typeof(T).Name);
        try
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(selector)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);

            return ApplyOrdering(query, orderBy, ascending, thenBys);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.GetFirstMatchIndex", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            List<T> ordered = ApplyOrdering(dataSource, orderBy, ascending, thenBys).ToList();
            Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
            return ordered.FindIndex(item => condition(item));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.GetLastMatchIndex", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            List<T> ordered = ApplyOrdering(dataSource, orderBy, ascending, thenBys).ToList();
            Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
            return ordered.FindLastIndex(item => condition(item));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        ValiFlow<T>? valiFlow = null,
        string? tag = null
    ) => GetLast(entities, orderBy, ascending, thenBys, valiFlow, negateCondition: true, tag: tag);

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
        bool negateCondition = false,
        string? tag = null
    )
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.GetLast", tag, typeof(T).Name);
        try
        {
            return EvaluateAll(entities, orderBy, ascending, thenBys, valiFlow, negateCondition).LastOrDefault();
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    ) where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateMin", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector).Min() ?? TResult.Zero;
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    ) where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateMax", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition)).Select(selector).Max() ?? TResult.Zero;
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    ) where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateAverage", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
            return ComputeAverage(dataSource.Where(condition).Select(selector));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    ) where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateSum", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource
                .Where(GetDefaultCondition(valiFlow, negateCondition))
                .Aggregate(TResult.Zero, (acc, item) => acc + selector(item));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
        bool negateCondition = false,
        string? tag = null
    ) where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateAggregate", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource
                .Where(GetDefaultCondition(valiFlow, negateCondition))
                .Select(selector)
                .Aggregate(TResult.Zero, aggregator);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

}
