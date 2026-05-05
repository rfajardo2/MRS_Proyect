namespace MRSDrunk.Api.Models;

public sealed class UsuarioSesion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int EmpresaId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime UltimaActividad { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaCierre { get; set; }
    public bool Estado { get; set; } = true;
    public string? CerradaPor { get; set; }
    public Usuario? Usuario { get; set; }
    public Empresa? Empresa { get; set; }
}
