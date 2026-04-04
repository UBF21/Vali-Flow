namespace Vali_Flow.Benchmarks.Models;

public sealed class BenchmarkOrder
{
    public int      Id        { get; set; }
    public decimal  Amount    { get; set; }
    public bool     IsActive  { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   Status    { get; set; } = string.Empty;
    public int      Quantity  { get; set; }
    public DateTime CreatedAt { get; set; }
}
