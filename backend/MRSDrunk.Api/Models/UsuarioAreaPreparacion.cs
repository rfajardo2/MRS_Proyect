namespace MRSDrunk.Api.Models;

public sealed class UsuarioAreaPreparacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int AreaPreparacionId { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Usuario? Usuario { get; set; }
    public AreaPreparacion? AreaPreparacion { get; set; }
}
