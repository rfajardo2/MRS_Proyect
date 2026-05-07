namespace MRSDrunk.Api.Models;

public sealed class ConfiguracionVenta
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public bool RequiereAprobacionCierre { get; set; } = true;
    public bool PermiteDividirCuenta { get; set; } = true;
    public bool PermiteEliminarItems { get; set; } = true;
    public bool RequiereMotivoEliminarItem { get; set; } = true;
    public bool RequiereMotivoAnularCuenta { get; set; } = true;
    public decimal PorcentajeRepartoBase { get; set; } = 45;
    public decimal TarifaCuatroPorMil { get; set; } = 0.4m;
    public decimal TarifaRetefuente { get; set; } = 1.5m;
    public decimal TarifaComisionDatafono { get; set; } = 3.29m;
    public decimal TarifaRetIca { get; set; } = 0.42m;
    public decimal ComisionFijaDatafono { get; set; } = 300;
    public decimal PorcentajePropinaDefecto { get; set; } = 10;
    public TimeSpan HoraInicioDiaOperativo { get; set; } = new(6, 0, 0);
    public TimeSpan HoraCierreDiaOperativo { get; set; } = new(5, 59, 0);
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
}
