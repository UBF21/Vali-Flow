using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Interfaces.Specification;

/// <summary>
/// Defines a specification that can filter and configure a query for entities of type T.
/// </summary>
/// <typeparam name="T">The type of entity to which the specification applies.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the validation flow used to filter the entities.
    /// </summary>
    ValiFlow<T> Filter { get; }
    
    /// <summary>
    /// Gets a collection of inclusion expressions for related properties, if any.
    /// </summary>
    IEnumerable<IIncludeExpression<T>>? Includes { get; }
    
    /// <summary>
    /// Gets the primary ordering expression, if any.
    /// </summary>
    IEfOrderBy<T>? OrderBy { get; }

    /// <summary>
    /// Gets a collection of secondary ordering expressions (ThenBy), if any.
    /// </summary>
    IEnumerable<IEfOrderThenBy<T>>? ThenBys { get; }
    
    /// <summary>
    /// Gets a value indicating whether the query should be executed without change tracking (no tracking).
    /// </summary>
    bool AsNoTracking { get; }
    
    /// <summary>
    /// Gets a value indicating whether the query should be executed as a split query.
    /// </summary>
    bool AsSplitQuery { get; }  
    
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
    
    /// <summary>
    /// Gets the block size for paginated block queries, if specified.
    /// </summary>
    int? BlockSize { get; }
}