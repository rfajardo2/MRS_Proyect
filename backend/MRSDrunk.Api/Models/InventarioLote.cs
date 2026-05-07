namespace MRSDrunk.Api.Models;

public sealed class InventarioLote
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int ProductoId { get; set; }
    public int? ProveedorId { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public decimal CantidadActual { get; set; }
    public decimal CostoUnitario { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Producto? Producto { get; set; }
    public Proveedor? Proveedor { get; set; }
}
