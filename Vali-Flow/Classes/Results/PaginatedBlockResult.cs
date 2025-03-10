namespace Vali_Flow.Classes.Results;

/// <summary>
/// Represents the result of a paginated query, organized into blocks of entities.
/// This class provides metadata about the current page, block size, and the items retrieved.
/// </summary>
/// <typeparam name="T">The type of the entities in the paginated result.</typeparam>
public class PaginatedBlockResult<T>
{
    /// <summary>
    /// Gets or sets the collection of entities for the current page within the block.
    /// </summary>
    /// <remarks>
    /// The default value is an empty list. This property contains the actual items retrieved
    /// based on the current page and page size within the block.
    /// </remarks>
    public IEnumerable<T> Items { get; set; } = new List<T>();
    
    /// <summary>
    /// Gets or sets the current page number (1-based) within the paginated result.
    /// </summary>
    public int CurrentPage { get; set; }
    
    /// <summary>
    /// Gets or sets the number of entities per page within the block.
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Gets or sets the number of entities per block in the paginated result.
    /// </summary>
    /// <remarks>
    /// A block is a larger chunk of data that contains multiple pages.
    /// </remarks>
    public int BlockSize { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of entities in the current block.
    /// </summary>
    /// <remarks>
    /// This value indicates how many items are available in the current block,
    /// which may be less than or equal to the <see cref="BlockSize"/> if it is the last block.
    /// </remarks>
    public int TotalItemsInBlock { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether there are more blocks available beyond the current one.
    /// </summary>
    /// <remarks>
    /// This property is true if additional blocks of data exist, false otherwise.
    /// </remarks>
    public bool HasMoreBlocks { get; set; }
}