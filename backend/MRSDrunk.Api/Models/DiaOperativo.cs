namespace MRSDrunk.Api.Models;

public sealed class DiaOperativo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public bool Cerrado { get; set; }
    public int UsuarioAperturaId { get; set; }
    public int? UsuarioCierreId { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Usuario? UsuarioApertura { get; set; }
    public Usuario? UsuarioCierre { get; set; }
}
