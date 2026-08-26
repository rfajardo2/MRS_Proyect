namespace MRSDrunk.Api.Models;

public sealed class Comanda
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int CuentaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int MeseroId { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Cuenta? Cuenta { get; set; }
    public Usuario? Mesero { get; set; }
    public ICollection<ComandaDetalle> Detalles { get; set; } = new List<ComandaDetalle>();
}
