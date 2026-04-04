namespace Vali_Flow.Sql.Tests.Models;

[Flags]
internal enum OrderPermissions
{
    None    = 0,
    View    = 1,
    Edit    = 2,
    Delete  = 4,
    Admin   = 8
}

internal sealed class TestOrder
{
    public int    Id          { get; set; }
    public string Reference   { get; set; } = string.Empty;
    public bool   IsShipped   { get; set; }
    public bool   IsCancelled { get; set; }
    public decimal Total      { get; set; }
    public int    Quantity    { get; set; }
    public OrderPermissions Permissions { get; set; }
}
