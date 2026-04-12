using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

/// <summary>
/// EF Core evaluator that implements both read and write operations for entities of type <typeparamref name="T"/>
/// using the Vali-Flow specification pattern over a <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
/// </summary>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
public sealed partial class ValiFlowEvaluator<T> : IEvaluatorRead<T>, IEvaluatorWrite<T> where T : class
{
    private readonly DbContext _dbContext;

    public ValiFlowEvaluator(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "The DbContext provided is null.");
    }

    #region Methods Private

    private IQueryable<T> ApplyAsNoTracking(IQueryable<T> query, bool asNoTracking = true) => asNoTracking ? query.AsNoTracking() : query;

    private IQueryable<T> ApplyAsNoTrackingWithIdentityResolution(IQueryable<T> query, bool apply)
        => apply ? query.AsNoTrackingWithIdentityResolution() : query;

    private IQueryable<T> ApplyTagWith(IQueryable<T> query, string? tag)
        => tag != null ? query.TagWith(tag) : query;

    private IQueryable<T> ApplyIgnoreAutoIncludes(IQueryable<T> query, bool apply)
        => apply ? query.IgnoreAutoIncludes() : query;

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
        if (specification.ValiSort != null)
            return specification.ValiSort.Apply(query);

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

    private IQueryable<T> ApplyAsSplitQuery(IQueryable<T> query, bool asSplitQuery = false) => asSplitQuery ? query.AsSplitQuery() : query;

    private IQueryable<T> BuildBasicQuery(ISpecification<T> specification, bool negateCondition = false)
    {
        IQueryable<T> query = _dbContext.Set<T>();

        query = ApplyIgnoreQueryFilters(query, specification.IgnoreQueryFilters);
        query = ApplyIgnoreAutoIncludes(query, specification.IgnoreAutoIncludes);
        query = ApplyWhere(query, specification.Filter, negateCondition);

        if (specification.AsNoTracking && specification.AsNoTrackingWithIdentityResolution)
            throw new InvalidOperationException(
                "AsNoTracking and AsNoTrackingWithIdentityResolution are mutually exclusive. " +
                "Use WithAsNoTracking(false) before calling WithAsNoTrackingWithIdentityResolution(true).");

        query = ApplyAsNoTracking(query, specification.AsNoTracking);
        query = ApplyAsNoTrackingWithIdentityResolution(query, specification.AsNoTrackingWithIdentityResolution);
        query = ApplyIncludes(query, specification.Includes);
        query = ApplyAsSplitQuery(query, specification.AsSplitQuery);
        query = ApplyTagWith(query, specification.TagWith);

        return query;
    }

    private IQueryable<T> ApplyWhere(IQueryable<T> query, ValiFlowQuery<T> filter, bool negateCondition = false)
    {
        Validation.ValidateFilterNotNull(filter);

        Expression<Func<T, bool>> condition = negateCondition ? filter.BuildNegated() : filter.BuildWithGlobal();
        return query.Where(condition);
    }

    private IQueryable<T> BuildQuery(IQuerySpecification<T> specification, bool negateFilter = false)
    {
        if (specification.Top.HasValue && (specification.Page.HasValue || specification.PageSize.HasValue))
            throw new InvalidOperationException(
                "Cannot combine Top with Page/PageSize pagination on the same specification. Use one or the other.");

        if ((specification.Page.HasValue && specification.Page.Value > 0 && (!specification.PageSize.HasValue || specification.PageSize.Value <= 0)) ||
            (specification.PageSize.HasValue && specification.PageSize.Value > 0 && (!specification.Page.HasValue || specification.Page.Value <= 0)))
            throw new InvalidOperationException(
                "Both Page and PageSize must be set together. Use WithPagination(page, pageSize).");

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
        List<ValueTuple<TKey, TResult>> pairs =
            await ProjectToPairsAsync(query, keySelector, valueSelector, operationName, cancellationToken);
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
        List<ValueTuple<TKey, TResult>> pairs =
            await ProjectToPairsAsync(query, keySelector, valueSelector, operationName, cancellationToken);
        return pairs
            .GroupBy(p => p.Item1)
            .ToDictionary(g => g.Key, g => g.Average(p => Convert.ToDecimal(p.Item2)));
    }

    private async Task<List<ValueTuple<TKey, TValue>>> ProjectToPairsAsync<TKey, TValue>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TValue>> valueSelector,
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
            typeof(ValueTuple<TKey, TValue>).GetConstructor(new[] { typeof(TKey), typeof(TValue) })!;
        NewExpression tupleNew = Expression.New(tupleCtorInfo, keyBody, valueBody);
        Expression<Func<T, ValueTuple<TKey, TValue>>> projectionLambda =
            Expression.Lambda<Func<T, ValueTuple<TKey, TValue>>>(tupleNew, sharedParam);
        return await ExecuteWithExceptionHandlingAsync(
            () => query.Select(projectionLambda).ToListAsync(cancellationToken),
            operationName);
    }

    #endregion
}
