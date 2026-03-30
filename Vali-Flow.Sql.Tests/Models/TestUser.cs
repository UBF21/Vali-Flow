namespace Vali_Flow.Sql.Tests.Models;

internal sealed class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public int Status { get; set; }
}
