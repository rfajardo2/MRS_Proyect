namespace MRSDrunk.Api.Models;

public sealed class Modulo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
    public int Orden { get; set; }
    public bool Estado { get; set; } = true;
    public ICollection<Ventana> Ventanas { get; set; } = new List<Ventana>();
}
