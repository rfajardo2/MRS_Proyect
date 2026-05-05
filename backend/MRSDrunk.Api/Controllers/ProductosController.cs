using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProductosController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet("categorias")]
    [RequirePermission("Productos.Categorias.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<ProductoCategoriaDto>>> GetCategorias(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var data = await db.ProductoCategorias.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.Nombre)
            .Select(x => new ProductoCategoriaDto(x.Id, x.Nombre, x.Descripcion, x.Orden, x.Estado))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost("categorias")]
    [RequirePermission("Productos.Categorias.Crear")]
    public async Task<ActionResult<ProductoCategoriaDto>> CrearCategoria(UpsertProductoCategoriaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { message = "El nombre es obligatorio." });
        }

        var entity = new ProductoCategoria
        {
            EmpresaId = User.GetEmpresaId(),
            Nombre = request.Nombre.Trim(),
            Descripcion = Clean(request.Descripcion),
            Orden = request.Orden,
            Estado = request.Estado
        };

        db.ProductoCategorias.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ProductoCategoriaDto(entity.Id, entity.Nombre, entity.Descripcion, entity.Orden, entity.Estado));
    }

    [HttpPut("categorias/{id:int}")]
    [RequirePermission("Productos.Categorias.Editar")]
    public async Task<IActionResult> EditarCategoria(int id, UpsertProductoCategoriaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.ProductoCategorias.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Nombre = request.Nombre.Trim();
        entity.Descripcion = Clean(request.Descripcion);
        entity.Orden = request.Orden;
        entity.Estado = request.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [RequirePermission("Productos.Productos.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<ProductoDto>>> GetProductos(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var productos = await db.Productos.AsNoTracking()
            .Include(x => x.Categoria)
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Categoria!.Orden)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        var data = productos.Select(ToDto).ToList();
        return Ok(data);
    }

    [HttpPost]
    [RequirePermission("Productos.Productos.Crear")]
    public async Task<ActionResult<ProductoDto>> CrearProducto(UpsertProductoRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateProducto(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var entity = new Producto { EmpresaId = User.GetEmpresaId() };
        MapProducto(entity, request);
        db.Productos.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.Categoria).LoadAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Productos.Productos.Editar")]
    public async Task<IActionResult> EditarProducto(int id, UpsertProductoRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Productos.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var validation = await ValidateProducto(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        MapProducto(entity, request);
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateProducto(UpsertProductoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "El nombre es obligatorio.";
        }

        if (request.PrecioVenta < 0 || request.CostoEstimado < 0)
        {
            return "Los valores no pueden ser negativos.";
        }

        var categoriaExists = await db.ProductoCategorias.AnyAsync(x => x.Id == request.CategoriaId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        return categoriaExists ? null : "La categoria no existe.";
    }

    private static void MapProducto(Producto entity, UpsertProductoRequest request)
    {
        entity.CategoriaId = request.CategoriaId;
        entity.Nombre = request.Nombre.Trim();
        entity.Descripcion = Clean(request.Descripcion);
        entity.PrecioVenta = request.PrecioVenta;
        entity.CostoEstimado = request.CostoEstimado;
        entity.ControlaInventario = request.ControlaInventario;
        entity.Estado = request.Estado;
    }

    private static ProductoDto ToDto(Producto x) => new(
        x.Id,
        x.CategoriaId,
        x.Categoria?.Nombre ?? "Sin categoria",
        x.Nombre,
        x.Descripcion,
        x.PrecioVenta,
        x.CostoEstimado,
        x.ControlaInventario,
        x.Estado);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
