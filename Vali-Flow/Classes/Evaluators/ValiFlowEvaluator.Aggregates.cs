using System.Linq.Expressions;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Evaluators;

public sealed partial class ValiFlowEvaluator<T>
{
    /// <summary>
    /// Returns the minimum value of the projected property for entities matching the specification.
    /// Returns <see cref="TResult.Zero"/> if the result set is empty.
    /// </summary>
    /// <typeparam name="TResult">Numeric type of the projected property.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the property to minimize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The minimum projected value, or <see cref="TResult.Zero"/> if the set is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<TResult> EvaluateMinAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => ExecuteNumericAggAsync(query, selector,
                (q, s, ct) => q.MinAsync(s, ct),
                (q, s, ct) => q.MinAsync(s, ct),
                (q, s, ct) => q.MinAsync(s, ct),
                (q, s, ct) => q.MinAsync(s, ct),
                (q, s, ct) => q.MinAsync(s, ct),
                values => values.Min()!,
                cancellationToken),
            nameof(EvaluateMinAsync));
    }

    /// <summary>
    /// Returns the maximum value of the projected property for entities matching the specification.
    /// Returns <see cref="TResult.Zero"/> if the result set is empty.
    /// </summary>
    /// <typeparam name="TResult">Numeric type of the projected property.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the property to maximize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The maximum projected value, or <see cref="TResult.Zero"/> if the set is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<TResult> EvaluateMaxAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => ExecuteNumericAggAsync(query, selector,
                (q, s, ct) => q.MaxAsync(s, ct),
                (q, s, ct) => q.MaxAsync(s, ct),
                (q, s, ct) => q.MaxAsync(s, ct),
                (q, s, ct) => q.MaxAsync(s, ct),
                (q, s, ct) => q.MaxAsync(s, ct),
                values => values.Max()!,
                cancellationToken),
            nameof(EvaluateMaxAsync));
    }

    /// <summary>
    /// Returns the arithmetic mean of the projected property for entities matching the specification.
    /// Computation is performed in memory using <see cref="decimal"/> precision to avoid floating-point rounding errors.
    /// Returns <c>0</c> if the result set is empty.
    /// </summary>
    /// <typeparam name="TResult">Numeric type of the projected property.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the property to average.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The average as <see cref="decimal"/>, or <c>0</c> if the set is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<decimal> EvaluateAverageAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        List<TResult> values = await ExecuteWithExceptionHandlingAsync(
            () => query.Select(selector).ToListAsync(cancellationToken),
            nameof(EvaluateAverageAsync));
        if (values.Count == 0) return 0m;
        // Use decimal.CreateChecked per value to preserve fractional precision for float/double columns
        decimal total = values.Aggregate(0m, (acc, x) => acc + decimal.CreateChecked(x));
        return total / values.Count;
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="int"/> property for entities matching the specification.
    /// Delegates computation to the database via EF Core.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the <see cref="int"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sum as <see cref="int"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<int> EvaluateSumAsync(
        IBasicSpecification<T> specification,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).SumAsync(cancellationToken),
            nameof(EvaluateSumAsync));
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="long"/> property for entities matching the specification.
    /// Delegates computation to the database via EF Core.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the <see cref="long"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sum as <see cref="long"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<long> EvaluateSumAsync(
        IBasicSpecification<T> specification,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).SumAsync(cancellationToken),
            nameof(EvaluateSumAsync));
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="double"/> property for entities matching the specification.
    /// Delegates computation to the database via EF Core.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the <see cref="double"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sum as <see cref="double"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<double> EvaluateSumAsync(
        IBasicSpecification<T> specification,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).SumAsync(cancellationToken),
            nameof(EvaluateSumAsync));
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="decimal"/> property for entities matching the specification.
    /// Delegates computation to the database via EF Core.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the <see cref="decimal"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sum as <see cref="decimal"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<decimal> EvaluateSumAsync(
        IBasicSpecification<T> specification,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).SumAsync(cancellationToken),
            nameof(EvaluateSumAsync));
    }

    /// <summary>
    /// Returns the sum of the projected <see cref="float"/> property for entities matching the specification.
    /// Delegates computation to the database via EF Core.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the <see cref="float"/> property to sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sum as <see cref="float"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    public async Task<float> EvaluateSumAsync(
        IBasicSpecification<T> specification,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).SumAsync(cancellationToken),
            nameof(EvaluateSumAsync));
    }

    /// <summary>
    /// Applies a custom binary aggregator over the projected property for entities matching the specification.
    /// Returns <see cref="TResult.Zero"/> if the result set is empty.
    /// </summary>
    /// <typeparam name="TResult">Numeric type of the projected property and aggregation result.</typeparam>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the property to aggregate.</param>
    /// <param name="aggregator">Binary function applied cumulatively over the projected values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated result, or <see cref="TResult.Zero"/> if the set is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/>, <paramref name="selector"/>, or <paramref name="aggregator"/> is <c>null</c>.</exception>
    /// <remarks>All matching values are loaded into memory to apply the custom aggregator. For standard aggregations (Sum, Min, Max, Avg), prefer the dedicated methods which delegate computation to the database.</remarks>
    public async Task<TResult> EvaluateAggregateAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));
        IQueryable<T> query = BuildBasicQuery(specification);
        IEnumerable<TResult> values = await query.Select(selector).ToListAsync(cancellationToken);
        return !values.Any() ? TResult.Zero : values.Aggregate(TResult.Zero, aggregator);
    }

    private async Task<TResult> ExecuteNumericAggAsync<TResult>(
        IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, Expression<Func<T, decimal?>>, CancellationToken, Task<decimal?>> decimalAgg,
        Func<IQueryable<T>, Expression<Func<T, double?>>, CancellationToken, Task<double?>> doubleAgg,
        Func<IQueryable<T>, Expression<Func<T, float?>>, CancellationToken, Task<float?>> floatAgg,
        Func<IQueryable<T>, Expression<Func<T, long?>>, CancellationToken, Task<long?>> longAgg,
        Func<IQueryable<T>, Expression<Func<T, int?>>, CancellationToken, Task<int?>> intAgg,
        Func<IEnumerable<TResult>, TResult> inMemoryFallback,
        CancellationToken cancellationToken
    ) where TResult : INumber<TResult>
    {
        // Use nullable overloads so EF Core returns null on empty sequences — avoids a separate AnyAsync roundtrip.
        if (selector is Expression<Func<T, decimal>> decSel)
        {
            var nullableSel = Expression.Lambda<Func<T, decimal?>>(
                Expression.Convert(decSel.Body, typeof(decimal?)), decSel.Parameters);
            var result = await decimalAgg(query, nullableSel, cancellationToken);
            return result.HasValue ? (TResult)(object)result.Value : TResult.Zero;
        }
        if (selector is Expression<Func<T, double>> dblSel)
        {
            var nullableSel = Expression.Lambda<Func<T, double?>>(
                Expression.Convert(dblSel.Body, typeof(double?)), dblSel.Parameters);
            var result = await doubleAgg(query, nullableSel, cancellationToken);
            return result.HasValue ? (TResult)(object)result.Value : TResult.Zero;
        }
        if (selector is Expression<Func<T, float>> fltSel)
        {
            var nullableSel = Expression.Lambda<Func<T, float?>>(
                Expression.Convert(fltSel.Body, typeof(float?)), fltSel.Parameters);
            var result = await floatAgg(query, nullableSel, cancellationToken);
            return result.HasValue ? (TResult)(object)result.Value : TResult.Zero;
        }
        if (selector is Expression<Func<T, long>> lngSel)
        {
            var nullableSel = Expression.Lambda<Func<T, long?>>(
                Expression.Convert(lngSel.Body, typeof(long?)), lngSel.Parameters);
            var result = await longAgg(query, nullableSel, cancellationToken);
            return result.HasValue ? (TResult)(object)result.Value : TResult.Zero;
        }
        if (selector is Expression<Func<T, int>> intSel)
        {
            var nullableSel = Expression.Lambda<Func<T, int?>>(
                Expression.Convert(intSel.Body, typeof(int?)), intSel.Parameters);
            var result = await intAgg(query, nullableSel, cancellationToken);
            return result.HasValue ? (TResult)(object)result.Value : TResult.Zero;
        }
        // Fallback: project and compute in memory
        var values = await query.Select(selector).ToListAsync(cancellationToken);
        return values.Count == 0 ? TResult.Zero : inMemoryFallback(values);
    }
}
