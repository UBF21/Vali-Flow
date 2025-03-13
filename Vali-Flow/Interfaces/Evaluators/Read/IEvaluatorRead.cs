using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Classes.Options;
using Vali_Flow.Classes.Results;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Interfaces.Evaluators.Read;

/// <summary>
/// Defines asynchronous methods for reading and querying entities using Vali-Flow with Entity Framework support.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IEvaluatorRead<T>
{
    /// <summary>
    /// Asynchronously evaluates whether a single entity satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="entity">The entity to evaluate.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the entity satisfies the condition; otherwise, false.</returns>
    Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity);

    Task<bool> EvaluateAnyAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default);

    Task<int> EvaluateCountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    Task<T?> GetFirstFailedAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    Task<T?> GetFirstAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    Task<IQueryable<T>> EvaluateAllFailedAsync<TKey>(
        ISpecification<T> specification,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;

    Task<IQueryable<T>> EvaluateAllAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;


    Task<IQueryable<T>> EvaluatePagedAsync<TKey>(
        ISpecification<T> specification,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.Ten,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;
    
    
    Task<IQueryable<T>> EvaluateTopAsync<TKey>(
        ISpecification<T> specification,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;


    Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;


    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;


    Task<T?> GetLastFailedAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );


    Task<T?> GetLastAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );


    Task<TResult> EvaluateMinAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;


    Task<TResult> EvaluateMaxAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    
    Task<decimal> EvaluateAverageAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

  
    Task<int> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    );


    Task<long> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    );


    Task<double> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    );


    Task<decimal> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    );


    Task<float> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    );


    Task<TResult> EvaluateAggregateAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;


    Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;


    Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;


    Task<Dictionary<TKey, TResult>> EvaluateSumByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;


    Task<Dictionary<TKey, TResult>> EvaluateMinByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;


    Task<Dictionary<TKey, TResult>> EvaluateMaxByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;


    Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;


    Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;


    Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;


    Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey, TKey2>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        int count,
        Expression<Func<T, TKey2>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TKey2 : notnull;


    Task<IQueryable<T>> EvaluateQuery<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;

    
    Task<PaginatedBlockResult<T>> GetPaginatedBlockAsync<TKey>(
        ISpecification<T> specification,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;


    Task<IQueryable<T>> GetPaginatedBlockQueryAsync<TKey>(
        ISpecification<T> specification,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null
    ) where TKey : notnull;
}