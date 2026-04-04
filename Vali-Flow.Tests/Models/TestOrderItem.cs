namespace Vali_Flow.Tests.Models;

public sealed class TestOrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public TestOrder Order { get; set; } = null!;
}
