using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Models;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

public sealed partial class ValiFlowEvaluator<T>
{
    /// <summary>
    /// Evaluates whether a single entity satisfies the given <see cref="ValiFlow{T}"/> condition.
    /// The compiled predicate is cached for reuse.
    /// </summary>
    /// <param name="valiFlow">The fluent condition builder to evaluate.</param>
    /// <param name="entity">The entity to test.</param>
    /// <returns><c>true</c> if the entity satisfies the condition; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="valiFlow"/> or <paramref name="entity"/> is <c>null</c>.</exception>
    public Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity)
    {
        if (valiFlow == null) throw new ArgumentNullException(nameof(valiFlow));
        Validation.ValidateEntityNotNull(entity);
        var condition = valiFlow.BuildCached();
        return Task.FromResult(condition(entity));
    }

    /// <summary>
    /// Returns <c>true</c> if at least one entity in the database matches the specification filter.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if any matching entity exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    public async Task<bool> EvaluateAnyAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.AnyAsync(cancellationToken),
            nameof(EvaluateAnyAsync));
    }

    /// <summary>
    /// Returns the number of entities in the database that match the specification filter.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    public async Task<int> EvaluateCountAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.CountAsync(cancellationToken),
            nameof(EvaluateCountAsync));
    }

    /// <summary>
    /// Returns the first entity that does <em>not</em> match the specification filter, or <c>null</c> if none exists.
    /// Equivalent to applying a logical NOT to the specification condition.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first non-matching entity, or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    public async Task<T?> EvaluateGetFirstFailedAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        var query = BuildBasicQuery(specification, true);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstFailedAsync));
    }

    /// <summary>
    /// Returns the first entity that matches the specification filter, or <c>null</c> if none exists.
    /// </summary>
    /// <param name="specification">Specification that defines the filter and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first matching entity, or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    public async Task<T?> EvaluateGetFirstAsync(
        IBasicSpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        var query = BuildBasicQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.FirstOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetFirstAsync));
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of entities that do <em>not</em> match the specification filter,
    /// with ordering and pagination applied.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <returns>A queryable over the non-matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    public Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildQuery(specification, negateFilter: true);
        return Task.FromResult(query);
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of entities that match the specification filter,
    /// with ordering and pagination applied.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <returns>A queryable over the matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    public Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        IQueryable<T> query = BuildQuery(specification);
        return Task.FromResult(query);
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> containing the first <paramref name="count"/> entities
    /// that match the specification filter, with ordering applied.
    /// Cannot be combined with pagination (<c>Page</c>/<c>PageSize</c>).
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, and EF Core query hints.</param>
    /// <param name="count">Maximum number of entities to return. Must be greater than zero.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A queryable limited to the top <paramref name="count"/> matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when both Top and pagination are set on the specification.</exception>
    public Task<IQueryable<T>> EvaluateTopAsync(
        IQuerySpecification<T> specification,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        if ((specification.Page.HasValue || specification.PageSize.HasValue) && count > 0)
            throw new InvalidOperationException("Cannot use Top with pagination. Use either Top or Page/PageSize.");
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);
        return Task.FromResult(query.Take(count));
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of all entities matching the specification.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <returns>A queryable over the matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    [Obsolete("Use EvaluateQueryAsync instead.", false)]
    public Task<IQueryable<T>> EvaluateAllAsync(IQuerySpecification<T> specification)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        return EvaluateQueryAsync(specification);
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of all entities that do <em>not</em> match the specification.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <returns>A queryable over the non-matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    [Obsolete("Use EvaluateQueryFailedAsync instead.", false)]
    public Task<IQueryable<T>> EvaluateAllFailedAsync(IQuerySpecification<T> specification)
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        return EvaluateQueryFailedAsync(specification);
    }

    /// <summary>
    /// Returns one representative entity per distinct key value among the entities matching the specification.
    /// Ordering and pagination (Page/PageSize or Top) are applied after in-memory deduplication.
    /// </summary>
    /// <typeparam name="TKey">Type of the key used for deduplication.</typeparam>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the key property used for deduplication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sequence of distinct entities, one per unique key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when pagination is requested without an ordering, or when Page and PageSize are not set together.</exception>
    /// <remarks>This method loads all matching entities into memory before grouping. For large unfiltered tables, apply a filter in the specification to limit the result set.</remarks>
    public async Task<IEnumerable<T>> EvaluateDistinctAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (specification.Page.HasValue != specification.PageSize.HasValue)
            throw new InvalidOperationException("Both Page and PageSize must be set together, or neither.");
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        bool hasPagination = specification.PageSize.HasValue || specification.Top.HasValue;
        bool hasOrdering = specification.OrderBy != null || specification.ValiSort != null;
        if (hasPagination && !hasOrdering)
            throw new InvalidOperationException("Pagination requires an ordering.");
        Func<T, TKey> keySelectorFn = selector.Compile();
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);
        List<T> items = await query.ToListAsync(cancellationToken);
        IEnumerable<T> result = items.GroupBy(keySelectorFn).Select(g => g.First());
        // Pagination applied AFTER grouping so it operates on distinct groups, not raw rows
        if (specification is { Page: not null, PageSize: not null })
            result = result.Skip((specification.Page.Value - Constants.One) * specification.PageSize.Value)
                           .Take(specification.PageSize.Value);
        else if (specification.Top != null)
            result = result.Take(specification.Top.Value);
        return result;
    }

    /// <summary>
    /// Returns all entities whose key value appears more than once among entities matching the specification.
    /// Ordering and pagination (Page/PageSize or Top) are applied after in-memory duplicate detection.
    /// </summary>
    /// <typeparam name="TKey">Type of the key used for duplicate detection.</typeparam>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <param name="selector">Expression that projects the key property used for duplicate detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sequence of all duplicate entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> or <paramref name="selector"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when pagination is requested without an ordering, or when Page and PageSize are not set together.</exception>
    /// <remarks>This method loads all matching entities into memory before grouping. For large unfiltered tables, apply a filter in the specification to limit the result set.</remarks>
    public async Task<IEnumerable<T>> EvaluateDuplicatesAsync<TKey>(
        IQuerySpecification<T> specification,
        Expression<Func<T, TKey>> selector,
        CancellationToken cancellationToken = default
    ) where TKey : notnull
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (specification.Page.HasValue != specification.PageSize.HasValue)
            throw new InvalidOperationException("Both Page and PageSize must be set together, or neither.");
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        bool hasPagination = specification.PageSize.HasValue || specification.Top.HasValue;
        bool hasOrdering = specification.OrderBy != null || specification.ValiSort != null;
        if (hasPagination && !hasOrdering)
            throw new InvalidOperationException("Pagination requires an ordering.");
        Func<T, TKey> keySelectorFn = selector.Compile();
        IQueryable<T> query = BuildBasicQuery(specification);
        query = ApplyOrdering(query, specification);
        List<T> items = await query.ToListAsync(cancellationToken);
        IEnumerable<T> result = items.GroupBy(keySelectorFn)
            .Where(g => g.Count() > Constants.One)
            .SelectMany(g => g);
        // Pagination applied AFTER grouping so it operates on the duplicate set, not raw rows
        if (specification is { Page: not null, PageSize: not null })
            result = result.Skip((specification.Page.Value - Constants.One) * specification.PageSize.Value)
                           .Take(specification.PageSize.Value);
        else if (specification.Top != null)
            result = result.Take(specification.Top.Value);
        return result;
    }

    /// <summary>
    /// Returns the last entity that does <em>not</em> match the specification filter, or <c>null</c> if none exists.
    /// Requires an explicit ordering; pagination is not supported.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The last non-matching entity, or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when pagination is set or when no ordering is specified.</exception>
    public async Task<T?> EvaluateGetLastFailedAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (specification.Page.HasValue || specification.PageSize.HasValue)
            throw new InvalidOperationException(
                "EvaluateGetLastFailedAsync does not support pagination. Use EvaluateQueryAsync for paged queries.");
        if (specification.OrderBy == null && specification.ValiSort == null)
            throw new InvalidOperationException(
                $"{nameof(EvaluateGetLastFailedAsync)} requires an ordering (OrderBy or ValiSort). EF Core cannot translate LastOrDefault without ORDER BY.");

        IQueryable<T> query = BuildQuery(specification, negateFilter: true);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastFailedAsync));
    }

    /// <summary>
    /// Returns the last entity that matches the specification filter, or <c>null</c> if none exists.
    /// Requires an explicit ordering; pagination is not supported.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The last matching entity, or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when pagination is set or when no ordering is specified.</exception>
    public async Task<T?> EvaluateGetLastAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (specification.Page.HasValue || specification.PageSize.HasValue)
            throw new InvalidOperationException(
                "EvaluateGetLastAsync does not support pagination. Use EvaluateQueryAsync for paged queries.");
        if (specification.OrderBy == null && specification.ValiSort == null)
            throw new InvalidOperationException(
                $"{nameof(EvaluateGetLastAsync)} requires an ordering (OrderBy or ValiSort). EF Core cannot translate LastOrDefault without ORDER BY.");

        IQueryable<T> query = BuildQuery(specification);
        return await ExecuteWithExceptionHandlingAsync(() => query.LastOrDefaultAsync(cancellationToken),
            nameof(EvaluateGetLastAsync));
    }

    /// <summary>
    /// Executes a paginated query and returns a <see cref="PagedResult{T}"/> containing the requested page of entities
    /// along with the total count and pagination metadata.
    /// Requires both <c>Page</c> and <c>PageSize</c> to be set on the specification, as well as an explicit ordering.
    /// </summary>
    /// <param name="specification">Specification that defines the filter, ordering, pagination, and EF Core query hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PagedResult{T}"/> with the page items, total count, and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="specification"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>Page</c> or <c>PageSize</c> is not set on the specification.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>Page</c> or <c>PageSize</c> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no ordering is specified.</exception>
    /// <remarks>Issues two separate database round-trips (COUNT then SELECT). In concurrent scenarios the total count may be stale relative to the returned items. Wrap both calls in a transaction if strict consistency is required.</remarks>
    public async Task<PagedResult<T>> EvaluatePagedAsync(
        IQuerySpecification<T> specification,
        CancellationToken cancellationToken = default
    )
    {
        if (specification == null) throw new ArgumentNullException(nameof(specification));
        if (!specification.Page.HasValue)
            throw new ArgumentException(
                $"{nameof(EvaluatePagedAsync)} requires Page to be explicitly set on the specification.",
                nameof(specification));
        if (!specification.PageSize.HasValue)
            throw new ArgumentException(
                $"{nameof(EvaluatePagedAsync)} requires PageSize to be explicitly set on the specification.",
                nameof(specification));

        bool hasOrdering = specification.OrderBy != null || specification.ValiSort != null;
        if (!hasOrdering)
            throw new InvalidOperationException(
                $"{nameof(EvaluatePagedAsync)} requires an ordering (OrderBy or ValiSort) for deterministic pagination results.");

        int page = specification.Page.Value;
        int pageSize = specification.PageSize.Value;

        if (page < 1) throw new ArgumentOutOfRangeException(nameof(specification), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(specification), "PageSize must be greater than or equal to 1.");

        IQueryable<T> baseQuery = BuildBasicQuery(specification);

        int totalCount = await ExecuteWithExceptionHandlingAsync(
            () => baseQuery.CountAsync(cancellationToken),
            nameof(EvaluatePagedAsync));

        IQueryable<T> orderedQuery = ApplyOrdering(baseQuery, specification);
        int skip = (page - Constants.One) * pageSize;
        IList<T> items = await ExecuteWithExceptionHandlingAsync(
            () => orderedQuery.Skip(skip).Take(pageSize).ToListAsync(cancellationToken),
            nameof(EvaluatePagedAsync));

        return new PagedResult<T>(items.AsReadOnly(), totalCount, page, pageSize);
    }
}
