namespace MRSDrunk.Api.Models;

public sealed class CajaTurno
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int UsuarioAperturaId { get; set; }
    public int? UsuarioCierreId { get; set; }
    public DateTime FechaOperativa { get; set; }
    public string Estado { get; set; } = "Abierta";
    public decimal SaldoInicial { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TotalPagos { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal? EfectivoReal { get; set; }
    public decimal? Diferencia { get; set; }
    public string? ObservacionApertura { get; set; }
    public string? ObservacionCierre { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Usuario? UsuarioApertura { get; set; }
    public Usuario? UsuarioCierre { get; set; }
}
