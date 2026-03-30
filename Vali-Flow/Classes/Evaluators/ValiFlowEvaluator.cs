using System.Linq.Expressions;
using System.Numerics
    ;
using System.Reflection;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Models;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

public sealed class ValiFlowEvaluator<T> : IEvaluatorRead<T>, IEvaluatorWrite<T> where T : class
{
    private readonly DbContext _dbContext;

    public ValiFlowEvaluator(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "The DbContext provided is null.");
    }

    #region Methods Read

    public Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity)
    {
        if (valiFlow == null) throw new ArgumentNullException(nameof(valiFlow));
        Validation.ValidateEntityNotNull(entity);
        var condition = valiFlow.BuildCached();
        return Task.FromResult(condition(entity));
    }

    public async Task<bool> EvaluateAnyAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.AnyAsync(cancellationToken),
            nameof(EvaluateAnyAsync));
    }

    public async Task<int> EvaluateCountAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.CountAsync(cancellationToken),
            nameof(EvaluateCountAsync));
    }

    public async Task<T?> EvaluateGetFirstFailedAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        var query = BuildBasicQuery(specification, true);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstFailedAsync));
    }

    public async Task<T?> EvaluateGetFirstAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstAsync));
    }

    public Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildQuery(specification, negateFilter: true);
        return Task.FromResult(query);
    }

    public Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildQuery(specification);
        return Task.FromResult(query);
    }

    public Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);
        query = query.GroupBy(selector).Select(g => g.First());
        return Task.FromResult(ApplyPagination(query, specification));
    }

    public Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);
        query = query.GroupBy(selector).Where(g => g.Count() > Constants.One).SelectMany(g => g);
        return Task.FromResult(ApplyPagination(query, specification));
    }

    public async Task<T?> EvaluateGetLastFailedAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (specification.OrderBy == null && specification.ValiSort == null)
            throw new InvalidOperationException(
                $"{nameof(EvaluateGetLastFailedAsync)} requires an ordering (OrderBy or ValiSort). EF Core cannot translate LastOrDefault without ORDER BY.");

        IQueryable<T> query = BuildQuery(specification, negateFilter: true);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastFailedAsync));
    }

    public async Task<T?> EvaluateGetLastAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (specification.OrderBy == null && specification.ValiSort == null)
            throw new InvalidOperationException(
                $"{nameof(EvaluateGetLastAsync)} requires an ordering (OrderBy or ValiSort). EF Core cannot translate LastOrDefault without ORDER BY.");

        IQueryable<T> query = BuildQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastAsync));
    }

    public async Task<TResult> EvaluateMinAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).MinAsync(cancellationToken),
            nameof(EvaluateMinAsync));
    }

    public async Task<TResult> EvaluateMaxAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.Select(selector).MaxAsync(cancellationToken),
            nameof(EvaluateMaxAsync));
    }

    public async Task<decimal> EvaluateAverageAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query.Select(selector).AverageAsync(x => Convert.ToDecimal(x), cancellationToken),
            nameof(EvaluateAverageAsync));
    }

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

    public async Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() =>
        {
            return query
                .GroupBy(keySelector)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);
        }, nameof(EvaluateGroupedAsync));
    }

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
            values => (float)values.Sum(v => (double)v), nameof(EvaluateSumByGroupAsync), cancellationToken);
    }

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

    public async Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
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
                .Where(g => g.Count() > 1)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken),
            nameof(EvaluateDuplicatesByGroupAsync));
    }

    public async Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
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
                .Where(g => g.Count() == 1)
                .Select(g => new { g.Key, First = g.First() })
                .ToDictionaryAsync(x => x.Key, x => x.First, cancellationToken),
            nameof(EvaluateUniquesByGroupAsync));
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        int top = specification.Top ?? Constants.Fifty;
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);

        List<T> items = await ExecuteWithExceptionHandlingAsync(
            () => query.ToListAsync(cancellationToken),
            nameof(EvaluateTopByGroupAsync));

        Func<T, TKey> keySelectorFn = keySelector.Compile();
        return items
            .GroupBy(keySelectorFn)
            .ToDictionary(g => g.Key, g => g.Take(top).ToList());
    }

    public async Task<PagedResult<T>> EvaluatePagedAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        // Fix #6: require explicit Page and PageSize — silent defaults caused surprising results
        if (!specification.Page.HasValue)
            throw new ArgumentException(
                $"{nameof(EvaluatePagedAsync)} requires Page to be explicitly set on the specification.",
                nameof(specification));
        if (!specification.PageSize.HasValue)
            throw new ArgumentException(
                $"{nameof(EvaluatePagedAsync)} requires PageSize to be explicitly set on the specification.",
                nameof(specification));

        // Fix #2: pagination without ordering is non-deterministic
        bool hasOrdering = specification.OrderBy != null || specification.ValiSort != null;
        if (!hasOrdering)
            throw new InvalidOperationException(
                $"{nameof(EvaluatePagedAsync)} requires an ordering (OrderBy or ValiSort) for deterministic pagination results.");

        int page = specification.Page.Value;
        int pageSize = specification.PageSize.Value;

        IQueryable<T> baseQuery = BuildBasicQuery(specification);

        int totalCount = await ExecuteWithExceptionHandlingAsync(
            () => baseQuery.CountAsync(cancellationToken),
            nameof(EvaluatePagedAsync));

        IQueryable<T> orderedQuery = ApplyOrdering(baseQuery, specification);
        int skip = (page - Constants.One) * pageSize;
        IList<T> items = await ExecuteWithExceptionHandlingAsync(
            () => orderedQuery.Skip(skip).Take(pageSize).ToListAsync(cancellationToken),
            nameof(EvaluatePagedAsync));

        return new PagedResult<T>(items.AsReadOnly(), totalCount, page, pageSize);
    }

    #endregion

    #region Methods Write

    public async Task<T> AddAsync(
        T entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);

        var addedEntity = await ExecuteWithExceptionHandlingAsync(
            async () => (await _dbContext.Set<T>().AddAsync(entity, cancellationToken)).Entity,
            nameof(AddAsync));

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(AddAsync));
        return addedEntity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync(
        IEnumerable<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.Set<T>().AddRangeAsync(entityList, cancellationToken);
                return entityList;
            }, nameof(AddRangeAsync));

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(AddRangeAsync));
        return entityList;
    }

    public async Task<T> UpdateAsync(
        T entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);
        _dbContext.Set<T>().Update(entity);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpdateAsync));
        return entity;
    }

    public async Task<IEnumerable<T>> UpdateRangeAsync(
        IEnumerable<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        _dbContext.Set<T>().UpdateRange(entityList);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpdateRangeAsync));
        return entityList;
    }

    public async Task DeleteAsync(
        T entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);
        _dbContext.Set<T>().Remove(entity);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(DeleteAsync));
    }

    public async Task DeleteRangeAsync(
        IEnumerable<T> entities,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        _dbContext.Set<T>().RemoveRange(entityList);

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(DeleteRangeAsync));
    }


    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteWithExceptionHandlingAsync(() => _dbContext.SaveChangesAsync(cancellationToken),
            nameof(SaveChangesAsync));
    }

    public async Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        Validation.ValidateEntityNotNull(entity);
        if (matchCondition == null) throw new ArgumentNullException(nameof(matchCondition));
        T? existingEntity = await _dbContext.Set<T>().FirstOrDefaultAsync(matchCondition, cancellationToken);

        if (existingEntity == null)
        {
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        }

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpsertAsync));
        return entity;
    }

    public async Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Expression<Func<T, TProperty>> keySelector,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        // Compile keySelector once for in-memory use
        var keySelectorFn = keySelector.Compile();

        // Materialize keys as a concrete List<TProperty> — EF can translate .Contains() on a local collection
        List<TProperty> keys = entityList.Select(keySelectorFn).ToList();

        // Build a predicate that EF can translate: e => keys.Contains(e.Prop)
        var param = keySelector.Parameters[0];
        var body = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TProperty)],
            Expression.Constant(keys),
            keySelector.Body
        );
        var predicate = Expression.Lambda<Func<T, bool>>(body, param);

        IEnumerable<T> existingEntities = await _dbContext.Set<T>()
            .Where(predicate)
            .ToListAsync(cancellationToken);

        Dictionary<TProperty, T> existingEntityDict = existingEntities.ToDictionary(keySelectorFn, e => e);

        foreach (T entity in entityList)
        {
            TProperty key = keySelectorFn(entity);
            if (existingEntityDict.TryGetValue(key, out var existingEntity))
            {
                _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
            }
        }

        await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(UpsertRangeAsync));
        return entityList;
    }


    public async Task DeleteByConditionAsync(
        Expression<Func<T, bool>> condition,
        CancellationToken cancellationToken = default
    )
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        // ExecuteDeleteAsync translates directly to DELETE FROM ... WHERE — no round-trip to load entities
        await ExecuteWithExceptionHandlingAsync(
            () => _dbContext.Set<T>().Where(condition).ExecuteDeleteAsync(cancellationToken),
            nameof(DeleteByConditionAsync));
    }


    public async Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)
    {
        if (operations == null) throw new ArgumentNullException(nameof(operations));
        if (_dbContext.Database.CurrentTransaction != null)
        {
            await operations();
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operations();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                throw new InvalidOperationException(
                    $"Error executing transaction in {nameof(ExecuteTransactionAsync)}. Rollback failed: {rollbackEx.Message}",
                    ex);
            }

            throw new InvalidOperationException(
                $"Error executing transaction in {nameof(ExecuteTransactionAsync)}.",
                ex);
        }
    }

    public async Task BulkInsertAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkInsertAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return Task.CompletedTask;
            },
            nameof(BulkInsertAsync));
    }

    public async Task BulkUpdateAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return Task.CompletedTask;
            },
            nameof(BulkUpdateAsync));
    }

    public async Task BulkDeleteAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkDeleteAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return Task.CompletedTask;
            },
            nameof(BulkDeleteAsync));
    }

    public async Task BulkInsertOrUpdateAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkInsertOrUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return Task.CompletedTask;
            },
            nameof(BulkInsertOrUpdateAsync));
    }

    #endregion

    #region Methods Private

    /// <summary>
    /// Applies the AsNoTracking option to the given query if specified.
    /// </summary>
    /// <typeparam name="T">The entity type of the query.</typeparam>
    /// <param name="query">The IQueryable instance to modify.</param>
    /// <param name="asNoTracking">
    /// A boolean flag indicating whether to apply AsNoTracking. 
    /// If <c>true</c>, AsNoTracking is applied; otherwise, the query remains unchanged.
    /// </param>
    /// <returns>
    /// The modified query with AsNoTracking applied if requested; otherwise, the original query.
    /// </returns>
    private IQueryable<T> ApplyAsNoTracking(IQueryable<T> query, bool asNoTracking = true) => asNoTracking ? query.AsNoTracking() : query;

    /// <summary>
    /// Applies the specified include expressions to an <see cref="IQueryable{T}"/> query.
    /// </summary>
    /// <param name="query">The base query to which the includes will be applied.</param>
    /// <param name="includes">
    /// A collection of include expressions defining related entities to be loaded.
    /// If <c>null</c>, the query remains unchanged.
    /// </param>
    /// <returns>
    /// The modified query with the specified includes applied, or the original query if no includes are provided.
    /// </returns>
    private IQueryable<T> ApplyIncludes(IQueryable<T> query, IEnumerable<IEfInclude<T>>? includes)
    {
        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = include.ApplyInclude(query);
            }
        }

        return query;
    }

    /// <summary>
    /// Applies ordering criteria to the provided query based on the specified query specification.
    /// </summary>
    /// <param name="query">The query to which the ordering criteria will be applied. Cannot be null.</param>
    /// <param name="specification">The query specification containing the ordering criteria. Cannot be null.</param>
    /// <returns>The query with the ordering criteria applied, or the original query if no ordering is specified.</returns>
    /// <remarks>
    /// This method applies the ordering criteria defined in the <paramref name="specification"/> to the query using Entity Framework Core's ordering methods. 
    /// If the <see cref="IQuerySpecification{T}.OrderBy"/> property is specified, it is applied using the <c>ApplyOrderBy</c> method to establish the primary ordering. 
    /// If the <see cref="IQuerySpecification{T}.ThenBys"/> collection is non-empty, each secondary ordering is applied using the <c>ApplyThenBy</c> method to further refine the order. 
    /// If no ordering criteria are specified, the original query is returned unchanged. 
    /// This method is used internally to incorporate the ordering logic defined by the specification into the query pipeline, 
    /// enabling consistent and reusable sorting of query results based on business requirements.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> or <paramref name="specification"/> is null.</exception>
    private IQueryable<T> ApplyOrdering(IQueryable<T> query, IQuerySpecification<T> specification)
    {
        // Fix #4: query is already IQueryable<T> — AsQueryable() was redundant
        if (specification.ValiSort != null)
            return specification.ValiSort.Apply(query);

        // Fix #1: ThenBy without OrderBy is a programming mistake — fail fast
        if (specification.OrderBy == null)
        {
            if (specification.ThenBys != null)
                throw new InvalidOperationException(
                    "ThenBy requires a primary OrderBy. Set OrderBy on the specification before adding ThenBy expressions.");
            return query;
        }

        IOrderedQueryable<T> orderedQuery = specification.OrderBy.ApplyOrderBy(query);

        if (specification.ThenBys != null)
        {
            foreach (var thenBy in specification.ThenBys)
                orderedQuery = thenBy.ApplyThenBy(orderedQuery);
        }

        return orderedQuery;
    }

    private async Task<TValue> ExecuteWithExceptionHandlingAsync<TValue>(Func<Task<TValue>> operation,
        string operationName)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing {operationName} for entity type {typeof(T).Name}.",
                ex);
        }
    }

    /// <summary>
    /// Applies pagination to the query based on the specification's Page, PageSize, and Top properties.
    /// </summary>
    /// <param name="query">The IQueryable instance to modify.</param>
    /// <param name="specification">The specification containing the pagination criteria.</param>
    /// <returns>The modified query with pagination applied.</returns>
    private IQueryable<T> ApplyPagination(IQueryable<T> query, IQuerySpecification<T> specification)
    {
        if (specification is { Page: not null, PageSize: not null })
        {
            int skip = (specification.Page.Value - Constants.One) * specification.PageSize.Value;
            int take = specification.PageSize.Value;
            query = query.Skip(skip).Take(take);
        }
        else if (specification.Top != null)
        {
            query = query.Take(specification.Top.Value);
        }

        return query;
    }

    /// <summary>
    /// Applies the split query option to the provided query if specified.
    /// </summary>
    /// <param name="query">The query to which the split query option will be applied. Cannot be null.</param>
    /// <param name="asSplitQuery">A value indicating whether the query should be executed as a split query. Defaults to <see langword="false"/>.</param>
    /// <returns>The query with the split query option applied if <paramref name="asSplitQuery"/> is <see langword="true"/>; otherwise, the original query.</returns>
    /// <remarks>
    /// This method configures the query to use Entity Framework Core's split query feature, which splits a query with multiple includes into separate SQL queries 
    /// to improve performance by reducing the complexity of the resulting SQL and avoiding large Cartesian products. 
    /// When <paramref name="asSplitQuery"/> is <see langword="true"/>, the query is modified with <c>AsSplitQuery()</c>, causing related data 
    /// (e.g., navigation properties included via <see cref="ISpecification{T}.Includes"/>) to be retrieved in separate queries rather than a single joined query.
    /// Use this option when dealing with complex queries involving multiple includes to optimize database performance, but note that it may increase the number of 
    /// database round-trips. This method is typically used internally to apply the <see cref="ISpecification{T}.AsSplitQuery"/> setting from a specification.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    private IQueryable<T> ApplyAsSplitQuery(IQueryable<T> query, bool asSplitQuery = false) => asSplitQuery ? query.AsSplitQuery() : query;
    

    /// <summary>
    /// Builds a minimal query from the specification, applying only the filter, AsNoTracking, Includes, and AsSplitQuery options.
    /// Does not apply ordering or pagination.
    /// </summary>
    /// <param name="specification">The specification containing the filtering and query configuration.</param>
    /// <param name="negateCondition">If true, negates the filter condition; otherwise, applies it as-is.</param>
    /// <returns>The constructed IQueryable.</returns>
    private IQueryable<T> BuildBasicQuery(ISpecification<T> specification, bool negateCondition = false)
    {
        IQueryable<T> query = _dbContext.Set<T>();

        query = ApplyIgnoreQueryFilters(query, specification.IgnoreQueryFilters);
        query = ApplyWhere(query, specification.Filter, negateCondition);
        query = ApplyAsNoTracking(query, specification.AsNoTracking);
        query = ApplyIncludes(query, specification.Includes);
        query = ApplyAsSplitQuery(query, specification.AsSplitQuery);

        return query;
    }

    /// <summary>
    /// Applies a filter condition to the provided query using the specified validation flow.
    /// </summary>
    /// <param name="query">The query to which the filter condition will be applied. Cannot be null.</param>
    /// <param name="filter">The validation flow defining the filter criteria. Cannot be null.</param>
    /// <param name="negateCondition">A value indicating whether the filter condition should be negated. Defaults to <see langword="false"/>.</param>
    /// <returns>The query with the filter condition applied using the <see cref="ValiFlowQuery{T}"/> criteria.</returns>
    /// <remarks>
    /// This method applies the filter criteria defined by the <paramref name="filter"/> to the query using Entity Framework Core's <c>Where</c> method. 
    /// If <paramref name="negateCondition"/> is <see langword="true"/>, the filter condition is negated (e.g., <c>NOT</c> is applied to the criteria), 
    /// allowing for exclusion-based filtering. The <paramref name="filter"/> is typically sourced from a specification's filter property.
    /// This method is used internally to incorporate the filtering logic defined by <see cref="ValiFlowQuery{T}"/> into the query pipeline, 
    /// enabling complex and reusable filter conditions. Use this method to ensure that filter criteria are consistently applied to queries, 
    /// supporting scenarios such as filtering entities based on business rules or excluding specific records when negating conditions.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> or <paramref name="filter"/> is null.</exception>
    private IQueryable<T> ApplyWhere(IQueryable<T> query, ValiFlowQuery<T> filter, bool negateCondition = false)
    {
        Validation.ValidateFilterNotNull(filter);

        Expression<Func<T, bool>> condition = negateCondition ? filter.BuildNegated() : filter.BuildWithGlobal();
        return query.Where(condition);
    }

    /// <summary>
    /// Builds a query from the specification and applies ordering as defined.
    /// </summary>
    /// <param name="specification">The specification containing the filtering and ordering criteria.</param>
    /// <param name="negateFilter">If true, negates the filter condition; otherwise, applies it as-is.</param>
    /// <returns>The constructed and ordered IQueryable.</returns>
    private IQueryable<T> BuildQuery(IQuerySpecification<T> specification, bool negateFilter = false)
    {
        // Fix #5: Top and Page/PageSize are mutually exclusive
        if (specification.Top.HasValue && (specification.Page.HasValue || specification.PageSize.HasValue))
            throw new InvalidOperationException(
                "Cannot combine Top with Page/PageSize pagination on the same specification. Use one or the other.");

        // Fix #2: Skip/Take without ORDER BY produces non-deterministic results in SQL
        bool hasPagination = specification.Page.HasValue || specification.PageSize.HasValue;
        bool hasOrdering = specification.OrderBy != null || specification.ValiSort != null;
        if (hasPagination && !hasOrdering)
            throw new InvalidOperationException(
                "Pagination requires an ordering to produce deterministic results. " +
                "Set OrderBy or ValiSort on the specification before enabling pagination.");

        IQueryable<T> query = BuildBasicQuery(specification, negateFilter);
        query = ApplyOrdering(query, specification);
        query = ApplyPagination(query, specification);
        return query;
    }

    /// <summary>
    /// Saves changes to the database if requested, with exception handling.
    /// </summary>
    /// <param name="saveChanges">If true, persists changes to the database; otherwise, no action is taken.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="operationName">The name of the calling operation for error reporting.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the save operation fails.</exception>
    private async Task SaveChangesIfRequestedAsync(bool saveChanges, CancellationToken cancellationToken,
        string operationName)
    {
        if (saveChanges)
        {
            await ExecuteWithExceptionHandlingAsync(
                () => _dbContext.SaveChangesAsync(cancellationToken),
                operationName);
        }
    }

    private IQueryable<T> ApplyIgnoreQueryFilters(IQueryable<T> query, bool ignoreQueryFilters = false) =>
        ignoreQueryFilters ? query.IgnoreQueryFilters() : query;

    private sealed class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        internal ParameterReplacerVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }

    private async Task<Dictionary<TKey, TResult>> ExecuteGroupAggregateAsync<TKey, TResult>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TResult>> valueSelector,
        Func<IEnumerable<TResult>, TResult> aggregator,
        string operationName,
        CancellationToken cancellationToken
    ) where TKey : notnull
    {
        ParameterExpression sharedParam = Expression.Parameter(typeof(T), "x");
        Expression keyBody = new ParameterReplacerVisitor(keySelector.Parameters[0], sharedParam)
            .Visit(keySelector.Body);
        Expression valueBody = new ParameterReplacerVisitor(valueSelector.Parameters[0], sharedParam)
            .Visit(valueSelector.Body);
        ConstructorInfo tupleCtorInfo =
            typeof(ValueTuple<TKey, TResult>).GetConstructor(new[] { typeof(TKey), typeof(TResult) })!;
        NewExpression tupleNew = Expression.New(tupleCtorInfo, keyBody, valueBody);
        Expression<Func<T, ValueTuple<TKey, TResult>>> projectionLambda =
            Expression.Lambda<Func<T, ValueTuple<TKey, TResult>>>(tupleNew, sharedParam);
        List<ValueTuple<TKey, TResult>> pairs = await ExecuteWithExceptionHandlingAsync(
            () => query.Select(projectionLambda).ToListAsync(cancellationToken),
            operationName);
        return pairs
            .GroupBy(p => p.Item1)
            .ToDictionary(g => g.Key, g => aggregator(g.Select(p => p.Item2)));
    }

    private async Task<Dictionary<TKey, decimal>> ExecuteGroupAverageAsync<TKey, TResult>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TResult>> valueSelector,
        string operationName,
        CancellationToken cancellationToken
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        ParameterExpression sharedParam = Expression.Parameter(typeof(T), "x");
        Expression keyBody = new ParameterReplacerVisitor(keySelector.Parameters[0], sharedParam)
            .Visit(keySelector.Body);
        Expression valueBody = new ParameterReplacerVisitor(valueSelector.Parameters[0], sharedParam)
            .Visit(valueSelector.Body);
        ConstructorInfo tupleCtorInfo =
            typeof(ValueTuple<TKey, TResult>).GetConstructor(new[] { typeof(TKey), typeof(TResult) })!;
        NewExpression tupleNew = Expression.New(tupleCtorInfo, keyBody, valueBody);
        Expression<Func<T, ValueTuple<TKey, TResult>>> projectionLambda =
            Expression.Lambda<Func<T, ValueTuple<TKey, TResult>>>(tupleNew, sharedParam);
        List<ValueTuple<TKey, TResult>> pairs = await ExecuteWithExceptionHandlingAsync(
            () => query.Select(projectionLambda).ToListAsync(cancellationToken),
            operationName);
        return pairs
            .GroupBy(p => p.Item1)
            .ToDictionary(g => g.Key, g => g.Average(p => Convert.ToDecimal(p.Item2)));
    }

    #endregion
}