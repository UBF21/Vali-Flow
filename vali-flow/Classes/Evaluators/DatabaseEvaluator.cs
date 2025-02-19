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
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return await Task.FromResult(compiledCondition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating the entity.", ex);
        }
    }

    public async Task<bool> EvaluateAnyAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> compiledCondition = _builder.Build();

            query = ApplyIncludes(query, includes);

            return await query.AnyAsync(compiledCondition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating AnyAsync.", ex);
        }
    }

    public Task<int> EvaluateCountAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> compiledCondition = _builder.Build();

            query = ApplyIncludes(query, includes);

            return query.CountAsync(compiledCondition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating CountAsync.", ex);
        }
    }

    public Task<T?> GetFirstFailedAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.BuildNegated();

            query = ApplyIncludes(query, includes);

            return query.FirstOrDefaultAsync(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating GetFirstFailedAsync.", ex);
        }
    }

    public Task<T?> GetFirstAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            Expression<Func<T, bool>> condition = _builder.Build();

            query = ApplyIncludes(query, includes);

            return query.FirstOrDefaultAsync(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating GetFirstAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            query = ApplyIncludes(query, includes);

            Expression<Func<T, bool>> condition = _builder.BuildNegated();

            query = query.Where(condition);

            if (orderBy != null) query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);

            return Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateAllFailedAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            query = ApplyIncludes(query, includes);

            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);

            if (orderBy != null) query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);

            return Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateAllFailedAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        IQueryable<T> query,
        int page,
        int pageSize,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        if (page <= ConstantsHelper.Zero)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        
        if (pageSize <= ConstantsHelper.Zero)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than zero.");

        try
        {
            query = ApplyIncludes(query, includes);

            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);

            if (orderBy != null) query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);

            query = query.Skip((page - ConstantsHelper.One) * pageSize).Take(pageSize);

            return Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluatePagedAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        IQueryable<T> query,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        if (count <= ConstantsHelper.Zero)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

        try
        {
            query = ApplyIncludes(query, includes);

            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);

            if (orderBy != null) query = ApplyOrdering(query, orderBy, ascending, thenBy, thenAscending);

            query = query.Take(count);

            return Task.FromResult(query);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateTopAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            query = ApplyIncludes(query, includes);

            Expression<Func<T, bool>> condition = _builder.Build();

            query = query.Where(condition);

            IQueryable<T> distinctQuery = query.GroupBy(selector).Select(g => g.First());

            if (orderBy != null)
            {
                distinctQuery = ApplyOrdering(distinctQuery, orderBy, ascending, thenBy, thenAscending);
            }

            if (page.HasValue && pageSize.HasValue)
            {
                if (page.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
                
                if (pageSize.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                distinctQuery = distinctQuery.Skip((page.Value - ConstantsHelper.One) * pageSize.Value)
                    .Take(pageSize.Value);
            }

            return Task.FromResult(distinctQuery);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateDistinctAsync.", ex);
        }
    }

    public Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        
        ValidationHelper.ValidateQueryNotNull(query);

        try
        {
            query = ApplyIncludes(query, includes);

            Expression<Func<T, bool>> condition = _builder.Build();
            
            query = query.Where(condition);

            IQueryable<T> duplicatesQuery = query.GroupBy(selector)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
            
            duplicatesQuery = ApplyOrdering(duplicatesQuery, orderBy, ascending, thenBy, thenAscending);
            
            if (page.HasValue && pageSize.HasValue)
            {
                if (page.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
                if (pageSize.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                duplicatesQuery = duplicatesQuery
                    .Skip((page.Value - ConstantsHelper.One) * pageSize.Value)
                    .Take(pageSize.Value);
            }
            
            return Task.FromResult(duplicatesQuery);

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error evaluating EvaluateDuplicatesAsync.", ex);
        }
    }

    public Task<int> GetFirstMatchIndexAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetLastMatchIndexAsync<TProperty>(IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        throw new NotImplementedException();
    }

    public Task<T?> GetLastFailedAsync<TProperty>(IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        throw new NotImplementedException();
    }

    public Task<T?> GetLastAsync<TProperty>(IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
    {
        throw new NotImplementedException();
    }

    public Task<TResult> EvaluateMinAsync<TResult, TProperty>(IQueryable<T> query, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<TResult> EvaluateMaxAsync<TResult, TProperty>(IQueryable<T> query, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<TResult> EvaluateAverageAsync<TResult, TProperty>(IQueryable<T> query, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<TResult> EvaluateSumAsync<TResult, TProperty>(IQueryable<T> query, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<TResult> EvaluateAggregateAsync<TResult, TProperty>(IQueryable<T> query, Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateGroupedAsync<TKey, TProperty>(IQueryable<T> query,
        Func<T, TKey> keySelector, IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey, TProperty>(IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateSumByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query, Func<T, TKey> keySelector, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
        where TKey : notnull where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateMinByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query, Func<T, TKey> keySelector, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
        where TKey : notnull where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateMaxByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query, Func<T, TKey> keySelector, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
        where TKey : notnull where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateAverageByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query, Func<T, TKey> keySelector, Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null)
        where TKey : notnull where TResult : INumber<TResult>
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateDuplicatesByGroupAsync<TKey, TProperty>(
        IQueryable<T> query, Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateUniquesByGroupAsync<TKey, TProperty>(IQueryable<T> query,
        Func<T, TKey> keySelector, IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Dictionary<TKey, T>>> EvaluateTopByGroupAsync<TKey, TProperty>(IQueryable<T> query,
        Func<T, TKey> keySelector, int count, Func<T, object>? orderBy = null,
        bool ascending = true, IEnumerable<Expression<Func<T, TProperty>>>? includes = null) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    #region Private

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
    private  IQueryable<T> ApplyOrdering<TKey>(
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

    #endregion
}