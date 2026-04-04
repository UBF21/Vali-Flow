using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic numeric aggregation contract.
/// </summary>
/// <remarks>
/// All methods accept an optional <see cref="ValiFlow{T}"/> filter.
/// For existence checks and single-entity retrieval see <see cref="IQueryReader{T}"/>.
/// </remarks>
/// <typeparam name="T">The entity / document type.</typeparam>
public interface IQueryAggregator<T> where T : class
{
    /// <summary>Returns the minimum value of <paramref name="selector"/> for entities matching the filter.</summary>
    Task<TResult> EvaluateMinAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default)
        where TResult : INumber<TResult>;

    /// <summary>Returns the maximum value of <paramref name="selector"/> for entities matching the filter.</summary>
    Task<TResult> EvaluateMaxAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default)
        where TResult : INumber<TResult>;

    /// <summary>Returns the average of <paramref name="selector"/> for entities matching the filter, or <c>0</c> if empty.</summary>
    Task<decimal> EvaluateAverageAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default)
        where TResult : INumber<TResult>;

    /// <summary>Returns the sum of <paramref name="selector"/> for entities matching the filter.</summary>
    Task<TResult> EvaluateSumAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default)
        where TResult : INumber<TResult>;
}
