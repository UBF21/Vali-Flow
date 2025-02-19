using System.Linq.Expressions;
using System.Numerics;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Evaluators;
using Microsoft.EntityFrameworkCore;
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
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            return await Task.FromResult(condition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating the entity.", ex);
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
            throw new InvalidOperationException("Error evaluating AnyAsync.", ex);
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

            return await query.CountAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating CountAsync.", ex);
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
            throw new InvalidOperationException("Error evaluating GetFirstFailedAsync.", ex);
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
            throw new InvalidOperationException("Error evaluating GetFirstAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        IQueryable<T> query,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.BuildNegated();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);
            query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);
            query = ApplyPagination(query, page, pageSize);

            return Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateAllFailedAsync.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);
            query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateAllAsync.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        IQueryable<T> query,
        int page,
        int pageSize,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        if (page <= ConstantsHelper.Zero)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

        if (pageSize <= ConstantsHelper.Zero)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than zero.");

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);
            query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);
            query = query.Skip((page - ConstantsHelper.One) * pageSize).Take(pageSize);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluatePagedAsync.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        IQueryable<T> query,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        if (count <= ConstantsHelper.Zero)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);
            query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);
            query = query.Take(count);

            return await Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateTopAsync.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            IQueryable<T> distinctQuery = query.GroupBy(selector).Select(g => g.First());
            distinctQuery = ApplyOrdering(distinctQuery, orderBy, ascending, thenBy, thenAscending);
            distinctQuery = ApplyPagination(distinctQuery, page, pageSize);

            return await Task.FromResult(distinctQuery);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateDistinctAsync.", ex);
        }
    }

    public async Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        bool asNoTracking = false,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            IQueryable<T> duplicatesQuery = query.GroupBy(selector)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);

            duplicatesQuery = ApplyOrdering(duplicatesQuery, orderBy, ascending, thenBy, thenAscending);
            duplicatesQuery = ApplyPagination(duplicatesQuery, page, pageSize);

            return await Task.FromResult(duplicatesQuery);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateDuplicatesAsync.", ex);
        }
    }


    public async Task<T?> GetLastFailedAsync<TKey, TProperty>(
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

            return await query.LastOrDefaultAsync(condition, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating GetLastFailedAsync.", ex);
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
            throw new InvalidOperationException("Error evaluating GetLastAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).MinAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateMinAsync.", ex);
        }
    }

    public async Task<TResult> EvaluateMaxAsync<
        TResult, TProperty>(
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).MaxAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateMaxAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).AverageAsync(x => Convert.ToDecimal(x), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateMinAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateSumAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateSumAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateSumAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateSumAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            return await query.Select(selector).SumAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateSumAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            List<TResult> values = await query.Select(selector).ToListAsync(cancellationToken);

            if (!values.Any()) return TResult.Zero;

            return values.Aggregate(TResult.Zero, aggregator);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateAggregateAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            List<T> data = await query.ToListAsync(cancellationToken);

            Dictionary<TKey, List<T>> result = data
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.ToList());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateGroupedAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            List<T> data = await query.ToListAsync(cancellationToken);

            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, int> result = groups.ToDictionary(g => g.Key, g => g.Count());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateCountByGroupAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

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
            throw new InvalidOperationException("Error evaluating EvaluateSumByGroupAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

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
            throw new InvalidOperationException("Error evaluating EvaluateMinByGroupAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

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
            throw new InvalidOperationException("Error evaluating EvaluateMaxByGroupAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

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
            throw new InvalidOperationException("Error evaluating EvaluateAverageByGroupAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            List<T> data = await query.ToListAsync(cancellationToken);

            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, List<T>> result = groups
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.ToList());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateDuplicatesByGroupAsync.", ex);
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

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

            List<T> data = await query.ToListAsync(cancellationToken);

            IEnumerable<IGrouping<TKey, T>> groups = data.GroupBy(keySelector);

            Dictionary<TKey, T> result = groups
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First());

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateUniquesByGroupAsync.", ex);
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
        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);
            query = ApplyAsNoTracking(query, asNoTracking);
            query = query.Where(condition);

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
            throw new InvalidOperationException("Error evaluating EvaluateTopByGroupAsync.", ex);
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
    /// <param name="thenBy">The secondary ordering expression. If null, no secondary ordering is applied.</param>
    /// <param name="thenAscending">Determines if the secondary ordering is ascending.</param>
    /// <returns>An <see cref="IQueryable{T}"/> with the specified ordering applied.</returns>
    private IQueryable<T> ApplyOrdering<TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy,
        bool ascending,
        Expression<Func<T, TKey>>? thenBy,
        bool thenAscending)
    {
        if (orderBy == null)
        {
            return query;
        }

        IOrderedQueryable<T> orderedQuery = ascending
            ? query.OrderBy(orderBy)
            : query.OrderByDescending(orderBy);

        if (thenBy != null)
        {
            orderedQuery = thenAscending
                ? orderedQuery.ThenBy(thenBy)
                : orderedQuery.ThenByDescending(thenBy);
        }

        return orderedQuery;
    }


    public IQueryable<T> ApplyPagination(IQueryable<T> query, int? page, int? pageSize)
    {
        if (page.HasValue && pageSize.HasValue)
        {
            if (page.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (pageSize.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

            query = query.Skip((page.Value - ConstantsHelper.One) * pageSize.Value)
                .Take(pageSize.Value);
        }

        return query;
    }

    #endregion
}