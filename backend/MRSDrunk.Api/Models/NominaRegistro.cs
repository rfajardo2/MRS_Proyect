namespace MRSDrunk.Api.Models;

public sealed class NominaRegistro
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public int EmpleadoId { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Concepto { get; set; } = "Dia";
    public string? TipoNovedad { get; set; }
    public string? CodigoNovedad { get; set; }
    public string EstadoDia { get; set; } = "Trabajado";
    public decimal? Horas { get; set; }
    public decimal? Porcentaje { get; set; }
    public decimal? BaseCalculo { get; set; }
    public decimal Valor { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public NominaPeriodo? Periodo { get; set; }
    public NominaEmpleado? Empleado { get; set; }
}
