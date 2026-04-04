using System.Linq.Expressions;
using Vali_Flow.Classes.Options;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Specification;

/// <summary>
/// Base class for all specifications. Provides filtering, inclusions, and EF Core query options
/// with type-safe fluent chaining via the CRTP (Curiously Recurring Template Pattern).
/// </summary>
/// <typeparam name="TSpec">The concrete specification type (self-referential CRTP parameter).</typeparam>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
/// <remarks>
/// Extend this class directly when building a custom specification that needs fluent chaining.
/// <code>
/// public class MySpec : BasicSpecification&lt;MySpec, User&gt;
/// {
///     public MySpec() { }
/// }
///
/// new MySpec()
///     .WithAsNoTracking(false)   // returns MySpec
///     .AddInclude(u =&gt; u.Roles)  // returns MySpec
///     .WithFilter(query);         // returns MySpec
/// </code>
/// For simple cases that don't require subclassing, use <see cref="BasicSpecification{T}"/> directly.
/// </remarks>
public class BasicSpecification<TSpec, T> : IBasicSpecification<T>
    where TSpec : BasicSpecification<TSpec, T>
    where T : class
{
    private ValiFlowQuery<T> _filter;
    private readonly List<IEfInclude<T>> _includes = new();
    private bool _asNoTracking = true;
    private bool _asSplitQuery;
    private bool _ignoreQueryFilters;
    private bool _ignoreAutoIncludes;
    private bool _asNoTrackingWithIdentityResolution;
    private string? _tagWith;

    /// <summary>Gets the query filter used to filter entities.</summary>
    public ValiFlowQuery<T> Filter => _filter;

    /// <summary>Gets the collection of inclusion expressions for eager loading.</summary>
    public IEnumerable<IEfInclude<T>> Includes => _includes;

    /// <summary>Gets a value indicating whether change tracking is disabled.</summary>
    public bool AsNoTracking => _asNoTracking;

    /// <summary>Gets a value indicating whether the query executes as a split query.</summary>
    public bool AsSplitQuery => _asSplitQuery;

    /// <summary>Gets a value indicating whether global query filters are bypassed.</summary>
    public bool IgnoreQueryFilters => _ignoreQueryFilters;

    /// <summary>Gets a value indicating whether auto-includes are bypassed.</summary>
    public bool IgnoreAutoIncludes => _ignoreAutoIncludes;

    /// <summary>Gets a value indicating whether AsNoTrackingWithIdentityResolution is applied.</summary>
    public bool AsNoTrackingWithIdentityResolution => _asNoTrackingWithIdentityResolution;

    /// <summary>Gets the optional SQL comment tag appended to the generated query.</summary>
    public string? TagWith => _tagWith;

    /// <summary>Initializes a new instance with an empty filter (matches all entities).</summary>
    protected BasicSpecification()
    {
        _filter = new ValiFlowQuery<T>();
    }

    /// <summary>
    /// Initializes a new instance with the specified filter and EF Core query options.
    /// </summary>
    /// <param name="filter">The query filter. Cannot be null.</param>
    /// <param name="asNoTracking">Disable change tracking. Default is <see langword="true"/>.</param>
    /// <param name="asSplitQuery">Use split queries for includes. Default is <see langword="false"/>.</param>
    /// <param name="ignoreQueryFilters">Bypass global EF Core query filters. Default is <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    protected BasicSpecification(
        ValiFlowQuery<T> filter,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        bool ignoreQueryFilters = false)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        _asNoTracking = asNoTracking;
        _asSplitQuery = asSplitQuery;
        _ignoreQueryFilters = ignoreQueryFilters;
    }

    /// <summary>Replaces the current filter with a new one.</summary>
    /// <param name="filter">The new filter. Cannot be null.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public TSpec WithFilter(ValiFlowQuery<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        return (TSpec)this;
    }

    /// <summary>
    /// Adds an eager-loading expression for a related navigation property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the navigation property.</typeparam>
    /// <param name="expression">The navigation expression. Cannot be null.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    /// <example>
    /// <code>
    /// var spec = new BasicSpecification&lt;User&gt;(query)
    ///     .AddInclude(u =&gt; u.Roles)
    ///     .AddInclude(u =&gt; u.Address);
    /// </code>
    /// </example>
    public TSpec AddInclude<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        _includes.Add(new EfInclude<T, TProperty>(expression ?? throw new ArgumentNullException(nameof(expression))));
        return (TSpec)this;
    }

    /// <summary>Configures whether change tracking is disabled for this query.</summary>
    /// <param name="asNoTracking">A value indicating whether to disable tracking.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec WithAsNoTracking(bool asNoTracking)
    {
        _asNoTracking = asNoTracking;
        if (asNoTracking) _asNoTrackingWithIdentityResolution = false;
        return (TSpec)this;
    }

    /// <summary>Configures whether the query executes as a split query.</summary>
    /// <param name="asSplitQuery">A value indicating whether to use split queries.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec WithAsSplitQuery(bool asSplitQuery)
    {
        _asSplitQuery = asSplitQuery;
        return (TSpec)this;
    }

    /// <summary>
    /// Configures whether global EF Core query filters (e.g. soft-delete, tenant) are bypassed.
    /// </summary>
    /// <param name="ignoreQueryFilters">
    /// <see langword="true"/> to bypass global filters; <see langword="false"/> to apply them (default).
    /// </param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <remarks>
    /// Use with caution — ignoring global filters may expose data that is intentionally excluded
    /// (e.g. soft-deleted records or records belonging to other tenants).
    /// </remarks>
    public TSpec WithIgnoreQueryFilters(bool ignoreQueryFilters)
    {
        _ignoreQueryFilters = ignoreQueryFilters;
        return (TSpec)this;
    }

    /// <summary>
    /// Configures whether auto-includes defined in <c>OnModelCreating</c> via <c>AutoInclude()</c> are bypassed.
    /// </summary>
    /// <param name="ignoreAutoIncludes"><see langword="true"/> to skip auto-includes.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec WithIgnoreAutoIncludes(bool ignoreAutoIncludes)
    {
        _ignoreAutoIncludes = ignoreAutoIncludes;
        return (TSpec)this;
    }

    /// <summary>
    /// Configures the query to use <c>AsNoTrackingWithIdentityResolution</c>.
    /// Mutually exclusive with <see cref="WithAsNoTracking"/>: enabling this disables plain AsNoTracking.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to enable identity resolution without tracking.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec WithAsNoTrackingWithIdentityResolution(bool enabled)
    {
        _asNoTrackingWithIdentityResolution = enabled;
        if (enabled) _asNoTracking = false;
        return (TSpec)this;
    }

    /// <summary>
    /// Sets an EF Core <c>TagWith</c> comment on the query for APM tracing and diagnostics.
    /// </summary>
    /// <param name="tag">The comment string to embed in the SQL query.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec WithTagWith(string? tag)
    {
        _tagWith = tag;
        return (TSpec)this;
    }

    /// <summary>
    /// Adds a ThenInclude for a collection navigation (e.g. <c>u =&gt; u.Orders</c> then <c>o =&gt; o.Items</c>).
    /// </summary>
    /// <typeparam name="TProperty">The type of the first-level navigation (a collection element).</typeparam>
    /// <typeparam name="TNav">The type of the nested navigation property.</typeparam>
    /// <param name="include">The first-level collection include expression.</param>
    /// <param name="thenInclude">The nested navigation expression.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec AddThenInclude<TProperty, TNav>(
        Expression<Func<T, IEnumerable<TProperty>>> include,
        Expression<Func<TProperty, TNav>> thenInclude)
    {
        _includes.Add(new EfThenInclude<T, TProperty, TNav>(
            include ?? throw new ArgumentNullException(nameof(include)),
            thenInclude ?? throw new ArgumentNullException(nameof(thenInclude))));
        return (TSpec)this;
    }

    /// <summary>
    /// Adds a ThenInclude for a reference navigation (e.g. <c>u =&gt; u.Profile</c> then <c>p =&gt; p.Avatar</c>).
    /// </summary>
    /// <typeparam name="TProperty">The type of the first-level reference navigation.</typeparam>
    /// <typeparam name="TNav">The type of the nested navigation property.</typeparam>
    /// <param name="include">The first-level reference include expression.</param>
    /// <param name="thenInclude">The nested navigation expression.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    public TSpec AddThenIncludeReference<TProperty, TNav>(
        Expression<Func<T, TProperty?>> include,
        Expression<Func<TProperty?, TNav>> thenInclude)
        where TProperty : class
    {
        _includes.Add(new EfThenIncludeReference<T, TProperty, TNav>(
            include ?? throw new ArgumentNullException(nameof(include)),
            thenInclude ?? throw new ArgumentNullException(nameof(thenInclude))));
        return (TSpec)this;
    }
}

/// <summary>
/// Sealed concrete specification for simple filtering, inclusions, and EF Core query options.
/// For specs that also need ordering and pagination, use <see cref="QuerySpecification{T}"/> instead.
/// </summary>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
public sealed class BasicSpecification<T> : BasicSpecification<BasicSpecification<T>, T>
    where T : class
{
    /// <summary>Initializes a new instance with an empty filter (matches all entities).</summary>
    public BasicSpecification() { }

    /// <summary>
    /// Initializes a new instance with the specified filter and EF Core query options.
    /// </summary>
    /// <param name="filter">The query filter. Cannot be null.</param>
    /// <param name="asNoTracking">Disable change tracking. Default is <see langword="true"/>.</param>
    /// <param name="asSplitQuery">Use split queries for includes. Default is <see langword="false"/>.</param>
    /// <param name="ignoreQueryFilters">Bypass global EF Core query filters. Default is <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public BasicSpecification(
        ValiFlowQuery<T> filter,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        bool ignoreQueryFilters = false)
        : base(filter, asNoTracking, asSplitQuery, ignoreQueryFilters) { }
}
