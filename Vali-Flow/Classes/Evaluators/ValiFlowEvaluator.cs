using System.Linq.Expressions;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Options;
using Vali_Flow.Classes.Results;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;

namespace Vali_Flow.Classes.Evaluators;

public class ValiFlowEvaluator<T> : IEvaluatorRead<T>, IEvaluatorWrite<T> where T : class
{
    private readonly DbContext _dbContext;

    public ValiFlowEvaluator(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "The DbContext provided is null.");
    }

    public async Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity)
    {
        ValidationHelper.ValidateEntityNotNull(entity);

        try
        {
            Func<T, bool> condition = valiFlow.Build().Compile();
            return await Task.FromResult(condition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<bool> EvaluateAnyAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            return await query.AnyAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<int> EvaluateCountAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            int result = await query.CountAsync(condition, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T?> GetFirstFailedAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.BuildNegated();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);


            return await query.FirstOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T?> GetFirstAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            return await query.FirstOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.BuildNegated();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);
        query = ApplyOrdering(query, orderBy, ascending, thenBys);
        query = ApplyPagination(query, page, pageSize);

        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);
        query = ApplyOrdering(query, orderBy, ascending, thenBys);

        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.Ten,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidatePageZero(page);
        ValidationHelper.ValidatePageSizeZero(pageSize);

        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);
        query = ApplyOrdering(query, orderBy, ascending, thenBys);
        query = ApplyPagination(query, page, pageSize);

        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidateCountZero(count);

        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);
        query = ApplyOrdering(query, orderBy, ascending, thenBys);
        query = query.Take(count);

        return await Task.FromResult(query);
    }

    public async Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);

        IQueryable<T> distinctQuery = query.GroupBy(selector).Select(g => g.First());

        distinctQuery = ApplyOrdering(distinctQuery, orderBy, ascending, thenBys);
        distinctQuery = ApplyPagination(distinctQuery, page, pageSize);

        return await Task.FromResult(distinctQuery);
    }

    public async Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);

        IQueryable<T> duplicatesQuery = query.GroupBy(selector)
            .Where(g => g.Count() > ConstantHelper.One)
            .SelectMany(g => g);

        duplicatesQuery = ApplyOrdering(duplicatesQuery, orderBy, ascending, thenBys);
        duplicatesQuery = ApplyPagination(duplicatesQuery, page, pageSize);

        return await Task.FromResult(duplicatesQuery);
    }

    public async Task<T?> GetLastFailedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.BuildNegated();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        return await query.LastOrDefaultAsync(condition, cancellationToken);
    }

    public async Task<T?> GetLastAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            return await query.LastOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<TResult> EvaluateMinAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).MinAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<TResult> EvaluateMaxAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).MaxAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<decimal> EvaluateAverageAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).AverageAsync(x => Convert.ToDecimal(x), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<int> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, int>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<long> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, long>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<double> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, double>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<decimal> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, decimal>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<float> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, float>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<TResult> EvaluateAggregateAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<TResult> values = await query.Select(selector).ToListAsync(cancellationToken);

            return !values.Any() ? TResult.Zero : values.Aggregate(TResult.Zero, aggregator);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            return await query.GroupBy(keySelector)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.ToList(),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, int> result = groups.ToDictionary(g => g.Key, g => g.Count());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, TResult>> EvaluateSumByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, TResult> result = groups
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Aggregate(TResult.Zero, (acc, x) => acc + x)
                );

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, TResult>> EvaluateMinByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, TResult> result = groups
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Min() ?? TResult.Zero
                );

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, TResult>> EvaluateMaxByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, TResult> result = groups
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Max() ?? TResult.Zero
                );

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, decimal> result = groups
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Average(x => Convert.ToDecimal(x))
                );

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, List<T>> result = groups
                .Where(g => g.Count() > ConstantHelper.One)
                .ToDictionary(g => g.Key, g => g.ToList());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, T> result = groups
                .Where(g => g.Count() == ConstantHelper.One)
                .ToDictionary(g => g.Key, g => g.First());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey, TKey2, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        int count,
        Expression<Func<T, TKey2>>? orderBy = null,
        bool ascending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TKey2 : notnull
    {
        ValidationHelper.ValidateCountZero(count);

        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);
            query = ApplyOrdering(query, orderBy, ascending, null);

            List<T> data = await query.Take(count).ToListAsync(cancellationToken);

            Dictionary<TKey, List<T>> result = data
                .GroupBy(keySelector)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateQuery<TProperty, TKey>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);
        query = ApplyOrdering(query, orderBy, ascending, thenBys);

        return await Task.FromResult(query);
    }

    public async Task<PaginatedBlockResult<T>> GetPaginatedBlockAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        CancellationToken cancellationToken = default
    )
        where TKey : notnull
    {
        try
        {
            Expression<Func<T, bool>> condition = valiFlow.Build();
            IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

            query = query.Where(condition);
            query = ApplyOrdering(query, orderBy, ascending, thenBys);

            int currentBlock = (page - ConstantHelper.One) * pageSize / blockSize;
            int blockOffset = currentBlock * blockSize;

            IQueryable<T> blockQuery = query.Skip(blockOffset).Take(blockSize);

            int totalItemsInBlock = await blockQuery.CountAsync(cancellationToken);

            IEnumerable<T> blockData = await blockQuery.ToListAsync(cancellationToken);
            IEnumerable<T> pageData = ApplyPaginationBlock(blockData, page, pageSize, blockSize);

            return new PaginatedBlockResult<T>
            {
                Items = pageData,
                CurrentPage = page,
                PageSize = pageSize,
                BlockSize = blockSize,
                TotalItemsInBlock = totalItemsInBlock,
                HasMoreBlocks = totalItemsInBlock == blockSize
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> GetPaginatedBlockQueryAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        Expression<Func<T, bool>> condition = valiFlow.Build();
        IQueryable<T> query = ConfigureQuery(includes, asNoTracking);

        query = query.Where(condition);
        query = ApplyOrdering(query, orderBy, ascending, thenBys);

        int currentBlock = ((page - ConstantHelper.One) * pageSize) / blockSize;
        int blockOffset = currentBlock * blockSize;
        int pageOffset = (((page - ConstantHelper.One) * pageSize)) % blockSize;
        int finalOffset = blockOffset + pageOffset;

        return await Task.FromResult(query.Skip(finalOffset).Take(pageSize));
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al insertar la entidad en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "La colección de entidades no puede ser nula.");

        List<T> entityList = entities.ToList();
    
        if (!entityList.Any())
            throw new ArgumentException("La colección de entidades no puede estar vacía.", nameof(entities));

        try
        {
            await _dbContext.Set<T>().AddRangeAsync(entityList, cancellationToken);
            return entityList;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al agregar múltiples entidades en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T> UpdateAsync(T entity)
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        try
        {
            _dbContext.Set<T>().Update(entity);
            await Task.CompletedTask;
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al actualizar la entidad en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "La colección de entidades no puede ser nula o vacía.");

        List<T> entityList = entities.ToList();

        if (!entityList.Any())
            throw new ArgumentException("La colección de entidades no puede estar vacía.", nameof(entities));

        
        try
        {
            _dbContext.Set<T>().UpdateRange(entityList);
            await Task.CompletedTask;
            return entityList;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al actualizar múltiples entidades en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task DeleteAsync(T entity)
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        try
        {
            _dbContext.Set<T>().Remove(entity);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al eliminar la entidad en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "La colección de entidades no puede ser nula.");

        List<T> entityList = entities.ToList();

        if (!entityList.Any())
            throw new ArgumentException("La colección de entidades no puede estar vacía.", nameof(entities));

        try
        {
            _dbContext.Set<T>().RemoveRange(entityList);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al eliminar múltiples entidades en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }


    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken); 
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al guardar los cambios en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T> UpsertAsync(T entity, Expression<Func<T, bool>> matchCondition, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateEntityNotNull(entity);
        try
        {
            var existingEntity = await _dbContext.Set<T>().FirstOrDefaultAsync(matchCondition, cancellationToken);
            if (existingEntity == null)
            {
                await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
            }
            else
            {
                _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                return existingEntity;
            }
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al realizar upsert en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(IEnumerable<T> entities, Func<T, TProperty> keySelector, CancellationToken cancellationToken = default) where TProperty : notnull
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), "La colección de entidades no puede ser nula.");

        List<T> entityList = entities.ToList();
        if (!entityList.Any())
            throw new ArgumentException("La colección de entidades no puede estar vacía.", nameof(entities));

        try
        {
            var keys = entityList.Select(keySelector).ToList();

            var existingEntities = await _dbContext.Set<T>()
                .Where(e => keys.Contains(keySelector(e)))
                .ToListAsync(cancellationToken);

            var existingEntityDict = existingEntities.ToDictionary(keySelector, e => e);

            foreach (var entity in entityList)
            {
                var key = keySelector(entity);
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
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al realizar upsert múltiple en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }


    public async Task DeleteByConditionAsync(Expression<Func<T, bool>> condition, CancellationToken cancellationToken = default)
    {
        try
        {
            var entitiesToDelete = await _dbContext.Set<T>().Where(condition).ToListAsync(cancellationToken);
            if (entitiesToDelete.Any())
            {
                _dbContext.Set<T>().RemoveRange(entitiesToDelete);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al eliminar por condición en {UtilHelper.GetCurrentMethodName()}.", ex);
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
                throw new InvalidOperationException($"Error al ejecutar transacción en {UtilHelper.GetCurrentMethodName()}. Falló rollback: {rollbackEx.Message}", ex);
            }
            throw new InvalidOperationException($"Error al ejecutar transacción en {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    #region Private

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
    private IQueryable<T> ApplyAsNoTracking(IQueryable<T> query, bool asNoTracking = false)
    {
        return asNoTracking ? query.AsNoTracking() : query;
    }


    /// <summary>
    /// Applies the specified include expressions to an <see cref="IQueryable{T}"/> query.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to be included.</typeparam>
    /// <param name="query">The base query to which the includes will be applied.</param>
    /// <param name="includes">
    /// A collection of include expressions defining related entities to be loaded.
    /// If <c>null</c>, the query remains unchanged.
    /// </param>
    /// <returns>
    /// The modified query with the specified includes applied, or the original query if no includes are provided.
    /// </returns>
    private IQueryable<T> ApplyIncludes<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        if (includes != null)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        return query;
    }

    /// <summary>
    /// Applies primary and secondary ordering to the given query.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the query.</typeparam>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="query">The original query.</param>
    /// <param name="orderBy">The primary ordering expression. If null, no ordering is applied.</param>
    /// <param name="ascending">Determines if the primary ordering is ascending.</param>
    /// <param name="thenBys">The secondary ordering expression. If null, no secondary ordering is applied.</param>
    /// <returns>An <see cref="IQueryable{T}"/> with the specified ordering applied.</returns>
    private IQueryable<T> ApplyOrdering<TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy,
        bool ascending,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys)
    {
        if (orderBy == null)
            return query;

        IOrderedQueryable<T> orderedQuery = ascending
            ? query.OrderBy(orderBy)
            : query.OrderByDescending(orderBy);

        if (thenBys != null)
        {
            IEnumerable<EfOrderThenBy<T, TKey>> thenByList = thenBys.ToList();

            if (thenByList.Any())
            {
                orderedQuery = thenByList.Aggregate(
                    orderedQuery,
                    (currentOrderedQuery, thenByExpression) =>
                        thenByExpression.Ascending
                            ? currentOrderedQuery.ThenBy(thenByExpression.ThenBy)
                            : currentOrderedQuery.ThenByDescending(thenByExpression.ThenBy)
                );
            }
        }

        return orderedQuery;
    }

    private IQueryable<T> ApplyPagination(IQueryable<T> query, int? page, int? pageSize)
    {
        if (!page.HasValue || !pageSize.HasValue) return query;

        if (page.Value <= ConstantHelper.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize.Value <= ConstantHelper.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        query = query.Skip((page.Value - ConstantHelper.One) * pageSize.Value).Take(pageSize.Value);

        return query;
    }

    private IQueryable<T> ApplyPagination(IQueryable<T> query, int page = ConstantHelper.One,
        int pageSize = ConstantHelper.Fifty)
    {
        if (page <= ConstantHelper.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        
        if (pageSize <= ConstantHelper.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        query = query.Skip((page - ConstantHelper.One) * pageSize).Take(pageSize);

        return query;
    }

    private IEnumerable<T> ApplyPaginationBlock(
        IEnumerable<T> query,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.Fifty,
        int blockSize = ConstantHelper.Thousand)
    {
        return query.Skip((page - ConstantHelper.One) * pageSize % blockSize).Take(pageSize).ToList();
    }

    private IQueryable<T> ConfigureQuery<TProperty>(
        IEnumerable<Expression<Func<T, TProperty>>>? includes,
        bool asNoTracking
    )
    {
        IQueryable<T> query = _dbContext.Set<T>().AsQueryable();

        ValidationHelper.ValidateQueryNotNull(query);

        query = ApplyAsNoTracking(query, asNoTracking);
        query = ApplyIncludes(query, includes);

        return query;
    }

    #endregion
}