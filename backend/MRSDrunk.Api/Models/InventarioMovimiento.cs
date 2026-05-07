namespace MRSDrunk.Api.Models;

public sealed class InventarioMovimiento
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int ProductoId { get; set; }
    public int UsuarioId { get; set; }
    public int? CuentaId { get; set; }
    public int? CuentaItemId { get; set; }
    public int? LoteId { get; set; }
    public int? CompraId { get; set; }
    public string Tipo { get; set; } = "Entrada";
    public decimal Cantidad { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public decimal? CostoUnitario { get; set; }
    public string? Referencia { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Producto? Producto { get; set; }
    public Usuario? Usuario { get; set; }
    public Cuenta? Cuenta { get; set; }
    public CuentaItem? CuentaItem { get; set; }
    public InventarioLote? Lote { get; set; }
    public InventarioCompra? Compra { get; set; }
}
