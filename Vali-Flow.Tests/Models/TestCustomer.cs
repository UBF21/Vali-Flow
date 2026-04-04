namespace Vali_Flow.Tests.Models;

public sealed class TestCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<TestOrder> Orders { get; set; } = new List<TestOrder>();
}
