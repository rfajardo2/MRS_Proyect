namespace MRSDrunk.Api.Models;

public sealed class NominaPeriodo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Cerrado { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public ICollection<NominaRegistro> Registros { get; set; } = new List<NominaRegistro>();
    public ICollection<NominaPeriodoDiaNoLaborado> DiasNoLaborados { get; set; } = new List<NominaPeriodoDiaNoLaborado>();
}
