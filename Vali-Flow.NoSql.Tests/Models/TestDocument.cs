namespace Vali_Flow.NoSql.Tests.Models;

public sealed class TestDocument
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string? Email   { get; set; }
    public int    Age      { get; set; }
    public decimal Price   { get; set; }
    public bool   IsActive { get; set; }
    public string         Category  { get; set; } = string.Empty;
    public double         Score     { get; set; }
    public float          Rating    { get; set; }
    public long           Quantity  { get; set; }
    public DateTime       CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
