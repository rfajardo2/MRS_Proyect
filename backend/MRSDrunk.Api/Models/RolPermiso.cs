namespace MRSDrunk.Api.Models;

public sealed class RolPermiso
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public int PermisoId { get; set; }
    public int VentanaId { get; set; }
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeConsultar { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
    public Rol? Rol { get; set; }
    public Permiso? Permiso { get; set; }
    public Ventana? Ventana { get; set; }
}
