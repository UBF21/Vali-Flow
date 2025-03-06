namespace Vali_Flow.Classes.Results;

public class PaginatedBlockResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int BlockSize { get; set; }
    public int TotalItemsInBlock { get; set; }
    public bool HasMoreBlocks { get; set; }
}