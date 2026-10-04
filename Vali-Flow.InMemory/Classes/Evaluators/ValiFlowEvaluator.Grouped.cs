using System.Numerics;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.InMemory.Classes.Evaluators;

public partial class ValiFlowEvaluator<T, TProperty>
{
    /// <summary>
    /// Groups matching entities by a key selector and returns a dictionary of lists.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to its group of entities.</returns>
    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateGrouped", tag, typeof(T).Name);
        try
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
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the count of matching entities per group key.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to its entity count.</returns>
    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateCountByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the sum of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to sum.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to the sum of the selected property.</returns>
    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateSumByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g =>
                    g.Aggregate(TResult.Zero, (acc, item) => acc + selector(item)));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the minimum value of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to minimize.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to the minimum of the selected property, or <c>TResult.Zero</c> for empty groups.</returns>
    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateMinByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Select(selector).Min() ?? TResult.Zero);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the maximum value of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to maximize.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to the maximum of the selected property, or <c>TResult.Zero</c> for empty groups.</returns>
    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateMaxByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Select(selector).Max() ?? TResult.Zero);
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns the average of a numeric property per group key for all matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to group entities.</param>
    /// <param name="selector">Property selector for the numeric value to average.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to the <see cref="decimal"/> average of the selected property.</returns>
    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateAverageByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            Func<T, bool> condition = GetDefaultCondition(valiFlow, negateCondition);
            return dataSource.Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => ComputeAverage(g.Select(selector)));
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns groups where the key appears more than once among matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to detect duplicates.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary containing only groups with more than one element.</returns>
    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateDuplicatesByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(keySelector)
                .Where(g => g.Skip(1).Any())
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }

    /// <summary>
    /// Returns groups where the key appears exactly once among matching entities.
    /// </summary>
    /// <param name="entities">Source collection; falls back to the internal store when <see langword="null"/>.</param>
    /// <param name="keySelector">Key selector used to identify unique groups.</param>
    /// <param name="valiFlow">Optional filter override.</param>
    /// <param name="negateCondition">When <see langword="true"/>, the filter predicate is logically negated.</param>
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary containing only singleton groups, mapping each key to its single entity.</returns>
    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateUniquesByGroup", tag, typeof(T).Name);
        try
        {
            IEnumerable<T> dataSource = entities ?? _store.Items;
            return dataSource.Where(GetDefaultCondition(valiFlow, negateCondition))
                .GroupBy(keySelector)
                .Where(g => !g.Skip(1).Any())
                .ToDictionary(g => g.Key, g => g.First());
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
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
    /// <param name="tag">Optional diagnostics tag identifying why/where this operation was triggered.</param>
    /// <returns>Dictionary mapping each key to its top entities.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than or equal to zero.</exception>
    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey, TOrderKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, TOrderKey>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false,
        string? tag = null
    ) where TKey : notnull
    {
        using var activity = ValiFlowDiagnostics.StartActivity("Vali-Flow.InMemory.EvaluateTopByGroup", tag, typeof(T).Name);
        try
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
            IEnumerable<T> dataSource = entities ?? _store.Items;
            IEnumerable<T> query = dataSource.Where(GetDefaultCondition(valiFlow, negateCondition));
            if (orderBy != null) query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

            return query.GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Take(count).ToList());
        }
        catch (Exception ex)
        {
            ValiFlowDiagnostics.RecordException(activity, ex);
            throw;
        }
    }
}
