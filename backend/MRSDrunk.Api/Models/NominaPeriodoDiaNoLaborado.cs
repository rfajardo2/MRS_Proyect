namespace MRSDrunk.Api.Models;

public sealed class NominaPeriodoDiaNoLaborado
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public NominaPeriodo? Periodo { get; set; }
}
