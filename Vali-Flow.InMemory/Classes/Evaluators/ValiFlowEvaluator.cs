using System.Numerics;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Classes.Stores;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;

namespace Vali_Flow.InMemory.Classes.Evaluators;

/// <summary>
/// Synchronous in-memory evaluator that applies <see cref="ValiFlow{T}"/> conditions to an <see cref="IEnumerable{T}"/> data source.
/// Implements both read and write operations without requiring a database context, making it suitable for unit testing and caching layers.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate. Must be a reference type.</typeparam>
/// <typeparam name="TProperty">The type of the property used as the entity identifier key. Must be non-nullable.</typeparam>
public partial class ValiFlowEvaluator<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
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
}
