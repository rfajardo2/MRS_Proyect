namespace MRSDrunk.Api.Models;

public sealed class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
}
