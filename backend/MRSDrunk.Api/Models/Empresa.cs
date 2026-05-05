namespace MRSDrunk.Api.Models;

public sealed class Empresa
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
