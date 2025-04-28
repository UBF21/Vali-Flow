using System.Linq.Expressions;
using System.Numerics;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

public class ValiFlowEvaluator<T> : IEvaluatorRead<T>, IEvaluatorWrite<T> where T : class
{
    private readonly DbContext _dbContext;

    public ValiFlowEvaluator(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "The DbContext provided is null.");
    }

    #region Methods Read

    public async Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity)
    {
        Validation.ValidateEntityNotNull(entity);
        var condition = valiFlow.Build().Compile();
        return await Task.FromResult(condition(entity));
    }

    public async Task<bool> EvaluateAnyAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.AnyAsync(cancellationToken),
            nameof(EvaluateAnyAsync));
    }

    public async Task<int> EvaluateCountAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.CountAsync(cancellationToken),
            nameof(EvaluateCountAsync));
    }

    public async Task<T?> EvaluateGetFirstFailedAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        var query = BuildBasicQuery(specification, true);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstFailedAsync));
    }

    public async Task<T?> EvaluateGetFirstAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstAsync));
    }

    public async Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification)
    {
        IQueryable<T> query = BuildQuery(specification, negateFilter: true);
        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification)
    {
        IQueryable<T> query = BuildQuery(specification);
        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildQuery(specification)
            .GroupBy(selector)
            .Select(g => g.First());
        return await Task.FromResult(ApplyPagination(query, specification));
    }

    public async Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildQuery(specification)
            .GroupBy(selector)
            .Where(g => g.Count() > Constants.One)
            .SelectMany(g => g);

        return await Task.FromResult(query);
    }

    public async Task<T?> EvaluateGetLastFailedAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<T> query = BuildBasicQuery(specification, true);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastFailedAsync));
    }

    public async Task<T?> EvaluateGetLastAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastAsync));
    }

    public async Task<TResult> EvaluateMinAsync<TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
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
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Sum = g.Sum(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken),
            nameof(EvaluateSumByGroupAsync));
    }

    public async Task<Dictionary<TKey, long>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Sum = g.Sum(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken),
            nameof(EvaluateSumByGroupAsync));
    }

    public async Task<Dictionary<TKey, float>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Sum = g.Sum(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken),
            nameof(EvaluateSumByGroupAsync));
    }

    public async Task<Dictionary<TKey, double>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Sum = g.Sum(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken),
            nameof(EvaluateSumByGroupAsync));
    }

    public async Task<Dictionary<TKey, decimal>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Sum = g.Sum(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken),
            nameof(EvaluateSumByGroupAsync));
    }

    public async Task<Dictionary<TKey, int>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Min = g.Min(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Min, cancellationToken),
            nameof(EvaluateMinByGroupAsync));
    }

    public async Task<Dictionary<TKey, long>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Min = g.Min(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Min, cancellationToken),
            nameof(EvaluateMinByGroupAsync));
    }

    public async Task<Dictionary<TKey, float>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Min = g.Min(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Min, cancellationToken),
            nameof(EvaluateMinByGroupAsync));
    }

    public async Task<Dictionary<TKey, double>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Min = g.Min(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Min, cancellationToken),
            nameof(EvaluateMinByGroupAsync));
    }

    public async Task<Dictionary<TKey, decimal>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Min = g.Min(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Min, cancellationToken),
            nameof(EvaluateMinByGroupAsync));
    }

    public async Task<Dictionary<TKey, int>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Max = g.Max(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Max, cancellationToken),
            nameof(EvaluateMaxByGroupAsync));
    }

    public async Task<Dictionary<TKey, long>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Max = g.Max(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Max, cancellationToken),
            nameof(EvaluateMaxByGroupAsync));
    }

    public async Task<Dictionary<TKey, float>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Max = g.Max(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Max, cancellationToken),
            nameof(EvaluateMaxByGroupAsync));
    }

    public async Task<Dictionary<TKey, double>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Max = g.Max(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Max, cancellationToken),
            nameof(EvaluateMaxByGroupAsync));
    }

    public async Task<Dictionary<TKey, decimal>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Max = g.Max(selector.Compile()) })
                .ToDictionaryAsync(x => x.Key, x => x.Max, cancellationToken),
            nameof(EvaluateMaxByGroupAsync));
    }

    public async Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Avg = g.Average(x => Convert.ToDecimal(selector.Compile()(x))) })
                .ToDictionaryAsync(x => x.Key, x => x.Avg, cancellationToken),
            nameof(EvaluateAverageByGroupAsync));
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        var query = BuildBasicQuery(specification);

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
        IQueryable<T> query = BuildQuery(specification);
        query = query.Take(specification.Top ?? Constants.Fifty);
        return await ExecuteWithExceptionHandlingAsync(
            () => query
                .GroupBy(keySelector)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken),
            nameof(EvaluateTopByGroupAsync));
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
        Func<T, TProperty> keySelector,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull
    {
        IEnumerable<T> entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        IEnumerable<TProperty> keys = entityList.Select(keySelector);
        IEnumerable<T> existingEntities = await _dbContext.Set<T>()
            .Where(e => keys.Contains(keySelector(e)))
            .ToListAsync(cancellationToken);

        Dictionary<TProperty, T> existingEntityDict = existingEntities.ToDictionary(keySelector, e => e);

        foreach (T entity in entityList)
        {
            TProperty key = keySelector(entity);
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
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        IEnumerable<T> entitiesToDelete = await _dbContext.Set<T>()
            .Where(condition)
            .ToListAsync(cancellationToken);

        if (entitiesToDelete.Any())
        {
            _dbContext.Set<T>().RemoveRange(entitiesToDelete);
            await SaveChangesIfRequestedAsync(saveChanges, cancellationToken, nameof(DeleteByConditionAsync));
        }
    }


    public async Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)
    {
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
        var entityList = entities.ToList();

        Validation.ValidateEntitiesNotNull(entityList);
        Validation.ValidateEntitiesEmpty(entityList);

        await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.BulkUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
                return Task.CompletedTask;
            },
            nameof(BulkInsertAsync));
    }

    public async Task BulkDeleteAsync(
        IEnumerable<T> entities,
        BulkConfig? bulkConfig = null,
        CancellationToken cancellationToken = default
    )
    {
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
        if (specification.OrderBy == null) return query;

        IOrderedQueryable<T> orderedQuery = specification.OrderBy.ApplyOrderBy(query);

        if (specification.ThenBys != null && specification.ThenBys.Any())
        {
            foreach (var thenBy in specification.ThenBys)
            {
                orderedQuery = thenBy.ApplyThenBy(orderedQuery);
            }
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
        else if (specification is { Top: not null, Page: null })
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

        Validation.ValidateQueryNotNull(query);

        query = ApplyIgnoreQueryFilters(query, specification.IgnoreQueryFilters);
        query = ApplyWhere(query, specification.Filter, negateCondition);
        query = ApplyAsNoTracking(query, specification.AsNoTracking);
        query = ApplyIncludes(query, specification.Includes);
        query = ApplyAsSplitQuery(query);

        return query;
    }

    /// <summary>
    /// Applies a filter condition to the provided query using the specified validation flow.
    /// </summary>
    /// <param name="query">The query to which the filter condition will be applied. Cannot be null.</param>
    /// <param name="filter">The validation flow defining the filter criteria. Cannot be null.</param>
    /// <param name="negateCondition">A value indicating whether the filter condition should be negated. Defaults to <see langword="false"/>.</param>
    /// <returns>The query with the filter condition applied using the <see cref="ValiFlow{T}"/> criteria.</returns>
    /// <remarks>
    /// This method applies the filter criteria defined by the <paramref name="filter"/> to the query using Entity Framework Core's <c>Where</c> method. 
    /// If <paramref name="negateCondition"/> is <see langword="true"/>, the filter condition is negated (e.g., <c>NOT</c> is applied to the criteria), 
    /// allowing for exclusion-based filtering. The <paramref name="filter"/> is typically sourced from a specification's filter property.
    /// This method is used internally to incorporate the filtering logic defined by <see cref="ValiFlow{T}"/> into the query pipeline, 
    /// enabling complex and reusable filter conditions. Use this method to ensure that filter criteria are consistently applied to queries, 
    /// supporting scenarios such as filtering entities based on business rules or excluding specific records when negating conditions.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> or <paramref name="filter"/> is null.</exception>
    private IQueryable<T> ApplyWhere(IQueryable<T> query, ValiFlow<T> filter, bool negateCondition = false)
    {
        Validation.ValidateQueryNotNull(query);
        Validation.ValidateFilterNotNull(filter);

        Expression<Func<T, bool>> condition = negateCondition ? filter.BuildNegated() : filter.Build();
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

    #endregion
}