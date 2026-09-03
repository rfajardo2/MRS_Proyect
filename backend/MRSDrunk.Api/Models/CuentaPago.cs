namespace MRSDrunk.Api.Models;

public sealed class CuentaPago
{
    public int Id { get; set; }
    public int CuentaId { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string Origen { get; set; } = "MANUAL";
    public string Estado { get; set; } = "APLICADO";
    public decimal Valor { get; set; }
    public bool IncluyePropina { get; set; }
    public decimal ValorPropina { get; set; }
    public string? Referencia { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public int UsuarioRegistroId { get; set; }
    public int? PagoPasarelaId { get; set; }
    public Cuenta? Cuenta { get; set; }
    public Usuario? UsuarioRegistro { get; set; }
    public PagoPasarela? PagoPasarela { get; set; }
}
