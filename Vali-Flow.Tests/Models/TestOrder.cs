namespace Vali_Flow.Tests.Models;

public sealed class TestOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsShipped { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CustomerId { get; set; }
    public TestCustomer? Customer { get; set; }
    public ICollection<TestOrderItem> Items { get; set; } = new List<TestOrderItem>();
}
