namespace MRSDrunk.Api.Models;

public sealed class InventarioCompraDetalle
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProductoId { get; set; }
    public int? LoteId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? CodigoLote { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal Total { get; set; }
    public InventarioCompra? Compra { get; set; }
    public Producto? Producto { get; set; }
    public InventarioLote? Lote { get; set; }
}
