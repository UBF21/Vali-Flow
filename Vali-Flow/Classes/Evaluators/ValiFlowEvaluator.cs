using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Evaluators;

/// <summary>
/// EF Core evaluator that implements both read and write operations for entities of type <typeparamref name="T"/>
/// using the Vali-Flow specification pattern over a <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
/// </summary>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
public partial class ValiFlowEvaluator<T> : IEvaluatorRead<T>, IEvaluatorWrite<T> where T : class
{
    private readonly DbContext _dbContext;
    private readonly QuerySpecificationBuilder<T> _queryBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValiFlowEvaluator{T}"/> class.
    /// </summary>
    /// <param name="dbContext">The Entity Framework Core <see cref="DbContext"/> instance used to perform queries and writes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is <c>null</c>.</exception>
    public ValiFlowEvaluator(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "The DbContext provided is null.");
        _queryBuilder = new QuerySpecificationBuilder<T>(_dbContext);
    }

    #region Methods Private

    // Query construction (filter, includes, tracking, ordering, pagination) lives in QuerySpecificationBuilder<T> —
    // these are thin delegating wrappers kept so Read/Write/Aggregates/Grouped partial files don't need to change.
    private IQueryable<T> BuildBasicQuery(ISpecification<T> specification, bool negateCondition = false)
        => _queryBuilder.BuildBasicQuery(specification, negateCondition);

    private IQueryable<T> BuildQuery(IQuerySpecification<T> specification, bool negateFilter = false)
        => _queryBuilder.BuildQuery(specification, negateFilter);

    private IQueryable<T> ApplyOrdering(IQueryable<T> query, IQuerySpecification<T> specification)
        => _queryBuilder.ApplyOrdering(query, specification);

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
