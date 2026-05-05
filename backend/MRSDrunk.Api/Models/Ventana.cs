namespace MRSDrunk.Api.Models;

public sealed class Ventana
{
    public int Id { get; set; }
    public int ModuloId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string? Icono { get; set; }
    public int Orden { get; set; }
    public bool Estado { get; set; } = true;
    public Modulo? Modulo { get; set; }
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
