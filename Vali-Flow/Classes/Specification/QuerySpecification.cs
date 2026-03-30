using System.Linq.Expressions;
using Vali_Flow.Classes.Options;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Utils;

namespace Vali_Flow.Classes.Specification;

/// <summary>
/// Represents a query specification for defining reusable query criteria and options for entities of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// Extends <see cref="BasicSpecification{TSpec,T}"/> and implements <see cref="IQuerySpecification{T}"/> to support
/// filtering, ordering, pagination, and EF Core-specific options in a single composable object.
///
/// <b>Ordering rules:</b>
/// <list type="bullet">
///   <item><see cref="WithOrderBy{TProperty}"/> and <see cref="WithValiSort"/> are mutually exclusive.</item>
///   <item><see cref="AddThenBy{TProperty}"/> requires <see cref="WithOrderBy{TProperty}"/> to be called first.</item>
/// </list>
///
/// <b>Pagination rules:</b>
/// <list type="bullet">
///   <item><see cref="WithTop"/> and <see cref="WithPagination"/> (Page/PageSize) are mutually exclusive.</item>
///   <item>Pagination requires an ordering to produce deterministic results.</item>
/// </list>
/// </remarks>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
public class QuerySpecification<T> : BasicSpecification<QuerySpecification<T>, T>, IQuerySpecification<T> where T : class
{
    private IEfOrderBy<T>? _orderBy;
    private readonly List<IEfOrderThenBy<T>> _thenBys = new();
    private int? _page;
    private int? _pageSize;
    private int? _top;
    private ValiSort<T>? _valiSort;

    /// <summary>Gets the primary ordering expression, if any.</summary>
    public IEfOrderBy<T>? OrderBy => _orderBy;

    /// <summary>Gets the secondary ordering expressions, or <see langword="null"/> if none.</summary>
    public IEnumerable<IEfOrderThenBy<T>>? ThenBys => _thenBys.Count > Constants.ZeroInt ? _thenBys : null;

    /// <summary>Gets the page number for pagination, if specified.</summary>
    public int? Page => _page;

    /// <summary>Gets the number of items per page for pagination, if specified.</summary>
    public int? PageSize => _pageSize;

    /// <summary>Gets the maximum number of items to take (Top), if specified.</summary>
    public int? Top => _top;

    /// <summary>Gets the <see cref="ValiSort{T}"/> ordering instance, if specified.</summary>
    public ValiSort<T>? ValiSort => _valiSort;

    /// <summary>Initializes a new instance with an empty filter.</summary>
    public QuerySpecification() { }

    /// <summary>
    /// Initializes a new instance with the specified filter.
    /// </summary>
    /// <param name="filter">The query filter. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public QuerySpecification(ValiFlowQuery<T> filter) : base(filter) { }

    /// <summary>
    /// Initializes a new instance with the specified filter and EF Core query options.
    /// </summary>
    /// <param name="filter">The query filter. Cannot be null.</param>
    /// <param name="asNoTracking">Disable change tracking. Default is <see langword="true"/>.</param>
    /// <param name="asSplitQuery">Use split queries for includes. Default is <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public QuerySpecification(
        ValiFlowQuery<T> filter,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) : base(filter, asNoTracking, asSplitQuery) { }

    /// <summary>
    /// Sets the primary ordering expression.
    /// </summary>
    /// <typeparam name="TProperty">The property type to order by. Must be non-nullable.</typeparam>
    /// <param name="expression">The ordering expression. Cannot be null.</param>
    /// <param name="ascending">Order ascending when <see langword="true"/> (default); descending otherwise.</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithValiSort"/> has already been called.</exception>
    public QuerySpecification<T> WithOrderBy<TProperty>(Expression<Func<T, TProperty>> expression, bool ascending = true)
        where TProperty : notnull
    {
        if (_valiSort != null)
            throw new InvalidOperationException(
                $"Cannot set OrderBy when ValiSort is already configured. Use either {nameof(WithOrderBy)} or {nameof(WithValiSort)}, not both.");

        _orderBy = new EfOrderBy<T, TProperty>(expression, ascending);
        return this;
    }

    /// <summary>
    /// Adds a secondary ordering expression (ThenBy).
    /// </summary>
    /// <typeparam name="TProperty">The property type to order by. Must be non-nullable.</typeparam>
    /// <param name="expression">The ordering expression. Cannot be null.</param>
    /// <param name="ascending">Order ascending when <see langword="true"/> (default); descending otherwise.</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithOrderBy{TProperty}"/> has not been called first, or when
    /// <see cref="WithValiSort"/> has already been called.
    /// </exception>
    public QuerySpecification<T> AddThenBy<TProperty>(Expression<Func<T, TProperty>> expression, bool ascending = true)
        where TProperty : notnull
    {
        if (_valiSort != null)
            throw new InvalidOperationException(
                $"Cannot add ThenBy when ValiSort is already configured. Use either OrderBy/ThenBy or {nameof(WithValiSort)}, not both.");
        if (_orderBy == null)
            throw new InvalidOperationException(
                $"ThenBy requires a primary ordering. Call {nameof(WithOrderBy)} before {nameof(AddThenBy)}.");

        _thenBys.Add(new EfOrderThenBy<T, TProperty>(expression, ascending));
        return this;
    }

    /// <summary>
    /// Adds multiple secondary ordering expressions (ThenBy) with the same direction.
    /// </summary>
    /// <typeparam name="TProperty">The property type to order by. Must be non-nullable.</typeparam>
    /// <param name="expressions">The ordering expressions. Cannot be null.</param>
    /// <param name="ascending">Order ascending when <see langword="true"/> (default); descending otherwise.</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expressions"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithOrderBy{TProperty}"/> has not been called first, or when
    /// <see cref="WithValiSort"/> has already been called.
    /// </exception>
    public QuerySpecification<T> AddThenBys<TProperty>(IEnumerable<Expression<Func<T, TProperty>>> expressions,
        bool ascending = true)
        where TProperty : notnull
    {
        if (_valiSort != null)
            throw new InvalidOperationException(
                $"Cannot add ThenBy when ValiSort is already configured. Use either OrderBy/ThenBy or {nameof(WithValiSort)}, not both.");
        if (_orderBy == null)
            throw new InvalidOperationException(
                $"ThenBy requires a primary ordering. Call {nameof(WithOrderBy)} before {nameof(AddThenBys)}.");

        foreach (var expression in expressions)
            _thenBys.Add(new EfOrderThenBy<T, TProperty>(expression, ascending));

        return this;
    }

    /// <summary>
    /// Configures pagination using a page number and page size.
    /// </summary>
    /// <param name="page">The page number (must be ≥ 1).</param>
    /// <param name="pageSize">The number of items per page (must be ≥ 1).</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="page"/> or <paramref name="pageSize"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithTop"/> has already been called.</exception>
    public QuerySpecification<T> WithPagination(int page, int pageSize)
    {
        if (_top.HasValue)
            throw new InvalidOperationException(
                $"Cannot use Page/PageSize together with Top. Use either {nameof(WithPagination)} or {nameof(WithTop)}, not both.");
        if (page < 1) throw new ArgumentException("Page must be greater than or equal to 1.", nameof(page));
        if (pageSize < 1) throw new ArgumentException("PageSize must be greater than or equal to 1.", nameof(pageSize));

        _page = page;
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Sets the page number for pagination.
    /// </summary>
    /// <param name="page">The page number (must be ≥ 1).</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="page"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithTop"/> has already been called.</exception>
    public QuerySpecification<T> WithPage(int page)
    {
        if (_top.HasValue)
            throw new InvalidOperationException(
                $"Cannot set Page when Top is already configured. Use either pagination or {nameof(WithTop)}, not both.");
        if (page < 1) throw new ArgumentException("Page must be greater than or equal to 1.", nameof(page));

        _page = page;
        return this;
    }

    /// <summary>
    /// Sets the number of items per page for pagination.
    /// </summary>
    /// <param name="pageSize">The number of items per page (must be ≥ 1).</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageSize"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithTop"/> has already been called.</exception>
    public QuerySpecification<T> WithPageSize(int pageSize)
    {
        if (_top.HasValue)
            throw new InvalidOperationException(
                $"Cannot set PageSize when Top is already configured. Use either pagination or {nameof(WithTop)}, not both.");
        if (pageSize < 1) throw new ArgumentException("PageSize must be greater than or equal to 1.", nameof(pageSize));

        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of items to take (Top).
    /// </summary>
    /// <param name="top">The maximum number of items (must be ≥ 1).</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="top"/> is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Page or PageSize has already been configured.</exception>
    public QuerySpecification<T> WithTop(int top)
    {
        if (_page.HasValue || _pageSize.HasValue)
            throw new InvalidOperationException(
                $"Cannot use Top together with Page/PageSize. Use either {nameof(WithTop)} or {nameof(WithPagination)}, not both.");
        if (top < 1) throw new ArgumentException("Top must be greater than or equal to 1.", nameof(top));

        _top = top;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="ValiSort{T}"/> ordering for the specification.
    /// </summary>
    /// <param name="sort">The ValiSort instance. Cannot be null.</param>
    /// <returns>The current specification for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sort"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when OrderBy or ThenBy has already been configured.</exception>
    public QuerySpecification<T> WithValiSort(ValiSort<T> sort)
    {
        if (_orderBy != null || _thenBys.Count > 0)
            throw new InvalidOperationException(
                $"Cannot set ValiSort when OrderBy or ThenBy is already configured. Use either {nameof(WithValiSort)} or {nameof(WithOrderBy)}/{nameof(AddThenBy)}, not both.");

        _valiSort = sort ?? throw new ArgumentNullException(nameof(sort));
        return this;
    }
}
