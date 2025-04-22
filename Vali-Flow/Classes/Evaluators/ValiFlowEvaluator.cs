using System.Linq.Expressions;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Results;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;

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
        ValidationHelper.ValidateEntityNotNull(entity);
        var condition = valiFlow.Build().Compile();
        return await Task.FromResult(condition(entity));
    }

    public async Task<bool> EvaluateAnyAsync(IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.AnyAsync(cancellationToken),
            nameof(EvaluateAnyAsync));
    }

    public async Task<int> EvaluateCountAsync(IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.CountAsync(cancellationToken),
            nameof(EvaluateCountAsync));
    }

    public async Task<T?> EvaluateGetFirstFailedAsync(IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBasicQuery(specification, true);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstFailedAsync));
    }

    public async Task<T?> EvaluateGetFirstAsync(IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstAsync));
    }

    public async Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification)
    {
        ValidateSpecificationForQuery(specification);

        IQueryable<T> query = BuildAndOrderQuery(specification, negateFilter: true);
        //query = ApplyPaginatedBlockQuery(query, specification);
        query = ApplyPagination(query, specification);
        
        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification)
    {
        ValidateSpecificationForQuery(specification);

        IQueryable<T> query = BuildAndOrderQuery(specification);
        //query = ApplyPaginatedBlockQuery(query, specification);
        query = ApplyPagination(query, specification);

        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull
    {
        ValidateSpecificationForQuery(specification);

        IQueryable<T> query = BuildAndOrderQuery(specification);
        query = query.GroupBy(selector).Select(g => g.First());
        query = ApplyOrdering(query, specification);

        return await Task.FromResult(ApplyPagination(query, specification));
    }

    public async Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull
    {
        ValidateSpecificationForQuery(specification);

        IQueryable<T> query = BuildAndOrderQuery(specification);
        query = query.GroupBy(selector).Where(g => g.Count() > ConstantHelper.One).SelectMany(g => g);
        query = ApplyOrdering(query, specification);

        return await Task.FromResult(ApplyPagination(query, specification));
    }

    public async Task<T?> EvaluateGetLastFailedAsync(IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = BuildBasicQuery(specification, true);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastFailedAsync));
    }

    public async Task<T?> EvaluateGetLastAsync(IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default)
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
            return query.GroupBy(keySelector)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);
        }, nameof(EvaluateGroupedAsync));
    }

    public async Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                IEnumerable<T> data = await query.ToListAsync(cancellationToken);
                IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

                return groups.ToDictionary(g => g.Key, g => g.Count());
            }, nameof(EvaluateCountByGroupAsync));
    }

    public async Task<Dictionary<TKey, TResult>> EvaluateSumByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                List<T> data = await query.ToListAsync(cancellationToken);
                IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

                return groups.ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Aggregate(TResult.Zero, (acc, x) => acc + x)
                );
            }, nameof(EvaluateSumByGroupAsync));
    }

    public async Task<Dictionary<TKey, TResult>> EvaluateMinByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                List<T> data = await query.ToListAsync(cancellationToken);
                IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

                return groups.ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Min() ?? TResult.Zero
                );
            }, nameof(EvaluateMinByGroupAsync));
    }

    public async Task<Dictionary<TKey, TResult>> EvaluateMaxByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                List<T> data = await query.ToListAsync(cancellationToken);
                IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

                return groups.ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Max() ?? TResult.Zero
                );
            }, nameof(EvaluateMaxByGroupAsync));
    }

    public async Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        return await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                List<T> data = await query.ToListAsync(cancellationToken);
                IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

                return groups.ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Average(x => Convert.ToDecimal(x))
                );
            }, nameof(EvaluateAverageByGroupAsync));
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        var query = BuildBasicQuery(specification);

        IEnumerable<T> data = await query.ToListAsync(cancellationToken);
        Dictionary<TKey, List<T>> result = data.GroupBy(keySelector)
            .Where(g => g.Count() > ConstantHelper.One)
            .ToDictionary(g => g.Key, g => g.ToList());

        return await ExecuteWithExceptionHandlingAsync(() => Task.FromResult(result),
            nameof(EvaluateDuplicatesByGroupAsync));
    }

    public async Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        IQueryable<T> query = BuildBasicQuery(specification);

        IEnumerable<T> data = await query.ToListAsync(cancellationToken);
        Dictionary<TKey, T> result = data.GroupBy(keySelector)
            .Where(g => g.Count() == ConstantHelper.One)
            .ToDictionary(g => g.Key, g => g.First());

        return await ExecuteWithExceptionHandlingAsync(() => Task.FromResult(result),
            nameof(EvaluateUniquesByGroupAsync));
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
        IQuerySpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidateSpecificationForQuery(specification);

        IQueryable<T> query = BuildAndOrderQuery(specification);
        query = ApplyOrdering(query, specification);
        List<T> data = await query.Take(specification.Top ?? ConstantHelper.Fifty).ToListAsync(cancellationToken);
        return await Task.FromResult(data.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToList()));
    }

    #endregion

    #region Methods Write

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        return await ExecuteWithExceptionHandlingAsync(
            async () => (await _dbContext.Set<T>().AddAsync(entity, cancellationToken)).Entity, nameof(AddAsync));
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "The collection of entities cannot be null.");

        IEnumerable<T> entityList = entities.ToList();

        if (!entityList.Any())
            throw new ArgumentException("The collection of entities cannot be empty.", nameof(entities));

        return await ExecuteWithExceptionHandlingAsync(
            async () =>
            {
                await _dbContext.Set<T>().AddRangeAsync(entityList, cancellationToken);
                return entityList;
            }, nameof(AddRangeAsync));
    }

    public async Task<T> UpdateAsync(T entity)
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        _dbContext.Set<T>().Update(entity);
        await Task.CompletedTask;
        return entity;
    }

    public async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "The collection of entities cannot be null.");

        IEnumerable<T> entityList = entities.ToList();

        if (!entityList.Any())
            throw new ArgumentException("The collection of entities cannot be empty.", nameof(entities));

        _dbContext.Set<T>().UpdateRange(entityList);
        await Task.CompletedTask;
        return entityList;
    }

    public async Task DeleteAsync(T entity)
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        _dbContext.Set<T>().Remove(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "The collection of entities cannot be null.");

        IEnumerable<T> entityList = entities.ToList();

        if (!entityList.Any())
            throw new ArgumentException("The collection of entities cannot be empty.", nameof(entities));

        _dbContext.Set<T>().RemoveRange(entityList);
        await Task.CompletedTask;
    }


    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteWithExceptionHandlingAsync(() => _dbContext.SaveChangesAsync(cancellationToken),
            nameof(SaveChangesAsync));
    }

    public async Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        CancellationToken cancellationToken = default
    )
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        T? existingEntity = await _dbContext.Set<T>().FirstOrDefaultAsync(matchCondition, cancellationToken);

        if (existingEntity == null)
        {
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        }

        return entity;
    }

    public async Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Func<T, TProperty> keySelector,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "The collection of entities cannot be null.");

        IEnumerable<T> entityList = entities.ToList();

        if (!entityList.Any())
            throw new ArgumentException("The collection of entities cannot be empty.", nameof(entities));

        IEnumerable<TProperty> keys = entityList.Select(keySelector).ToList();
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

        return entityList;
    }


    public async Task DeleteByConditionAsync(Expression<Func<T, bool>> condition,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<T> entitiesToDelete = await _dbContext.Set<T>()
            .Where(condition)
            .ToListAsync(cancellationToken);
        
        if (entitiesToDelete.Any()) _dbContext.Set<T>().RemoveRange(entitiesToDelete);
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

            throw new InvalidOperationException($"Error executing transaction in {nameof(ExecuteTransactionAsync)}.",
                ex);
        }
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
    private IQueryable<T> ApplyAsNoTracking(IQueryable<T> query, bool asNoTracking = true)
    {
        return asNoTracking ? query.AsNoTracking() : query;
    }


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
            int skip = (specification.Page.Value - ConstantHelper.One) * specification.PageSize.Value;
            int take = specification.PageSize.Value;
            query = query.Skip(skip).Take(take);
        }
        else if (specification is { Top: not null, Page: null })
        {
            query = query.Take(specification.Top.Value);
        }

        return query;
    }

    private IQueryable<T> ApplyAsSplitQuery(IQueryable<T> query, bool asSplitQuery = false)
    {
        return asSplitQuery ? query.AsSplitQuery() : query;
    }

    /// <summary>
    /// Builds a minimal query from the specification, applying only the filter, AsNoTracking, Includes, and AsSplitQuery options.
    /// Does not apply ordering or pagination.
    /// </summary>
    /// <param name="specification">The specification containing the filtering and query configuration.</param>
    /// <param name="negateCondition">If true, negates the filter condition; otherwise, applies it as-is.</param>
    /// <returns>The constructed IQueryable.</returns>
    private IQueryable<T> BuildBasicQuery(ISpecification<T> specification, bool negateCondition = false)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsQueryable();

        ValidationHelper.ValidateQueryNotNull(query);

        query = ApplyAsNoTracking(query, specification.AsNoTracking);

        var includeList = specification.Includes;

        if (includeList != null)
        {
            query = ApplyIncludes(query, includeList);

            if (specification.AsSplitQuery)
            {
                query = ApplyAsSplitQuery(query);
            }
        }

        Expression<Func<T, bool>> condition =
            negateCondition ? specification.Filter.BuildNegated() : specification.Filter.Build();
        query = query.Where(condition);

        return query;
    }

    private IQueryable<T> BuildQuery(ISpecification<T> specification, bool negateCondition = false)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsQueryable();

        ValidationHelper.ValidateQueryNotNull(query);

        query = ApplyAsNoTracking(query, specification.AsNoTracking);

        if (specification.Includes != null)
        {
            foreach (var include in specification.Includes)
            {
                query = include.ApplyInclude(query);
            }
        }

        Expression<Func<T, bool>> condition =
            negateCondition ? specification.Filter.BuildNegated() : specification.Filter.Build();
        query = query.Where(condition);

        return query;
    }

    /// <summary>
    /// Validates the specification properties to ensure they are valid for query construction.
    /// </summary>
    /// <param name="specification">The specification to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown if specification properties are invalid.</exception>
    private void ValidateSpecificationForQuery(IQuerySpecification<T> specification)
    {
        if (specification.Filter == null)
        {
            throw new InvalidOperationException("Filter must be specified in the specification.");
        }

        if (specification is { Page: not null, PageSize: null })
        {
            throw new InvalidOperationException("PageSize must be specified when Page is specified.");
        }

        if (specification.Page is < ConstantHelper.One)
        {
            throw new InvalidOperationException("Page must be greater than or equal to 1.");
        }

        if (specification.PageSize is < ConstantHelper.One)
        {
            throw new InvalidOperationException("PageSize must be greater than or equal to 1.");
        }

        if (specification.Top is < ConstantHelper.One)
        {
            throw new InvalidOperationException("Top must be greater than or equal to 1.");
        }
    }

    /// <summary>
    /// Builds a query from the specification and applies ordering as defined.
    /// </summary>
    /// <param name="specification">The specification containing the filtering and ordering criteria.</param>
    /// <param name="negateFilter">If true, negates the filter condition; otherwise, applies it as-is.</param>
    /// <returns>The constructed and ordered IQueryable.</returns>
    private IQueryable<T> BuildAndOrderQuery(IQuerySpecification<T> specification, bool negateFilter = false)
    {
        IQueryable<T> query = BuildQuery(specification, negateFilter);
        return ApplyOrdering(query, specification);
    }

    #endregion
}