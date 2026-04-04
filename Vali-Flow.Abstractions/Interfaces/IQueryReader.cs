using Vali_Flow.Core.Builder;

namespace Vali_Flow.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic read contract: existence checks and single-entity retrieval.
/// </summary>
/// <remarks>
/// All methods accept an optional <see cref="ValiFlow{T}"/> filter.
/// When <c>null</c>, the evaluator applies no filter (operates on all entities).
/// For numeric aggregation see <see cref="IQueryAggregator{T}"/>.
/// </remarks>
/// <typeparam name="T">The entity / document type.</typeparam>
public interface IQueryReader<T> where T : class
{
    /// <summary>Returns <c>true</c> if at least one entity matches the filter.</summary>
    Task<bool> EvaluateAnyAsync(
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the count of entities matching the filter.</summary>
    Task<int> EvaluateCountAsync(
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the first entity matching the filter, or <c>null</c> if none.</summary>
    Task<T?> EvaluateGetFirstAsync(
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the last entity matching the filter, or <c>null</c> if none.</summary>
    Task<T?> EvaluateGetLastAsync(
        ValiFlow<T>? filter = null,
        CancellationToken cancellationToken = default);
}
