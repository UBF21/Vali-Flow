using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Interfaces.Specification;

/// <summary>
/// Defines a specification that can filter and configure a query for entities of type T.
/// </summary>
/// <typeparam name="T">The type of entity to which the specification applies.</typeparam>
public interface ISpecification<T> where T : class
{
    /// <summary>
    /// Gets the validation flow used to filter the entities.
    /// </summary>
    ValiFlowQuery<T> Filter { get; }
    
    /// <summary>
    /// Gets a collection of inclusion expressions for related properties, if any.
    /// </summary>
    IEnumerable<IEfInclude<T>>? Includes { get; }
    
    /// <summary>
    /// Gets a value indicating whether the query should be executed without change tracking (no tracking).
    /// </summary>
    bool AsNoTracking { get; }
    
    /// <summary>
    /// Gets a value indicating whether the query should be executed as a split query.
    /// </summary>
    bool AsSplitQuery { get; }  
    
    /// <summary>
    /// Gets a value indicating whether global query filters configured on the entity type should be ignored for this specification.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="true"/>, this property instructs Entity Framework Core to bypass any global query filters
    /// defined on the entity type (e.g., soft delete filters or tenant-specific filters) when executing the query.
    /// This is useful for scenarios where you need to retrieve entities that would otherwise be excluded by global filters,
    /// such as retrieving soft-deleted records or accessing data across all tenants in a multi-tenant application.
    /// Use this property with caution, as ignoring global filters may expose sensitive data or break application logic
    /// that relies on these filters for data isolation.
    /// </remarks>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// Gets a value indicating whether auto-includes configured via <c>AutoInclude()</c> in <c>OnModelCreating</c> should be ignored.
    /// </summary>
    bool IgnoreAutoIncludes { get; }

    /// <summary>
    /// Gets a value indicating whether the query should use <c>AsNoTrackingWithIdentityResolution</c>.
    /// Like <c>AsNoTracking</c> but resolves identical entities to the same instance across related data.
    /// Mutually exclusive with <see cref="AsNoTracking"/>.
    /// </summary>
    bool AsNoTrackingWithIdentityResolution { get; }

    /// <summary>
    /// Gets an optional tag string appended to the generated SQL as a comment (EF Core <c>TagWith</c>).
    /// Useful for APM tracing and query diagnostics.
    /// </summary>
    string? TagWith { get; }
}