namespace MRSDrunk.Api.Models;

public sealed class RefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Usuario? Usuario { get; set; }
}
