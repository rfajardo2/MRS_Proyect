namespace MRSDrunk.Api.Models;

public sealed class Reserva
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int MesaId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int NumeroPersonas { get; set; } = 2;
    public DateTime FechaHora { get; set; }
    public int DuracionMinutos { get; set; } = 90;
    public string Estado { get; set; } = "Pendiente";
    public string? Observacion { get; set; }
    public int UsuarioCreacionId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Mesa? Mesa { get; set; }
    public Usuario? UsuarioCreacion { get; set; }
}
