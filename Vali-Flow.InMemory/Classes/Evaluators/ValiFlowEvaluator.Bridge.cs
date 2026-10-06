using System.Linq.Expressions;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.InMemory.Classes.Evaluators;

/// <summary>Explicit <see cref="IQueryReader{T}"/>/<see cref="IQueryAggregator{T}"/> implementation — provider-agnostic async surface, delegated to <see cref="AsyncInMemoryAdapter{T,TProperty}"/>.</summary>
public partial class ValiFlowEvaluator<T, TProperty>
{
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
}
