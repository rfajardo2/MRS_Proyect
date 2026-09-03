namespace MRSDrunk.Api.Models;

public sealed class ComandaDetalle
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public int CuentaItemId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int? AreaPreparacionId { get; set; }
    public decimal Cantidad { get; set; }
    public bool RequierePreparacion { get; set; } = true;
    public string Estado { get; set; } = "PENDIENTE";
    public string? Observacion { get; set; }
    public int? UsuarioTomaId { get; set; }
    public DateTime? FechaToma { get; set; }
    public int? UsuarioListoId { get; set; }
    public DateTime? FechaListo { get; set; }
    public int? UsuarioDespachoId { get; set; }
    public DateTime? FechaDespacho { get; set; }
    public int? UsuarioEntregaId { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Comanda? Comanda { get; set; }
    public CuentaItem? CuentaItem { get; set; }
    public Producto? Producto { get; set; }
    public AreaPreparacion? AreaPreparacion { get; set; }
    public Usuario? UsuarioToma { get; set; }
    public Usuario? UsuarioListo { get; set; }
    public Usuario? UsuarioDespacho { get; set; }
    public Usuario? UsuarioEntrega { get; set; }
    public ICollection<ComandaDetalleEvento> Eventos { get; set; } = new List<ComandaDetalleEvento>();
}
