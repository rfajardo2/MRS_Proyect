namespace MRSDrunk.Api.Models;

public sealed class ComandaDetalleEvento
{
    public int Id { get; set; }
    public int ComandaDetalleId { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Observacion { get; set; }
    public ComandaDetalle? ComandaDetalle { get; set; }
    public Usuario? Usuario { get; set; }
}
