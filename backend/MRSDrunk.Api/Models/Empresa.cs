namespace MRSDrunk.Api.Models;

public sealed class Empresa
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string? DigitoVerificacion { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string TipoDocumento { get; set; } = "NIT";
    public string RegimenTributario { get; set; } = "Responsable de IVA";
    public string ResponsabilidadFiscal { get; set; } = "R-99-PN";
    public string? MatriculaMercantil { get; set; }
    public string? ActividadEconomicaCiiu { get; set; }
    public string? DireccionFiscal { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string Pais { get; set; } = "CO";
    public string? Telefono { get; set; }
    public string? CorreoFacturacion { get; set; }
    public string? RepresentanteLegal { get; set; }
    public string? DocumentoRepresentante { get; set; }
    public bool EsPrincipal { get; set; }
    public string? LogoUrl { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
