namespace MRSDrunk.Api.Models;

public sealed class InventarioStock
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int ProductoId { get; set; }
    public decimal CantidadActual { get; set; }
    public decimal CantidadMinima { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Producto? Producto { get; set; }
}
