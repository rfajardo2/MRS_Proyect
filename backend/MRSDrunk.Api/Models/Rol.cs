namespace MRSDrunk.Api.Models;

public sealed class Rol
{
    public int Id { get; set; }
    public int? EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSuperUsuario { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
