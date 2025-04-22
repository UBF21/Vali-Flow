using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Classes.Results;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Interfaces.Evaluators.Read;

/// <summary>
/// Defines asynchronous methods for reading and querying entities using Vali-Flow with Entity Framework support.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IEvaluatorRead<T> where T : class
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
    Task<bool> EvaluateAnyAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the number of entities that satisfy the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the total count of entities.</returns>
    Task<int> EvaluateCountAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the first entity that fails the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first entity that fails the specification or null if none fail.</returns>
    Task<T?> EvaluateGetFirstFailedAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the first entity that satisfies the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first entity that satisfies the specification or null if none match.</returns>
    Task<T?> EvaluateGetFirstAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously evaluates a query for entities that fail the provided specification, applying filtering (negated), ordering, pagination, and block-based retrieval as defined.
    /// </summary>
    /// <param name="specification">The specification defining the filtering, ordering, pagination, and block criteria.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of T representing the entities that fail the specification.</returns>
    Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification);

    /// <summary>
    /// Asynchronously evaluates a query based on the provided specification, applying filtering, ordering, pagination, and block-based retrieval as defined.
    /// </summary>
    /// <param name="specification">The specification defining the filtering, ordering, pagination, and block criteria.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of T representing the evaluated query.</returns>
    Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification);

    /// <summary>
    /// Evaluates a query to retrieve distinct entities based on a specified key selector, applying the provided specification.
    /// The method groups entities by the given key selector and selects the first entity from each group, ensuring uniqueness based on the key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping entities, which must be non-nullable.</typeparam>
    /// <param name="specification">The query specification defining the filtering, ordering, and pagination criteria for the query. Must not be null.</param>
    /// <param name="selector">An expression that defines the key to group entities by for determining uniqueness. Must not be null.</param>
    /// <returns>
    /// A task that resolves to an <see cref="IQueryable{T}"/> containing the distinct entities based on the specified key.
    /// The query is not executed immediately; it can be further composed or executed later.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="specification"/> or <paramref name="selector"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specification contains invalid properties, such as a null filter, invalid pagination values, or an invalid block size.
    /// </exception>
    /// <remarks>
    /// This method applies the filtering, ordering, and pagination defined in the specification after grouping by the key selector.
    /// If pagination or block size is specified in the <paramref name="specification"/>, it will be applied to the resulting query.
    /// </remarks>
    Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull;

    /// <summary>
    /// Evaluates a query to retrieve duplicate entities based on a specified key selector, applying the provided specification.
    /// The method groups entities by the given key selector and selects all entities from groups that contain more than one item, identifying duplicates.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping entities, which must be non-nullable.</typeparam>
    /// <param name="specification">The query specification defining the filtering, ordering, and pagination criteria for the query. Must not be null.</param>
    /// <param name="selector">An expression that defines the key to group entities by for identifying duplicates. Must not be null.</param>
    /// <returns>
    /// A task that resolves to an <see cref="IQueryable{T}"/> containing the duplicate entities based on the specified key.
    /// The query is not executed immediately; it can be further composed or executed later.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="specification"/> or <paramref name="selector"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specification contains invalid properties, such as a null filter, invalid pagination values, or an invalid block size.
    /// </exception>
    /// <remarks>
    /// This method applies the filtering, ordering, and pagination defined in the specification after identifying duplicates.
    /// Only groups with more than one entity are included in the result. If pagination or block size is specified in the <paramref name="specification"/>,
    /// it will be applied to the resulting query.
    /// </remarks>
    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves the last entity that fails the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the last entity that fails the specification or null if none fail.</returns>
    Task<T?> EvaluateGetLastFailedAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves the last entity that satisfies the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the last entity that satisfies the specification or null if none match.</returns>
    Task<T?> EvaluateGetLastAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously evaluates and returns the minimum value of a selected property from entities that satisfy the specification.
    /// </summary>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="selector">The expression to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the minimum value of the selected property.</returns>
    Task<TResult> EvaluateMinAsync<TResult>(
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
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
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the count of entities grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The expression function to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their respective counts.</returns>
    Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the sum of integer values for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the integer value to sum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the sum of integer values for that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/> or <paramref name="keySelector"/> or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, int>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the sum of long integer values for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the long integer value to sum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the sum of long integer values for that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/> or <paramref name="keySelector"/> or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, long>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the sum of float values for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the float value to sum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the sum of float values for that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/> or <paramref name="keySelector"/> or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, float>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the sum of double values for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the double value to sum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the sum of double values for that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/> or <paramref name="keySelector"/> or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, double>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the sum of decimal values for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the decimal value to sum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the sum of decimal values for that group.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/> or <paramref name="keySelector"/> or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, decimal>> EvaluateSumByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the minimum integer value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the integer value to find the minimum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the minimum integer value for that group. Returns default(int) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, int>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;
    
    /// <summary>
    /// Groups entities by a specified key and computes the minimum long integer value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the long integer value to find the minimum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the minimum long integer value for that group. Returns default(long) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, long>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the minimum float value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the float value to find the minimum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the minimum float value for that group. Returns default(float) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, float>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the minimum double value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the double value to find the minimum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the minimum double value for that group. Returns default(double) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, double>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the minimum decimal value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the decimal value to find the minimum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the minimum decimal value for that group. Returns default(decimal) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public  Task<Dictionary<TKey, decimal>> EvaluateMinByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Groups entities by a specified key and computes the maximum integer value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the integer value to find the maximum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the maximum integer value for that group. Returns default(int) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, int>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;
    
    /// <summary>
    /// Groups entities by a specified key and computes the maximum long integer value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the long integer value to find the maximum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the maximum long integer value for that group. Returns default(long) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, long>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull; 
    
    /// <summary>
    /// Groups entities by a specified key and computes the maximum float value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the float value to find the maximum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the maximum float value for that group. Returns default(float) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, float>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, float>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull; 
    
    /// <summary>
    /// Groups entities by a specified key and computes the maximum double value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the double value to find the maximum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the maximum double value for that group. Returns default(double) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, double>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;   
    
    /// <summary>
    /// Groups entities by a specified key and computes the maximum decimal value for each group asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to group by. Must not be null.</typeparam>
    /// <param name="specification">The specification defining the filter criteria for the entities.</param>
    /// <param name="keySelector">An expression that selects the key to group by.</param>
    /// <param name="selector">An expression that selects the decimal value to find the maximum for each entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each group key to the maximum decimal value for that group. Returns default(decimal) for empty groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="specification"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the query execution fails.</exception>
    public Task<Dictionary<TKey, decimal>> EvaluateMaxByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with the average value of a selected property grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <typeparam name="TResult">The type of the result, which must implement INumber.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The expression function to select the key for grouping.</param>
    /// <param name="selector">The expression function to select the property to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their average values as decimals.</returns>
    Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with lists of duplicate entities grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The expression function to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and lists of duplicate entities.</returns>
    Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously evaluates and returns a dictionary with unique entities grouped by a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key, which must be non-nullable.</typeparam>
    /// <param name="specification">The specification defining the filtering and inclusion criteria.</param>
    /// <param name="keySelector">The expression function to select the key for grouping.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary with keys and their unique entities.</returns>
    Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
        IBasicSpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Evaluates a query to retrieve the top entities based on the specified <see cref="IQuerySpecification{T}"/> and groups them by a key selector.
    /// The method applies the ordering and top limit defined in the specification, then groups the resulting entities by the provided key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping entities, which must be non-nullable.</typeparam>
    /// <param name="specification">
    /// The query specification defining the filtering, ordering, and top limit criteria for the query. Must not be null.
    /// The <see cref="IQuerySpecification{T}.Top"/> property must be specified to define the maximum number of entities to retrieve.
    /// </param>
    /// <param name="keySelector">A expression function that defines the key to group entities by. Must not be null.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A task that resolves to a dictionary where the keys are of type <typeparamref name="TKey"/> and the values are lists of entities of type <typeparamref name="T"/>.
    /// Each list contains the entities that share the same key, limited to the top number specified in the <paramref name="specification"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="specification"/> or <paramref name="keySelector"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specification contains invalid properties, such as a null filter, or if the <see cref="IQuerySpecification{T}.Top"/> property is not specified.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method applies the filtering and ordering defined in the <paramref name="specification"/> before selecting the top entities.
    /// If <see cref="IQuerySpecification{T}.Top"/> is not specified, the method defaults to taking 50 entities as defined by <see cref="ConstantHelper.Fifty"/>.
    /// Pagination properties like <see cref="IQuerySpecification{T}.Page"/> and <see cref="IQuerySpecification{T}.PageSize"/> are ignored in this method.
    /// </remarks>
    Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> keySelector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;
    
}