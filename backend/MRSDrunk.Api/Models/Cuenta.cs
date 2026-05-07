namespace MRSDrunk.Api.Models;

public sealed class Cuenta
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int MeseroId { get; set; }
    public int? DiaOperativoId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string? Mesa { get; set; }
    public string? Cliente { get; set; }
    public string Estado { get; set; } = "Abierta";
    public bool Dividida { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaSolicitudCierre { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int? AdministradorCierreId { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? MotivoAnulacion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Usuario? Mesero { get; set; }
    public Usuario? AdministradorCierre { get; set; }
    public DiaOperativo? DiaOperativo { get; set; }
    public ICollection<CuentaItem> Items { get; set; } = new List<CuentaItem>();
    public ICollection<CuentaPago> Pagos { get; set; } = new List<CuentaPago>();
}
