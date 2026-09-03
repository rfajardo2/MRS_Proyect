namespace MRSDrunk.Api.Models;

public sealed class PagoPasarela
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int CuentaId { get; set; }
    public string? MesaReferencia { get; set; }
    public string Proveedor { get; set; } = "PAYU";
    public string MetodoPago { get; set; } = string.Empty;
    public string ReferenciaUnica { get; set; } = string.Empty;
    public decimal ValorEsperado { get; set; }
    public decimal ValorPagado { get; set; }
    public string Moneda { get; set; } = "COP";
    public string Estado { get; set; } = "PENDING";
    public string? TransaccionPayU { get; set; }
    public string? OrderIdPayU { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaExpiracion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaUltimaConsulta { get; set; }
    public int UsuarioCreacionId { get; set; }
    public int? CuentaPagoId { get; set; }
    public string? Observacion { get; set; }
    public string? MensajeError { get; set; }
    public Empresa? Empresa { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Cuenta? Cuenta { get; set; }
    public Usuario? UsuarioCreacion { get; set; }
    public CuentaPago? CuentaPago { get; set; }
    public ICollection<PagoConfirmacionPayU> Confirmaciones { get; set; } = new List<PagoConfirmacionPayU>();
}
