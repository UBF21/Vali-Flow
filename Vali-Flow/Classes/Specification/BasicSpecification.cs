using System.Linq.Expressions;
using Vali_Flow.Classes.Options;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Specification;

/// <summary>
/// Implements a basic specification that allows defining filters, inclusions, and configurations for simple entity queries.
/// </summary>
/// <typeparam name="T">The type of entity to which the specification applies, which must be a class.</typeparam>
public class BasicSpecification<T>: IBasicSpecification<T> where T : class
{
    private ValiFlow<T> _filter;
    private readonly List<IEfInclude<T>> _includes = new();
    private bool _asNoTracking = true;
    private bool _asSplitQuery;
    private bool _ignoreQueryFilters;
    
    /// <summary>
    /// Gets the validation flow used to filter the entities.
    /// </summary>
    public ValiFlow<T> Filter => _filter;

    /// <summary>
    /// Gets the collection of inclusion expressions for related properties.
    /// </summary>
    public IEnumerable<IEfInclude<T>> Includes => _includes;

    /// <summary>
    /// Gets a value indicating whether the query should be executed without change tracking (no tracking).
    /// </summary>
    public bool AsNoTracking => _asNoTracking;

    /// <summary>
    /// Gets a value indicating whether the query should be executed as a split query.
    /// </summary>
    public bool AsSplitQuery => _asSplitQuery;
    
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
    public bool IgnoreQueryFilters => _ignoreQueryFilters;
    
    public BasicSpecification()
    {
        _filter = new ValiFlow<T>();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicSpecification{T}"/> class with a validation filter.
    /// </summary>
    /// <param name="filter">The validation flow that defines the filtering criteria. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filter"/> parameter is null.</exception>
    public BasicSpecification(ValiFlow<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicSpecification{T}"/> class with a validation filter and optional configurations.
    /// </summary>
    /// <param name="filter">The validation flow that defines the filtering criteria. Cannot be null.</param>
    /// <param name="asNoTracking">Indicates whether the query should be executed without change tracking. Default is true.</param>
    /// <param name="asSplitQuery">Indicates whether the query should be executed as a split query. Default is false.</param>
    /// <param name="ignoreQueryFilters">Indicates whether global query filters should be ignored. Default is false.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filter"/> parameter is null.</exception>
    public BasicSpecification(
        ValiFlow<T> filter,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        bool ignoreQueryFilters = false
    )
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        _asNoTracking = asNoTracking;
        _asSplitQuery = asSplitQuery;
        _ignoreQueryFilters = ignoreQueryFilters;
    }
    
    /// <summary>
    /// Updates the validation filter of the specification with a new validation flow.
    /// </summary>
    /// <param name="filter">The new validation flow that defines the filtering criteria. Cannot be null.</param>
    /// <returns>The current instance of <see cref="BasicSpecification{T}"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filter"/> parameter is null.</exception>
    public BasicSpecification<T> WithFilter(ValiFlow<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        return this;
    }

    /// <summary>
    /// Adds an inclusion expression for a related property of the specified type to the specification.
    /// This method allows including navigation properties (e.g., collections or single entities) in the query 
    /// using a strongly-typed lambda expression, avoiding boxing/unboxing overhead.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related property to include.</typeparam>
    /// <param name="expression">The lambda expression that defines the related property to include. 
    /// Cannot be null.</param>
    /// <returns>The current instance of <see cref="BasicSpecification{T}"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expression"/> parameter is null.</exception>
    /// <example>
    /// <code>
    /// var spec = new BasicSpecification&lt;User&gt;(u => u.Name != null)
    ///     .AddInclude(u => u.Roles)
    ///     .AddInclude(u => u.Address);
    /// </code>
    /// </example>
    public BasicSpecification<T> AddInclude<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        _includes.Add(new EfInclude<T, TProperty>(expression ?? throw new ArgumentNullException(nameof(expression))));
        return this;
    }
    
    
    /// <summary>
    /// Configures whether the query should be executed without change tracking (no tracking).
    /// </summary>
    /// <param name="asNoTracking">A value indicating whether to use no tracking.</param>
    /// <returns>The current instance of <see cref="BasicSpecification{T}"/> for chaining.</returns>
    public BasicSpecification<T> WithAsNoTracking(bool asNoTracking)
    {
        _asNoTracking = asNoTracking;
        return this;
    }
    
    /// <summary>
    /// Configures whether the query should be executed as a split query.
    /// </summary>
    /// <param name="asSplitQuery">A value indicating whether to use a split query.</param>
    /// <returns>The current instance of <see cref="BasicSpecification{T}"/> for chaining.</returns>
    public BasicSpecification<T> WithAsSplitQuery(bool asSplitQuery)
    {
        _asSplitQuery = asSplitQuery;
        return this;
    }

    /// <summary>
    /// Configures whether global query filters configured on the entity type should be ignored for this specification.
    /// </summary>
    /// <param name="ignoreQueryFilters">A value indicating whether to ignore global query filters. 
    /// When set to <see langword="true"/>, Entity Framework Core bypasses any global query filters defined on the entity type.</param>
    /// <returns>The current instance of <see cref="BasicSpecification{T}"/> for method chaining.</returns>
    /// <remarks>
    /// This method allows you to specify whether global query filters (e.g., soft delete filters or tenant-specific filters) 
    /// should be ignored when executing the query. This is useful for scenarios where you need to retrieve entities that would 
    /// otherwise be excluded by global filters, such as retrieving soft-deleted records or accessing data across all tenants 
    /// in a multi-tenant application. Use this setting with caution, as ignoring global filters may expose sensitive data 
    /// or break application logic that relies on these filters for data isolation.
    /// </remarks>
    public BasicSpecification<T> WithIgnoreQueryFilters(bool ignoreQueryFilters)
    {
        _ignoreQueryFilters = ignoreQueryFilters;
        return this;
    }
}