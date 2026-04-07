using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.InMemory.Classes.Evaluators;

/// <summary>
/// Wraps a synchronous <see cref="ValiFlowEvaluator{T,TProperty}"/> and exposes its results
/// as <see cref="Task{T}"/> for compatibility with async interfaces.
/// All methods complete synchronously — no I/O is performed.
/// </summary>
internal sealed class AsyncInMemoryAdapter<T, TProperty>
    where T : class
    where TProperty : notnull
{
    private static readonly ConcurrentDictionary<string, Delegate> _compiledCache = new();

    private readonly ValiFlowEvaluator<T, TProperty> _evaluator;

    internal AsyncInMemoryAdapter(ValiFlowEvaluator<T, TProperty> evaluator)
        => _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

    private static Func<T, TResult> GetOrCompile<TResult>(Expression<Func<T, TResult>> selector) =>
        (Func<T, TResult>)_compiledCache.GetOrAdd(
            selector.ToString(),
            _ => selector.Compile());

    internal Task<bool> EvaluateAnyAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(_evaluator.EvaluateAny(null, filter));

    internal Task<int> EvaluateCountAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(_evaluator.EvaluateCount(null, filter));

    internal Task<T?> EvaluateGetFirstAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(_evaluator.GetFirst(null, filter));

    internal Task<T?> EvaluateGetLastAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => Task.FromResult(_evaluator.GetLast<object>(null, null, true, null, filter));

    internal Task<TResult> EvaluateMinAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        where TResult : INumber<TResult>
        => Task.FromResult(_evaluator.EvaluateMin<TResult>(null, GetOrCompile(selector), filter));

    internal Task<TResult> EvaluateMaxAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        where TResult : INumber<TResult>
        => Task.FromResult(_evaluator.EvaluateMax<TResult>(null, GetOrCompile(selector), filter));

    internal Task<decimal> EvaluateAverageAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        where TResult : INumber<TResult>
        => Task.FromResult(_evaluator.EvaluateAverage<TResult>(null, GetOrCompile(selector), filter));

    internal Task<TResult> EvaluateSumAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        where TResult : INumber<TResult>
        => Task.FromResult(_evaluator.EvaluateSum<TResult>(null, GetOrCompile(selector), filter));
}
