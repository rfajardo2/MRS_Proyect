namespace MRSDrunk.Api.Models;

public sealed class Producto
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal? CostoEstimado { get; set; }
    public bool ControlaInventario { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Empresa? Empresa { get; set; }
    public ProductoCategoria? Categoria { get; set; }
    public ICollection<CuentaItem> CuentaItems { get; set; } = new List<CuentaItem>();
}
