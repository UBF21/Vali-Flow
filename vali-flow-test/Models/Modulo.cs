namespace vali_flow_test.Models;

public class Modulo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public Guid CustomerId { get; set; }
    public Guid UbicacionId { get; set; }
    public Guid ClasificacionId { get; set; }
    public DateTime? Deleted { get; set; }
}