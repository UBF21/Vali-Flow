namespace vali_flow.Classes.Results;

public class PaginatedBlockResult<T>
{
    public List<T> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int BlockSize { get; set; }
    public int TotalItemsInBlock { get; set; }
    public bool HasMoreBlocks { get; set; }
}