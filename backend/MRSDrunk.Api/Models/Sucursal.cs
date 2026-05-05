namespace MRSDrunk.Api.Models;

public sealed class Sucursal
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Empresa? Empresa { get; set; }
}
