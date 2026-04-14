namespace Vali_Flow.InMemory.Models;

/// <summary>
/// Represents a paginated result set with metadata about the current page, total count, and navigation flags.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the total number of items across all pages (matching the filter).</summary>
    public int TotalCount { get; }

    /// <summary>Gets the current page number (1-based).</summary>
    public int Page { get; }

    /// <summary>Gets the maximum number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages { get; }

    /// <summary>Gets a value indicating whether a next page exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Gets a value indicating whether a previous page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Initializes a new <see cref="PagedResult{T}"/>.</summary>
    public PagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(items));
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
    }
}
