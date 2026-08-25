namespace MRSDrunk.Api.Models;

public sealed class PagoConfirmacionPayU
{
    public int Id { get; set; }
    public int? PagoPasarelaId { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public string? TransaccionPayU { get; set; }
    public string? EstadoRecibido { get; set; }
    public decimal? ValorRecibido { get; set; }
    public string? Moneda { get; set; }
    public string PayloadCompleto { get; set; } = string.Empty;
    public string? FirmaRecibida { get; set; }
    public bool FirmaValida { get; set; }
    public bool Procesado { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaRecepcion { get; set; } = DateTime.UtcNow;
    public PagoPasarela? PagoPasarela { get; set; }
}
