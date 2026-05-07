namespace MRSDrunk.Api.Models;

public sealed class InventarioCompra
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int? ProveedorId { get; set; }
    public int UsuarioId { get; set; }
    public string? NumeroFactura { get; set; }
    public DateTime FechaCompra { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Proveedor? Proveedor { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<InventarioCompraDetalle> Detalles { get; set; } = new List<InventarioCompraDetalle>();
}
