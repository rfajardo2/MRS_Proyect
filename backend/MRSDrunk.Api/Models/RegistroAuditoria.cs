namespace MRSDrunk.Api.Models;

public sealed class RegistroAuditoria
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int UsuarioId { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Empresa? Empresa { get; set; }
    public Usuario? Usuario { get; set; }
}
