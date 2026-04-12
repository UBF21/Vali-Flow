namespace Vali_Flow.Models;

/// <summary>
/// Represents a single page of query results together with pagination metadata.
/// </summary>
/// <typeparam name="T">Type of entity in the page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>The entities on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total number of entities across all pages that match the query filter.</summary>
    public int TotalCount { get; }

    /// <summary>One-based index of the current page.</summary>
    public int Page { get; }

    /// <summary>Maximum number of entities per page.</summary>
    public int PageSize { get; }

    /// <summary>Total number of pages computed from <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary><c>true</c> if there is a page after the current one.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary><c>true</c> if there is a page before the current one.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Initializes a new <see cref="PagedResult{T}"/>.
    /// </summary>
    /// <param name="items">The entities on the current page. Must not be <c>null</c>.</param>
    /// <param name="totalCount">Total matching entity count across all pages. Must be non-negative.</param>
    /// <param name="page">Current page number (one-based). Must be &gt;= 1.</param>
    /// <param name="pageSize">Page size. Must be &gt;= 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="totalCount"/> is negative, or <paramref name="page"/> or <paramref name="pageSize"/> is less than 1.</exception>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount cannot be negative.");
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be >= 1.");
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
