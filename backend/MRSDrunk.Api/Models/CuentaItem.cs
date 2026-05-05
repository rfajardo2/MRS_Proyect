namespace MRSDrunk.Api.Models;

public sealed class CuentaItem
{
    public int Id { get; set; }
    public int CuentaId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public bool Eliminado { get; set; }
    public string? MotivoEliminacion { get; set; }
    public int UsuarioCreacionId { get; set; }
    public int? UsuarioEliminacionId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEliminacion { get; set; }
    public Cuenta? Cuenta { get; set; }
    public Producto? Producto { get; set; }
    public Usuario? UsuarioCreacion { get; set; }
    public Usuario? UsuarioEliminacion { get; set; }
}
