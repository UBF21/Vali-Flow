using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Interfaces.Specification;

/// <summary>
/// Represents a query specification interface that defines filtering, inclusion, configuration, and advanced 
/// query options (e.g., ordering, pagination) for querying entities of type <typeparamref name="T"/>. 
/// This interface extends <see cref="ISpecification{T}"/> and is intended for complex query scenarios 
/// that require additional query customization.
/// </summary>
/// <typeparam name="T">The type of entity to which the specification applies, constrained to be a class.</typeparam>
/// <remarks>
/// This interface is designed for scenarios where advanced query features such as ordering, pagination, 
/// or limiting the number of results are needed, in addition to filtering and eager loading of related data. 
/// Implementations should provide methods to define filters, includes, and query-specific options like 
/// page size, block size, and ordering criteria.
/// </remarks>
public interface IQuerySpecification<T> : ISpecification<T> where T : class
{
    /// <summary>
    /// Gets the primary ordering expression, if any.
    /// </summary>
    IEfOrderBy<T>? OrderBy { get; }

    /// <summary>
    /// Gets a collection of secondary ordering expressions (ThenBy), if any.
    /// </summary>
    IEnumerable<IEfOrderThenBy<T>>? ThenBys { get; }
    
    /// <summary>
    /// Gets the page number for pagination, if specified.
    /// </summary>
    int? Page { get; }

    /// <summary>
    /// Gets the number of items per page for pagination, if specified.
    /// </summary>
    int? PageSize { get; }
    
    /// <summary>
    /// Gets the maximum number of items to take (top), if specified.
    /// </summary>
    int? Top { get; }
    
}