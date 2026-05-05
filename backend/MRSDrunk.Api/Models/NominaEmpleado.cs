namespace MRSDrunk.Api.Models;

public sealed class NominaEmpleado
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string TipoDocumento { get; set; } = "CC";
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime? FechaExpedicionDocumento { get; set; }
    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? Departamento { get; set; }
    public string? Municipio { get; set; }
    public string Pais { get; set; } = "CO";
    public string? Cargo { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow.Date;
    public DateTime? FechaRetiro { get; set; }
    public string TipoContrato { get; set; } = "Termino indefinido";
    public string TipoTrabajador { get; set; } = "Dependiente";
    public string SubtipoCotizante { get; set; } = "No aplica";
    public string TipoSalario { get; set; } = "Ordinario";
    public bool SalarioIntegral { get; set; }
    public decimal SalarioBase { get; set; }
    public decimal ValorDiaBase { get; set; }
    public string PeriodicidadPago { get; set; } = "Mensual";
    public string JornadaLaboral { get; set; } = "Tiempo completo";
    public string? Eps { get; set; }
    public string? FondoPension { get; set; }
    public string? FondoCesantias { get; set; }
    public string? Arl { get; set; }
    public string NivelRiesgoArl { get; set; } = "I";
    public string? CajaCompensacion { get; set; }
    public string? Banco { get; set; }
    public string? TipoCuenta { get; set; }
    public string? NumeroCuenta { get; set; }
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }
    public string? Observaciones { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
}
