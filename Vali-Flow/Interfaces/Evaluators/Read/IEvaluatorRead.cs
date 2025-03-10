using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Classes.Options;
using Vali_Flow.Classes.Results;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;

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
    /// Asynchronously determines whether any entity in the data source satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if at least one entity satisfies the condition; otherwise, false.</returns>
    Task<bool> EvaluateAnyAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the number of entities in the data source that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the number of entities that satisfy the condition.</returns>
    Task<int> EvaluateCountAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves the first entity that does not satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first entity that fails the condition, or null if none fail.</returns>
    Task<T?> GetFirstFailedAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves the first entity that satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the first entity that satisfies the condition, or null if none satisfy.</returns>
    Task<T?> GetFirstAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously retrieves all entities that do not satisfy the specified Vali-Flow condition, with optional pagination and ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="page">The optional page number (1-based) for pagination. If null, no pagination is applied.</param>
    /// <param name="pageSize">The optional number of entities per page. If null, no pagination is applied.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of entities that fail the condition.</returns>
    Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves all entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of entities that satisfy the condition.</returns>
    Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves a paginated subset of entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of entities per page.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of paginated entities that satisfy the condition.</returns>
    Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.Ten,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves the top N entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="count">The maximum number of entities to return.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of the top N entities that satisfy the condition.</returns>
    Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves distinct entities based on a selector, with optional pagination and ordering, that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for selection and ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the key for determining distinctness.</param>
    /// <param name="page">The optional page number (1-based) for pagination. If null, no pagination is applied.</param>
    /// <param name="pageSize">The optional number of entities per page. If null, no pagination is applied.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of distinct entities that satisfy the condition.</returns>
    Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves entities that have duplicate values based on a selector, with optional pagination and ordering, that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for selection and ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the key for determining duplicates.</param>
    /// <param name="page">The optional page number (1-based) for pagination. If null, no pagination is applied.</param>
    /// <param name="pageSize">The optional number of entities per page. If null, no pagination is applied.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of entities with duplicate keys that satisfy the condition.</returns>
    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves the last entity that does not satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering (unused in this method but required for consistency).</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the last entity that fails the condition, or null if none fail.</returns>    
    Task<T?> GetLastFailedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves the last entity that satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the last entity that satisfies the condition, or null if none satisfy.</returns>
    Task<T?> GetLastAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously computes the minimum value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the property value to evaluate.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the minimum value of the selected property for entities that satisfy the condition.</returns>
    Task<TResult> EvaluateMinAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously computes the maximum value of a selected property for entities that satisfy the specified ValiFlow condition.
    /// </summary>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the property value to evaluate.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the maximum value of the selected property for entities that satisfy the condition.</returns>
    Task<TResult> EvaluateMaxAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously computes the average value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the property value to evaluate.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the average value of the selected property as a decimal.</returns>
    Task<decimal> EvaluateAverageAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously computes the sum of a selected integer property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the integer property value to sum.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected integer property values.</returns>
    Task<int> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, int>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously computes the sum of a selected long property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the long property value to sum.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected long property values.</returns>
    Task<long> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, long>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously computes the sum of a selected long property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the long property value to sum.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected long property values.</returns>
    Task<double> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, double>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously computes the sum of a selected long property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the long property value to sum.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected long property values.</returns>
    Task<decimal> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, decimal>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously computes the sum of a selected decimal property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the decimal property value to sum.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the sum of the selected decimal property values.</returns>
    Task<float> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, float>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Asynchronously applies a custom aggregation to a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="selector">An expression to extract the property value to aggregate.</param>
    /// <param name="aggregator">A function that defines how to aggregate two values into a single result.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the aggregated result of the selected property values.</returns>
    Task<TResult> EvaluateAggregateAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is a list of entities in that group.</returns>
    Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and counts the entities in each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is the count of entities in that group.</returns>
    Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and computes the sum of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="selector">An expression to extract the property value to sum.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is the sum of the selected property for that group.</returns>
    Task<Dictionary<TKey, TResult>> EvaluateSumByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and computes the minimum value of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="selector">An expression to extract the property value to evaluate.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is the minimum of the selected property for that group.</returns>
    Task<Dictionary<TKey, TResult>> EvaluateMinByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and computes the maximum value of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="selector">An expression to extract the property value to evaluate.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is the maximum of the selected property for that group.</returns>
    Task<Dictionary<TKey, TResult>> EvaluateMaxByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and computes the average value of a selected property for each group.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TResult" TResult=".">The type of the selected property, must implement INumber</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="selector">An expression to extract the property value to average.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is the average of the selected property for that group as a decimal.</returns>
    Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and returns groups with more than one element (duplicates).
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is a list of entities with duplicate keys.</returns>
    Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and returns groups with exactly one element (uniques).
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is the single entity in that group.</returns>
    Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously groups entities that satisfy the specified Vali-Flow condition by a key and returns the top N entities for each group, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for grouping.</typeparam>
    /// <typeparam name="TKey2">The type of the key used for ordering within groups.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="keySelector">An expression to extract the grouping key from each entity.</param>
    /// <param name="count">The maximum number of entities to return per group.</param>
    /// <param name="orderBy">Optional expression to extract the key for ordering within each group. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a dictionary where each key is a grouping key and the value is a list of the top N entities in that group, ordered as specified.</returns>
    Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey, TKey2, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        int count,
        Expression<Func<T, TKey2>>? orderBy = null,
        bool ascending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TKey2 : notnull;

    /// <summary>
    /// Asynchronously builds and returns a queryable of entities based on the specified Vali-Flow condition, with optional ordering and includes.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of entities based on the specified criteria.</returns>
    Task<IQueryable<T>> EvaluateQuery<TProperty, TKey>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously retrieves a paginated block of entities that satisfy the specified Vali -Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="blockSize">The number of entities per block (default is 1000).</param>
    /// <param name="page">The page number to retrieve within the block (1-based, default is 1).</param>
    /// <param name="pageSize">The number of entities per page within the block (default is 100).</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning a <see cref="PaginatedBlockResult{T}"/> containing the paginated block of entities.</returns>
    Task<PaginatedBlockResult<T>> GetPaginatedBlockAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    /// <summary>
    /// Asynchronously builds a queryable for a paginated block of entities that satisfy the specified Vali-Flow condition, with optional ordering.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <typeparam name="TProperty">The type of the related entities to include.</typeparam>
    /// <param name="valiFlow">The Vali-Flow condition to apply.</param>
    /// <param name="blockSize">The number of entities per block (default is 1000).</param>
    /// <param name="page">The page number to retrieve within the block (1-based, default is 1).</param>
    /// <param name="pageSize">The number of entities per page within the block (default is 100).</param>
    /// <param name="orderBy">Optional expression to extract the key for primary ordering. If null, no ordering is applied.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <param name="thenBys">Optional collection of secondary ordering specifications.</param>
    /// <param name="includes">Optional collection of expressions to include related entities. If null, no includes are applied.</param>
    /// <param name="asNoTracking">If true, disables change tracking for the query.</param>
    /// <param name="asSplitQuery">If true and includes are present, splits the query into separate requests for better performance.</param>
    /// <returns>A task that represents the asynchronous operation, returning an IQueryable of the paginated block of entities.</returns>
    Task<IQueryable<T>> GetPaginatedBlockQueryAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;
}