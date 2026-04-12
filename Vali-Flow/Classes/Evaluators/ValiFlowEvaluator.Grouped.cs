using System.Linq.Expressions;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

public sealed partial class ValiFlowEvaluator<T>
{
    /// <summary>
    /// Groups entities matching the specification by the given key and returns a dictionary of lists.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to the list of entities in that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="keySelector"/> is <c>null</c>.</exception>
    /// <remarks>This method loads all matching entities into memory before grouping. Avoid using it on large, unfiltered tables.</remarks>
    public async Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        Func<T, TKey> keySelectorFn = keySelector.Compile();
        IQueryable<T> query = BuildBasicQuery(specification);
        var list = await query.ToListAsync(cancellationToken);
        return list
            .GroupBy(keySelectorFn)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Returns the count of matching entities per group key, computed directly in the database.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its entity count.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="keySelector"/> is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken),
            nameof(EvaluateCountByGroupAsync));
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="int"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="int"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="int"/> sum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, int>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Sum(), nameof(EvaluateSumByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="long"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="long"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="long"/> sum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, long>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Sum(), nameof(EvaluateSumByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="float"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="float"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="float"/> sum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, float>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Sum(), nameof(EvaluateSumByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="double"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="double"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="double"/> sum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, double>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Sum(), nameof(EvaluateSumByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="decimal"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="decimal"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="decimal"/> sum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, decimal>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Sum(), nameof(EvaluateSumByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the minimum of the projected <see cref="int"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="int"/> property to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="int"/> minimum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, int>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Min(), nameof(EvaluateMinByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the minimum of the projected <see cref="long"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="long"/> property to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="long"/> minimum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, long>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Min(), nameof(EvaluateMinByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the minimum of the projected <see cref="float"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="float"/> property to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="float"/> minimum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, float>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Min(), nameof(EvaluateMinByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the minimum of the projected <see cref="double"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="double"/> property to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="double"/> minimum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, double>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Min(), nameof(EvaluateMinByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the minimum of the projected <see cref="decimal"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="decimal"/> property to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="decimal"/> minimum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, decimal>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Min(), nameof(EvaluateMinByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the maximum of the projected <see cref="int"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="int"/> property to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="int"/> maximum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, int>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Max(), nameof(EvaluateMaxByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the maximum of the projected <see cref="long"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="long"/> property to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="long"/> maximum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, long>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Max(), nameof(EvaluateMaxByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the maximum of the projected <see cref="float"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="float"/> property to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="float"/> maximum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, float>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Max(), nameof(EvaluateMaxByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the maximum of the projected <see cref="double"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="double"/> property to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="double"/> maximum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, double>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Max(), nameof(EvaluateMaxByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the maximum of the projected <see cref="decimal"/> property per group key for entities matching the specification.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the <see cref="decimal"/> property to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="decimal"/> maximum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, decimal>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAggregateAsync(BuildBasicQuery(specification), keySelector, selector,
            values => values.Max(), nameof(EvaluateMaxByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns the arithmetic mean of the projected numeric property per group key for entities matching the specification.
    /// The result is always expressed as <see cref="decimal"/> to preserve fractional precision.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <typeparam name="TResult">Numeric type of the projected property.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="selector">Expression that projects the numeric property to average.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to its <see cref="decimal"/> average.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public async Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return await ExecuteGroupAverageAsync(BuildBasicQuery(specification), keySelector, selector,
            nameof(EvaluateAverageByGroupAsync), cancellationToken);
    }

    /// <summary>
    /// Returns groups that contain more than one entity, keyed by the grouping key.
    /// Only keys with duplicate entries are included in the result.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for duplicate detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each duplicate key to the list of entities sharing that key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="keySelector"/> is <c>null</c>.</exception>
    /// <remarks>This method loads all matching entities into memory before grouping. Avoid using it on large, unfiltered tables.</remarks>
    public async Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(async () =>
            {
                var compiled = keySelector.Compile();
                var all = await query.ToListAsync(cancellationToken);
                return all.GroupBy(compiled)
                    .Where(g => g.Count() > 1)
                    .ToDictionary(g => g.Key, g => g.ToList());
            },
            nameof(EvaluateDuplicatesByGroupAsync));
    }

    /// <summary>
    /// Returns groups that contain exactly one entity, keyed by the grouping key.
    /// Only keys with a single unique entity are included in the result.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for uniqueness detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each unique key to its single entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="keySelector"/> is <c>null</c>.</exception>
    /// <remarks>This method loads all matching entities into memory before grouping. For large unfiltered tables, apply a filter in the specification to limit the result set.</remarks>
    public async Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(async () =>
            {
                var compiled = keySelector.Compile();
                var all = await query.ToListAsync(cancellationToken);
                return all.GroupBy(compiled)
                    .Where(g => g.Count() == 1)
                    .ToDictionary(g => g.Key, g => g.First());
            },
            nameof(EvaluateUniquesByGroupAsync));
    }

    /// <summary>
    /// Returns the top-N entities per group key for entities matching the specification.
    /// The per-group limit is controlled by <see cref="Vali_Flow.Interfaces.Specification.IQuerySpecification{T}.Top"/>
    /// on the specification; when not set, defaults to 50.
    /// Ordering defined on the specification is applied before grouping.
    /// </summary>
    /// <typeparam name="TKey">Type of the grouping key.</typeparam>
    /// <param name="specification">Specification that defines the filter, ordering, optional Top, and EF Core query hints.</param>
    /// <param name="keySelector">Expression that projects the key used for grouping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping each key to the top-N entities in that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="keySelector"/> is <c>null</c>.</exception>
    /// <remarks>
    /// All matching entities are loaded into memory before grouping and top-N selection. Use a targeted query with explicit filters to limit the result set.
    /// <para>
    /// If <see cref="Vali_Flow.Interfaces.Specification.IQuerySpecification{T}.Top"/> is not set, defaults to 50 entities per group.
    /// Set <c>Top</c> on the specification to control the per-group limit.
    /// </para>
    /// </remarks>
    public async Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        int top = specification.Top ?? Constants.Fifty;
        Func<T, TKey> keySelectorFn = keySelector.Compile();
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);

        List<T> items = await ExecuteWithExceptionHandlingAsync(
            () => query.ToListAsync(cancellationToken),
            nameof(EvaluateTopByGroupAsync));
        return items
            .GroupBy(keySelectorFn)
            .ToDictionary(g => g.Key, g => g.Take(top).ToList());
    }
}
