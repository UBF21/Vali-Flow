using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Evaluators;

/// <summary>
/// Translates an <see cref="ISpecification{T}"/>/<see cref="IQuerySpecification{T}"/> into an EF Core
/// <see cref="IQueryable{T}"/> — filter, includes, tracking/split-query hints, ordering and pagination.
/// Extracted from <see cref="ValiFlowEvaluator{T}"/> so query-construction logic (a single, self-contained
/// responsibility) does not share state or private helpers with the read/write/aggregate operations that
/// consume it.
/// </summary>
internal sealed class QuerySpecificationBuilder<T> where T : class
{
    private readonly DbContext _dbContext;

    public QuerySpecificationBuilder(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<T> BuildBasicQuery(ISpecification<T> specification, bool negateCondition = false)
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

    public IQueryable<T> BuildQuery(IQuerySpecification<T> specification, bool negateFilter = false)
    {
        ValidatePaginationAndOrdering(specification);

        IQueryable<T> query = BuildBasicQuery(specification, negateFilter);
        query = ApplyOrdering(query, specification);
        query = ApplyPagination(query, specification);
        return query;
    }

    private static void ValidatePaginationAndOrdering(IQuerySpecification<T> specification)
    {
        EnsureTopNotCombinedWithPaging(specification);
        EnsurePageAndPageSizeSetTogether(specification);
        EnsurePaginationHasOrdering(specification);
    }

    private static void EnsureTopNotCombinedWithPaging(IQuerySpecification<T> specification)
    {
        if (specification.Top.HasValue && (specification.Page.HasValue || specification.PageSize.HasValue))
            throw new InvalidOperationException(
                "Cannot combine Top with Page/PageSize pagination on the same specification. Use one or the other.");
    }

    private static void EnsurePageAndPageSizeSetTogether(IQuerySpecification<T> specification)
    {
        bool pageSetWithoutPageSize = specification.Page.HasValue && specification.Page.Value > 0 &&
                                       (!specification.PageSize.HasValue || specification.PageSize.Value <= 0);
        bool pageSizeSetWithoutPage = specification.PageSize.HasValue && specification.PageSize.Value > 0 &&
                                       (!specification.Page.HasValue || specification.Page.Value <= 0);
        if (pageSetWithoutPageSize || pageSizeSetWithoutPage)
            throw new InvalidOperationException(
                "Both Page and PageSize must be set together. Use WithPagination(page, pageSize).");
    }

    private static void EnsurePaginationHasOrdering(IQuerySpecification<T> specification)
    {
        bool hasPagination = specification.Page.HasValue || specification.PageSize.HasValue;
        bool hasOrdering = specification.OrderBy != null || specification.ValiSort != null;
        if (hasPagination && !hasOrdering)
            throw new InvalidOperationException(
                "Pagination requires an ordering to produce deterministic results. " +
                "Set OrderBy or ValiSort on the specification before enabling pagination.");
    }

    public IQueryable<T> ApplyOrdering(IQueryable<T> query, IQuerySpecification<T> specification)
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

    private IQueryable<T> ApplyWhere(IQueryable<T> query, ValiFlowQuery<T> filter, bool negateCondition = false)
    {
        Validation.ValidateFilterNotNull(filter);

        Expression<Func<T, bool>> condition = negateCondition ? filter.BuildNegated() : filter.BuildWithGlobal();
        return query.Where(condition);
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

    private IQueryable<T> ApplyAsNoTracking(IQueryable<T> query, bool asNoTracking = true) => asNoTracking ? query.AsNoTracking() : query;

    private IQueryable<T> ApplyAsNoTrackingWithIdentityResolution(IQueryable<T> query, bool apply)
        => apply ? query.AsNoTrackingWithIdentityResolution() : query;

    private IQueryable<T> ApplyTagWith(IQueryable<T> query, string? tag)
        => tag != null ? query.TagWith(tag) : query;

    private IQueryable<T> ApplyIgnoreAutoIncludes(IQueryable<T> query, bool apply)
        => apply ? query.IgnoreAutoIncludes() : query;

    private IQueryable<T> ApplyIgnoreQueryFilters(IQueryable<T> query, bool ignoreQueryFilters = false) =>
        ignoreQueryFilters ? query.IgnoreQueryFilters() : query;

    private IQueryable<T> ApplyAsSplitQuery(IQueryable<T> query, bool asSplitQuery = false) => asSplitQuery ? query.AsSplitQuery() : query;

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
}
