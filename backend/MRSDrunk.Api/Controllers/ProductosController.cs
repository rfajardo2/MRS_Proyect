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
    private const int MaxCategoriaNombre = 80;
    private const int MaxCategoriaDescripcion = 250;
    private const int MaxProductoNombre = 120;
    private const int MaxProductoDescripcion = 400;

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
        var validation = await ValidateCategoria(null, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
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

        var validation = await ValidateCategoria(id, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
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
            .Include(x => x.UnidadVenta)
            .Include(x => x.UnidadInventario)
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Categoria!.Orden)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        var data = productos.Select(ToDto).ToList();
        return Ok(data);
    }

    [HttpGet("catalogo-operacion")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<ActionResult<IReadOnlyCollection<ProductoDto>>> GetCatalogoOperacion(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var productos = await db.Productos.AsNoTracking()
            .Include(x => x.Categoria)
            .Include(x => x.UnidadVenta)
            .Include(x => x.UnidadInventario)
            .Where(x => x.EmpresaId == empresaId && x.Estado && x.Categoria != null && x.Categoria.Estado)
            .OrderBy(x => x.Categoria!.Orden)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        var data = productos.Select(ToDto).ToList();
        return Ok(data);
    }

    [HttpGet("catalogo-admin-cuentas")]
    [RequirePermission("AdministracionCuentas.Usuarios.Editar")]
    public async Task<ActionResult<IReadOnlyCollection<ProductoDto>>> GetCatalogoAdminCuentas(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var productos = await db.Productos.AsNoTracking()
            .Include(x => x.Categoria)
            .Include(x => x.UnidadVenta)
            .Include(x => x.UnidadInventario)
            .Where(x => x.EmpresaId == empresaId && x.Estado && x.Categoria != null && x.Categoria.Estado)
            .OrderBy(x => x.Categoria!.Orden)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        var data = productos.Select(ToDto).ToList();
        return Ok(data);
    }

    [HttpGet("unidades")]
    [RequirePermission("Productos.Productos.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<UnidadMedidaDto>>> GetUnidades(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var unidades = await EnsureDefaultUnidades(empresaId, cancellationToken);
        return Ok(unidades);
    }

    [HttpPost]
    [RequirePermission("Productos.Productos.Crear")]
    public async Task<ActionResult<ProductoDto>> CrearProducto(UpsertProductoRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateProducto(null, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var entity = new Producto { EmpresaId = User.GetEmpresaId() };
        MapProducto(entity, request);
        db.Productos.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.Categoria).LoadAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.UnidadVenta).LoadAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.UnidadInventario).LoadAsync(cancellationToken);
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

        var validation = await ValidateProducto(id, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        MapProducto(entity, request);
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{productoId:int}/receta")]
    [RequirePermission("Productos.Productos.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<ProductoRecetaDto>>> GetReceta(int productoId, CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var productoExists = await db.Productos.AsNoTracking().AnyAsync(x => x.Id == productoId && x.EmpresaId == empresaId, cancellationToken);
        if (!productoExists)
        {
            return NotFound();
        }

        var receta = await db.ProductoRecetas.AsNoTracking()
            .Include(x => x.InsumoProducto)
            .Include(x => x.UnidadMedida)
            .Where(x => x.EmpresaId == empresaId && x.ProductoVentaId == productoId)
            .OrderBy(x => x.InsumoProducto!.Nombre)
            .Select(x => new ProductoRecetaDto(
                x.Id,
                x.ProductoVentaId,
                x.InsumoProductoId,
                x.InsumoProducto!.Nombre,
                x.Cantidad,
                x.UnidadMedidaId,
                x.UnidadMedida != null ? x.UnidadMedida.Nombre : null,
                x.Estado))
            .ToListAsync(cancellationToken);

        return Ok(receta);
    }

    [HttpPost("{productoId:int}/receta")]
    [RequirePermission("Productos.Productos.Editar")]
    public async Task<ActionResult<ProductoRecetaDto>> GuardarRecetaItem(int productoId, UpsertProductoRecetaRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateReceta(productoId, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var empresaId = User.GetEmpresaId();
        var item = await db.ProductoRecetas.FirstOrDefaultAsync(x =>
            x.EmpresaId == empresaId &&
            x.ProductoVentaId == productoId &&
            x.InsumoProductoId == request.InsumoProductoId,
            cancellationToken);

        if (item is null)
        {
            item = new ProductoReceta { EmpresaId = empresaId, ProductoVentaId = productoId, InsumoProductoId = request.InsumoProductoId };
            db.ProductoRecetas.Add(item);
        }

        item.Cantidad = request.Cantidad;
        item.UnidadMedidaId = request.UnidadMedidaId;
        item.Estado = request.Estado;
        item.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(item).Reference(x => x.InsumoProducto).LoadAsync(cancellationToken);
        await db.Entry(item).Reference(x => x.UnidadMedida).LoadAsync(cancellationToken);

        return Ok(new ProductoRecetaDto(item.Id, item.ProductoVentaId, item.InsumoProductoId, item.InsumoProducto!.Nombre, item.Cantidad, item.UnidadMedidaId, item.UnidadMedida?.Nombre, item.Estado));
    }

    [HttpDelete("{productoId:int}/receta/{itemId:int}")]
    [RequirePermission("Productos.Productos.Editar")]
    public async Task<IActionResult> EliminarRecetaItem(int productoId, int itemId, CancellationToken cancellationToken)
    {
        var item = await db.ProductoRecetas.FirstOrDefaultAsync(x => x.Id == itemId && x.ProductoVentaId == productoId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        db.ProductoRecetas.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateCategoria(int? id, UpsertProductoCategoriaRequest request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return "El nombre de la categoria es obligatorio.";
        }

        if (nombre.Length > MaxCategoriaNombre)
        {
            return $"El nombre de la categoria no puede superar {MaxCategoriaNombre} caracteres.";
        }

        if (Clean(request.Descripcion)?.Length > MaxCategoriaDescripcion)
        {
            return $"La descripcion de la categoria no puede superar {MaxCategoriaDescripcion} caracteres.";
        }

        if (request.Orden < 0)
        {
            return "El orden de la categoria no puede ser negativo.";
        }

        var empresaId = User.GetEmpresaId();
        var nombreNormalizado = nombre.ToUpperInvariant();
        var duplicate = await db.ProductoCategorias.AsNoTracking().AnyAsync(x =>
            x.EmpresaId == empresaId &&
            x.Nombre.ToUpper() == nombreNormalizado &&
            (!id.HasValue || x.Id != id.Value),
            cancellationToken);

        if (duplicate)
        {
            return "Ya existe una categoria con ese nombre.";
        }

        if (id.HasValue && !request.Estado)
        {
            var hasActiveProducts = await db.Productos.AsNoTracking().AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.CategoriaId == id.Value &&
                x.Estado,
                cancellationToken);

            if (hasActiveProducts)
            {
                return "No puedes inactivar una categoria con productos activos. Inactiva o mueve primero sus productos.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateProducto(int? id, UpsertProductoRequest request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return "El nombre del producto es obligatorio.";
        }

        if (nombre.Length > MaxProductoNombre)
        {
            return $"El nombre del producto no puede superar {MaxProductoNombre} caracteres.";
        }

        if (Clean(request.Descripcion)?.Length > MaxProductoDescripcion)
        {
            return $"La descripcion del producto no puede superar {MaxProductoDescripcion} caracteres.";
        }

        if (request.PrecioVenta <= 0)
        {
            return "El precio de venta debe ser mayor que cero.";
        }

        if (request.CostoEstimado is < 0)
        {
            return "El costo estimado no puede ser negativo.";
        }

        if (request.CostoEstimado > request.PrecioVenta)
        {
            return "El costo estimado no debe ser mayor que el precio de venta.";
        }

        if (request.FactorConversionInventario.HasValue && request.FactorConversionInventario <= 0)
        {
            return "El factor de conversion debe ser mayor que cero.";
        }

        var empresaId = User.GetEmpresaId();
        var nombreNormalizado = nombre.ToUpperInvariant();
        var categoria = await db.ProductoCategorias.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CategoriaId && x.EmpresaId == empresaId, cancellationToken);

        if (categoria is null)
        {
            return "La categoria no existe.";
        }

        if (request.Estado && !categoria.Estado)
        {
            return "No puedes activar un producto en una categoria inactiva.";
        }

        if (request.UnidadVentaId.HasValue)
        {
            var exists = await db.UnidadesMedida.AsNoTracking().AnyAsync(x => x.Id == request.UnidadVentaId.Value && x.EmpresaId == empresaId && x.Estado, cancellationToken);
            if (!exists)
            {
                return "La unidad de venta no existe o esta inactiva.";
            }
        }

        if (request.UnidadInventarioId.HasValue)
        {
            var exists = await db.UnidadesMedida.AsNoTracking().AnyAsync(x => x.Id == request.UnidadInventarioId.Value && x.EmpresaId == empresaId && x.Estado, cancellationToken);
            if (!exists)
            {
                return "La unidad de inventario no existe o esta inactiva.";
            }
        }

        var duplicate = await db.Productos.AsNoTracking().AnyAsync(x =>
            x.EmpresaId == empresaId &&
            x.Nombre.ToUpper() == nombreNormalizado &&
            (!id.HasValue || x.Id != id.Value),
            cancellationToken);

        return duplicate ? "Ya existe un producto con ese nombre." : null;
    }

    private static void MapProducto(Producto entity, UpsertProductoRequest request)
    {
        entity.CategoriaId = request.CategoriaId;
        entity.Nombre = request.Nombre.Trim();
        entity.Descripcion = Clean(request.Descripcion);
        entity.PrecioVenta = request.PrecioVenta;
        entity.CostoEstimado = request.CostoEstimado;
        entity.UnidadVentaId = request.UnidadVentaId;
        entity.UnidadInventarioId = request.UnidadInventarioId;
        entity.FactorConversionInventario = request.FactorConversionInventario ?? 1;
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
        x.UnidadVentaId,
        x.UnidadVenta?.Nombre,
        x.UnidadInventarioId,
        x.UnidadInventario?.Nombre,
        x.FactorConversionInventario,
        x.ControlaInventario,
        x.Estado);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string?> ValidateReceta(int productoId, UpsertProductoRecetaRequest request, CancellationToken cancellationToken)
    {
        if (request.Cantidad <= 0)
        {
            return "La cantidad de la receta debe ser mayor que cero.";
        }

        if (productoId == request.InsumoProductoId)
        {
            return "Un producto no puede consumirse a si mismo en la receta.";
        }

        var empresaId = User.GetEmpresaId();
        var productoVentaExists = await db.Productos.AsNoTracking().AnyAsync(x => x.Id == productoId && x.EmpresaId == empresaId, cancellationToken);
        if (!productoVentaExists)
        {
            return "El producto de venta no existe.";
        }

        var insumoExists = await db.Productos.AsNoTracking().AnyAsync(x => x.Id == request.InsumoProductoId && x.EmpresaId == empresaId && x.Estado && x.ControlaInventario, cancellationToken);
        if (!insumoExists)
        {
            return "El insumo debe ser un producto activo que controla inventario.";
        }

        if (request.UnidadMedidaId.HasValue)
        {
            var unidadExists = await db.UnidadesMedida.AsNoTracking().AnyAsync(x => x.Id == request.UnidadMedidaId.Value && x.EmpresaId == empresaId && x.Estado, cancellationToken);
            if (!unidadExists)
            {
                return "La unidad de medida no existe o esta inactiva.";
            }
        }

        return null;
    }

    private async Task<IReadOnlyCollection<UnidadMedidaDto>> EnsureDefaultUnidades(int empresaId, CancellationToken cancellationToken)
    {
        if (!await db.UnidadesMedida.AnyAsync(x => x.EmpresaId == empresaId, cancellationToken))
        {
            db.UnidadesMedida.AddRange(
                NewUnidad(empresaId, "UND", "Unidad", 0),
                NewUnidad(empresaId, "BOT", "Botella", 0),
                NewUnidad(empresaId, "COPA", "Copa", 2),
                NewUnidad(empresaId, "ML", "Mililitro", 3),
                NewUnidad(empresaId, "GR", "Gramo", 3),
                NewUnidad(empresaId, "PAQ", "Paquete", 0));
            await db.SaveChangesAsync(cancellationToken);
        }

        return await db.UnidadesMedida.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Nombre)
            .Select(x => new UnidadMedidaDto(x.Id, x.Codigo, x.Nombre, x.Decimales, x.Estado))
            .ToListAsync(cancellationToken);
    }

    private static UnidadMedida NewUnidad(int empresaId, string codigo, string nombre, int decimales) => new()
    {
        EmpresaId = empresaId,
        Codigo = codigo,
        Nombre = nombre,
        Decimales = decimales,
        Estado = true
    };
}
