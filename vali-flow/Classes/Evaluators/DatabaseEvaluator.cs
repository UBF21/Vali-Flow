using System.Linq.Expressions;
using System.Numerics;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Evaluators;
using Microsoft.EntityFrameworkCore;
using vali_flow.Classes.Options;
using vali_flow.Utils;

namespace vali_flow.Classes.Evaluators;

public class DatabaseEvaluator<TBuilder, T> : IDatabaseEvaluator<T>
    where TBuilder : BaseExpression<TBuilder, T>, IDatabaseEvaluator<T>, new() where T : class
{
    private readonly BaseExpression<TBuilder, T> _builder;

    public DatabaseEvaluator(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public async Task<bool> EvaluateAsync(T entity)
    {
        ValidationHelper.ValidateEntityNotNull(entity);

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            return await Task.FromResult(condition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<bool> EvaluateAnyAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.AnyAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<int> EvaluateCountAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            int result = await query.CountAsync(condition, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T?> GetFirstFailedAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.BuildNegated();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.FirstOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T?> GetFirstAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.FirstOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        IQueryable<T> query,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        List<ThenByDataBaseExpression<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
        where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.BuildNegated();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = ApplyOrdering(query, orderBy, ascending, thenBys);
            query = ApplyPagination(query, page, pageSize);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        List<ThenByDataBaseExpression<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = ApplyOrdering(query, orderBy, ascending, thenBys);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        IQueryable<T> query,
        int page,
        int pageSize,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        List<ThenByDataBaseExpression<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);
        ValidationHelper.ValidatePageZero(page);
        ValidationHelper.ValidatePageSizeZero(pageSize);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = ApplyOrdering(query, orderBy, ascending, thenBys);
            query = query.Skip((page - ConstantsHelper.One) * pageSize).Take(pageSize);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        IQueryable<T> query,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        List<ThenByDataBaseExpression<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);
        ValidationHelper.ValidateCountZero(count);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = ApplyOrdering(query, orderBy, ascending, thenBys);
            query = query.Take(count);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        List<ThenByDataBaseExpression<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            IQueryable<T> distinctQuery = query.GroupBy(selector).Select(g => g.First());

            distinctQuery = ApplyOrdering(distinctQuery, orderBy, ascending, thenBys);
            distinctQuery = ApplyPagination(distinctQuery, page, pageSize);

            return await Task.FromResult(distinctQuery);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        List<ThenByDataBaseExpression<T, TKey>>? thenBys = null,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            IQueryable<T> duplicatesQuery = query.GroupBy(selector)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);

            duplicatesQuery = ApplyOrdering(duplicatesQuery, orderBy, ascending, thenBys);
            duplicatesQuery = ApplyPagination(duplicatesQuery, page, pageSize);

            return await Task.FromResult(duplicatesQuery);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }


    public async Task<T?> GetLastFailedAsync<TKey, TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.BuildNegated();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.LastOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<T?> GetLastAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.LastOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<TResult> EvaluateMinAsync<TResult, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).MinAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<TResult> EvaluateMaxAsync<TResult, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).MaxAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<decimal> EvaluateAverageAsync<TResult, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).AverageAsync(x => Convert.ToDecimal(x), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<int> EvaluateSumAsync<TProperty>(
        IQueryable<T> query,
        Expression<Func<T, int>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<long> EvaluateSumAsync<TProperty>(
        IQueryable<T> query,
        Expression<Func<T, long>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<double> EvaluateSumAsync<TProperty>(
        IQueryable<T> query,
        Expression<Func<T, double>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<decimal> EvaluateSumAsync<TProperty>(
        IQueryable<T> query,
        Expression<Func<T, decimal>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<float> EvaluateSumAsync<TProperty>(
        IQueryable<T> query,
        Expression<Func<T, float>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<TResult> EvaluateAggregateAsync<TResult, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<TResult> values = await query.Select(selector).ToListAsync(cancellationToken);

            if (!values.Any()) return TResult.Zero;

            return values.Aggregate(TResult.Zero, aggregator);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

            Dictionary<TKey, List<T>> result = data
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.ToList());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

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
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

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
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

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
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

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
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TResult : INumber<TResult>
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

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
        IQueryable<T> query, Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, List<T>> result = groups
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.ToList());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            IEnumerable<T> data = await query.ToListAsync(cancellationToken);
            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, T> result = groups
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        ValidationHelper.ValidateQueryNotNull(query);
        ValidationHelper.ValidateCountZero(count);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);

            List<T> data = await query.ToListAsync(cancellationToken);

            var groups = data.GroupBy(keySelector);

            Dictionary<TKey, List<T>> result = groups.ToDictionary(
                g => g.Key,
                g =>
                {
                    List<T> items = g.ToList();

                    if (orderBy != null)
                        items = (ascending ? items.OrderBy(orderBy) : items.OrderByDescending(orderBy)).ToList();

                    return items.Take(count).ToList();
                }
            );

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateQuery(
        IQueryable<T> query,
        bool asNoTracking = false
    )
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);
            query = ApplyAsNoTracking(query, asNoTracking);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error evaluating {UtilHelper.GetCurrentMethodName()}.", ex);
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
        List<ThenByDataBaseExpression<T, TKey>>? thenBys)
    {
        if (orderBy == null) return query;

        IOrderedQueryable<T> orderedQuery = ascending
            ? query.OrderBy(orderBy)
            : query.OrderByDescending(orderBy);

        if (thenBys != null && thenBys.Any())
        {
            orderedQuery = thenBys.Aggregate(
                orderedQuery,
                (currentOrderedQuery, thenByExpression) =>
                    thenByExpression.Ascending
                        ? currentOrderedQuery.ThenBy(thenByExpression.ThenBy)
                        : currentOrderedQuery.ThenByDescending(thenByExpression.ThenBy)
            );
        }

        return orderedQuery;
    }


    private IQueryable<T> ApplyPagination(IQueryable<T> query, int? page, int? pageSize)
    {
        if (!page.HasValue || !pageSize.HasValue) return query;

        if (page.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

        if (pageSize.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        query = query.Skip((page.Value - ConstantsHelper.One) * pageSize.Value).Take(pageSize.Value);

        return query;
    }

    #endregion
}