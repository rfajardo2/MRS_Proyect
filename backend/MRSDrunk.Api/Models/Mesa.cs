namespace MRSDrunk.Api.Models;

public sealed class Mesa
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; } = 4;
    public int PosicionX { get; set; }
    public int PosicionY { get; set; }
    public string Estado { get; set; } = "Libre";
    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
