namespace MRSDrunk.Api.Models;

public sealed class Usuario
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string UsuarioNombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public int? UsuarioCreacion { get; set; }
    public int? UsuarioModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Rol? Rol { get; set; }
}
