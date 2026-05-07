namespace MRSDrunk.Api.Models;

public sealed class ProductoReceta
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int ProductoVentaId { get; set; }
    public int InsumoProductoId { get; set; }
    public int? UnidadMedidaId { get; set; }
    public decimal Cantidad { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Producto? ProductoVenta { get; set; }
    public Producto? InsumoProducto { get; set; }
    public UnidadMedida? UnidadMedida { get; set; }
}
