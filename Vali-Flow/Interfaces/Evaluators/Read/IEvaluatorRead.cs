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

    /// <summary>
    /// Asynchronously evaluates whether any entity satisfies the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if at least one entity satisfies the specification; otherwise, false.</returns>
    Task<bool> EvaluateAnyAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the number of entities that satisfy the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the total count of entities.</returns>
    Task<int> EvaluateCountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves the first entity that fails the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first entity that fails the specification or null if none fail.</returns>
    Task<T?> GetFirstFailedAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves the first entity that satisfies the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first entity that satisfies the specification or null if none match.</returns>
    Task<T?> GetFirstAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    Task<IQueryable<T>> EvaluateQueryFailedAsync(ISpecification<T> specification);

    //Task<IQueryable<T>> EvaluateAllAsync(ISpecification<T> specification);
    
    //Task<IQueryable<T>> EvaluatePagedAsync(ISpecification<T> specification);
    
    //Task<IQueryable<T>> EvaluateTopAsync(ISpecification<T> specification);
    
    Task<IQueryable<T>> EvaluateQueryAsync(ISpecification<T> specification);

    Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>> selector
    )where TKey : notnull;

    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves the last entity that fails the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the last entity that fails the specification or null if none fail.</returns>
    Task<T?> GetLastFailedAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves the last entity that satisfies the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the last entity that satisfies the specification or null if none match.</returns>
    Task<T?> GetLastAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously evaluates and returns the minimum value of a selected property from entities that satisfy the specification.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the minimum value of the selected property.</returns>
    Task<TResult> EvaluateMinAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously evaluates and returns the maximum value of a selected property from entities that satisfy the specification.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the maximum value of the selected property.</returns>
    Task<TResult> EvaluateMaxAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously evaluates and returns the average value of a selected property from entities that satisfy the specification.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the average value as a decimal.</returns>
    Task<decimal> EvaluateAverageAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously evaluates and returns the sum of a selected integer property from entities that satisfy the specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the integer property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected integer property.</returns>
    Task<int> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously evaluates and returns the sum of a selected long property from entities that satisfy the specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the long property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected long property.</returns>
    Task<long> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously evaluates and returns the sum of a selected double property from entities that satisfy the specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the double property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected double property.</returns>
    Task<double> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously evaluates and returns the sum of a selected decimal property from entities that satisfy the specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the decimal property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected decimal property.</returns>
    Task<decimal> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously evaluates and returns the sum of a selected float property from entities that satisfy the specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the float property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected float property.</returns>
    Task<float> EvaluateSumAsync(
        ISpecification<T> specification,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously evaluates and returns an aggregated result of a selected property from entities that satisfy the specification.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the property to aggregate.</param>
    /// <param name="aggregator">The function to aggregate the values.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the aggregated result.</returns>
    Task<TResult> EvaluateAggregateAsync<TResult>(
        ISpecification<T> specification,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary grouping entities by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The expression to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and lists of entities.</returns>
    Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey>(
        ISpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the count of entities grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their respective counts.</returns>
    Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the sum of a selected property grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="selector">The function to select the property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their sums.</returns>
    Task<Dictionary<TKey, TResult>> EvaluateSumByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the minimum value of a selected property grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="selector">The function to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their minimum values.</returns>
    Task<Dictionary<TKey, TResult>> EvaluateMinByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the maximum value of a selected property grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="selector">The function to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their maximum values.</returns>
    Task<Dictionary<TKey, TResult>> EvaluateMaxByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the average value of a selected property grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="selector">The function to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their average values as decimals.</returns>
    Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with lists of duplicate entities grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and lists of duplicate entities.</returns>
    Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with unique entities grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The function to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their unique entities.</returns>
    Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
        ISpecification<T> specification,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    //Task<IQueryable<T>> EvaluateQuery(ISpecification<T> specification);

    Task<PaginatedBlockResult<T>> GetPaginatedBlockAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );


    //Task<IQueryable<T>> GetPaginatedBlockQueryAsync(ISpecification<T> specification);
}