namespace Vali_Flow.NoSql.Elasticsearch.Tests.Models;

public sealed class TestDocument
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;
    public string? Email    { get; set; }
    public int     Age      { get; set; }
    public decimal Price    { get; set; }
    public bool    IsActive { get; set; }
    public string  Category { get; set; } = string.Empty;
}
