using System.Linq.Expressions;
using Vali_Flow.Classes.Options;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Specification;

/// <summary>
/// Implements a specification that allows defining filters, inclusions, and configurations for entity queries.
/// </summary>
/// <typeparam name="T">The type of entity to which the specification applies, which must be a class.</typeparam>
public class Specification<T> : ISpecification<T> where T : class
{
    private ValiFlow<T> _filter;
    private readonly List<IIncludeExpression<T>> _includes = new();
    private IEfOrderBy<T>? _orderBy;
    private readonly List<IEfOrderThenBy<T>> _thenBys = new();
    private bool _asNoTracking = true;
    private bool _asSplitQuery;
    private int? _page;
    private int? _pageSize;
    private int? _top;
    private int? _blockSize;
    
    /// <summary>
    /// Gets the validation flow used to filter the entities.
    /// </summary>
    public ValiFlow<T> Filter => _filter;

    /// <summary>
    /// Gets the collection of inclusion expressions for related properties.
    /// </summary>
    public IEnumerable<IIncludeExpression<T>> Includes => _includes;

    /// <summary>
    /// Gets the primary ordering expression, if any.
    /// </summary>
    public IEfOrderBy<T>? OrderBy => _orderBy;

    /// <summary>
    /// Gets a collection of secondary ordering expressions (ThenBy), if any.
    /// </summary>
    public IEnumerable<IEfOrderThenBy<T>>? ThenBys => _thenBys.Count > 0 ? _thenBys : null;

    /// <summary>
    /// Gets the number of items to skip for pagination, if specified.
    /// </summary>
    public int? Page => _page;    
    
    /// <summary>
    /// Gets the number of items to take for pagination, if specified.
    /// </summary>
    public int? PageSize => _pageSize;
    
    /// <summary>
    /// Gets the maximum number of items to take (top), if specified.
    /// </summary>
    public int? Top => _top;
    
    /// <summary>
    /// Gets a value indicating whether the query should be executed without change tracking (no tracking).
    /// </summary>
    public bool AsNoTracking => _asNoTracking;

    /// <summary>
    /// Gets a value indicating whether the query should be executed as a split query.
    /// </summary>
    public bool AsSplitQuery => _asSplitQuery;
    
    public int? BlockSize => _blockSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{T}"/> class with a validation filter.
    /// </summary>
    /// <param name="filter">The validation flow that defines the filtering criteria. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filter"/> parameter is null.</exception>
    public Specification(ValiFlow<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{T}"/> class with a validation filter and optional configurations.
    /// </summary>
    /// <param name="filter">The validation flow that defines the filtering criteria. Cannot be null.</param>
    /// <param name="asNoTracking">Indicates whether the query should be executed without change tracking. Default is true.</param>
    /// <param name="asSplitQuery">Indicates whether the query should be executed as a split query. Default is false.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filter"/> parameter is null.</exception>
    public Specification(
        ValiFlow<T> filter,
        bool asNoTracking = true,
        bool asSplitQuery = false
    )
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        _asNoTracking = asNoTracking;
        _asSplitQuery = asSplitQuery;
    }

    /// <summary>
    /// Updates the validation filter of the specification with a new validation flow.
    /// </summary>
    /// <param name="filter">The new validation flow that defines the filtering criteria. Cannot be null.</param>
    /// <returns>The current instance of <see cref="Specification{T}"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filter"/> parameter is null.</exception>
    public Specification<T> WithFilter(ValiFlow<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        return this;
    }

    /// <summary>
    /// Adds an inclusion expression for a related property of type <typeparamref name="TProperty"/> to the specification.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related property.</typeparam>
    /// <param name="include">The expression that defines the related property to include. Cannot be null.</param>
    /// <returns>The current instance of <see cref="Specification{T}"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="include"/> parameter is null.</exception>
    public Specification<T> AddInclude<TProperty>(Expression<Func<T, TProperty>> include)
    {
        _includes.Add(new IncludeExpression<T, TProperty>(include));
        return this;
    }

    /// <summary>
    /// Adds multiple inclusion expressions for related properties of type <typeparamref name="TProperty"/> to the specification.
    /// </summary>
    /// <typeparam name="TProperty">The type of the related properties.</typeparam>
    /// <param name="includes">The collection of expressions that define the related properties to include.</param>
    /// <returns>The current instance of <see cref="Specification{T}"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="includes"/> parameter is null or empty.</exception>
    public Specification<T> AddIncludes<TProperty>(IEnumerable<Expression<Func<T, TProperty>>> includes)
    {
        foreach (var include in includes)
        {
            _includes.Add(new IncludeExpression<T, TProperty>(include));
        }

        return this;
    }

    /// <summary>
    /// Configures whether the query should be executed without change tracking (no tracking).
    /// </summary>
    /// <param name="asNoTracking">A value indicating whether to use no tracking.</param>
    /// <returns>The current instance of <see cref="Specification{T}"/> for chaining.</returns>
    public Specification<T> WithAsNoTracking(bool asNoTracking)
    {
        _asNoTracking = asNoTracking;
        return this;
    }

    /// <summary>
    /// Configures whether the query should be executed as a split query.
    /// </summary>
    /// <param name="asSplitQuery">A value indicating whether to use a split query.</param>
    /// <returns>The current instance of <see cref="Specification{T}"/> for chaining.</returns>
    public Specification<T> WithAsSplitQuery(bool asSplitQuery)
    {
        _asSplitQuery = asSplitQuery;
        return this;
    }

    /// <summary>
    /// Sets the primary ordering expression for the specification.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property used for ordering, which must be non-nullable.</typeparam>
    /// <param name="expression">The expression that defines the property to order by. Cannot be null.</param>
    /// <param name="ascending">A value indicating whether to order in ascending order. Default is true.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="expression"/> parameter is null.</exception>
    public Specification<T> WithOrderBy<TProperty>(Expression<Func<T, TProperty>> expression, bool ascending = true)
        where TProperty : notnull
    {
        _orderBy = new EfOrderBy<T, TProperty>(expression, ascending);
        return this;
    }

    /// <summary>
    /// Adds a single secondary ordering expression (ThenBy) to the specification.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property used for ordering, which must be non-nullable.</typeparam>
    /// <param name="expression">The expression that defines the property to order by. Cannot be null.</param>
    /// <param name="ascending">A value indicating whether to order in ascending order. Default is true.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="expression"/> parameter is null.</exception>
    public Specification<T> AddThenBy<TProperty>(Expression<Func<T, TProperty>> expression, bool ascending = true)
        where TProperty : notnull
    {
        _thenBys.Add(new EfOrderThenBy<T, TProperty>(expression, ascending));
        return this;
    }

    /// <summary>
    /// Adds multiple secondary ordering expressions (ThenBy) to the specification.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property used for ordering, which must be non-nullable.</typeparam>
    /// <param name="expressions">The collection of expressions that define the properties to order by. Cannot be null.</param>
    /// <param name="ascending">A value indicating whether to order in ascending order. Default is true.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="expressions"/> parameter is null.</exception>
    public Specification<T> AddThenBys<TProperty>(IEnumerable<Expression<Func<T, TProperty>>> expressions,
        bool ascending = true)
        where TProperty : notnull
    {
        foreach (var expression in expressions)
        {
            _thenBys.Add(new EfOrderThenBy<T, TProperty>(expression, ascending));
        }

        return this;
    }
    
    /// <summary>
    /// Configures pagination for the specification using a page number and page size.
    /// </summary>
    /// <param name="page">The page number to retrieve (must be greater than or equal to 1).</param>
    /// <param name="pageSize">The number of items per page (must be greater than or equal to 1).</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="page"/> is less than 1 or <paramref name="pageSize"/> is less than 1.</exception>
    public Specification<T> WithPagination(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentException("Page must be greater than or equal to 1.", nameof(page));
        if (pageSize < 1) throw new ArgumentException("PageSize must be greater than or equal to 1.", nameof(pageSize));

        _page = page;
        _pageSize = pageSize;
        return this;
    }
    
    /// <summary>
    /// Sets the page number for pagination.
    /// </summary>
    /// <param name="page">The page number to retrieve (must be greater than or equal to 1).</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="page"/> is less than 1.</exception>
    public Specification<T> WithPage(int page)
    {
        if (page < 1) throw new ArgumentException("Page must be greater than or equal to 1.", nameof(page));
        _page = page;
        return this;
    }
    
    /// <summary>
    /// Sets the number of items per page for pagination.
    /// </summary>
    /// <param name="pageSize">The number of items per page (must be greater than or equal to 1).</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="pageSize"/> is less than 1.</exception>
    public Specification<T> WithPageSize(int pageSize)
    {
        if (pageSize < 1) throw new ArgumentException("PageSize must be greater than or equal to 1.", nameof(pageSize));
        _pageSize = pageSize;
        return this;
    }
    
    /// <summary>
    /// Sets the maximum number of items to take (top) for the specification.
    /// </summary>
    /// <param name="top">The maximum number of items to take (must be greater than or equal to 1).</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="top"/> is less than 1.</exception>
    public Specification<T> WithTop(int top)
    {
        // if (_take.HasValue) throw new InvalidOperationException("Cannot set Top when Take is already set.");
        if (top < 1) throw new ArgumentException("Top must be greater than or equal to 1.", nameof(top));
        _top = top;
        return this;
    }
    
    /// <summary>
    /// Sets the block size for paginated block queries.
    /// </summary>
    /// <param name="blockSize">The block size (must be greater than or equal to 1).</param>
    /// <returns>The current specification instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="blockSize"/> is less than 1.</exception>
    public Specification<T> WithBlockSize(int blockSize)
    {
        if (blockSize < 1) throw new ArgumentException("BlockSize must be greater than or equal to 1.", nameof(blockSize));
        _blockSize = blockSize;
        return this;
    }
}