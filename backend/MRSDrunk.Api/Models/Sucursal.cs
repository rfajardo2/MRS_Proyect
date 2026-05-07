namespace MRSDrunk.Api.Models;

public sealed class Sucursal
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Direccion { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string Pais { get; set; } = "CO";
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public bool EsPrincipal { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
}
