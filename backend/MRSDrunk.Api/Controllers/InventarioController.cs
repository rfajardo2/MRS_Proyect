using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/inventario")]
[Authorize]
public sealed class InventarioController(MrsDrunkDbContext db, IInventarioService inventarioService) : ControllerBase
{
    private const int MaxReferencia = 80;
    private const int MaxMotivo = 200;

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Entrada",
        "AjusteEntrada",
        "AjusteSalida",
        "Devolucion",
        "Rotura",
        "Vencimiento",
        "Dano",
        "ConsumoInterno"
    };

    private static readonly HashSet<string> TiposConMotivoObligatorio = new(StringComparer.OrdinalIgnoreCase)
    {
        "AjusteEntrada",
        "AjusteSalida",
        "Devolucion",
        "Rotura",
        "Vencimiento",
        "Dano",
        "ConsumoInterno"
    };

    [HttpGet("stock")]
    [RequirePermission("Productos.Inventario.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<InventarioStockDto>>> Stock(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var productos = await db.Productos.AsNoTracking()
            .Include(x => x.Categoria)
            .Include(x => x.UnidadInventario)
            .Where(x => x.EmpresaId == empresaId && x.ControlaInventario)
            .OrderBy(x => x.Categoria!.Orden)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        var stocks = await db.InventarioStocks.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId)
            .ToDictionaryAsync(x => x.ProductoId, cancellationToken);

        var data = productos.Select(producto =>
        {
            stocks.TryGetValue(producto.Id, out var stock);
            var actual = stock?.CantidadActual ?? 0;
            var minimo = stock?.CantidadMinima ?? 0;
            return new InventarioStockDto(
                producto.Id,
                producto.Nombre,
                producto.Categoria?.Nombre ?? "Sin categoria",
                producto.UnidadInventario?.Nombre,
                actual,
                minimo,
                minimo > 0 && actual <= minimo,
                producto.Estado);
        }).ToList();

        return Ok(data);
    }

    [HttpGet("movimientos")]
    [RequirePermission("Productos.Inventario.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<InventarioMovimientoDto>>> Movimientos(CancellationToken cancellationToken)
    {
        var data = await db.InventarioMovimientos.AsNoTracking()
            .Include(x => x.Producto)
            .ThenInclude(x => x!.Categoria)
            .Include(x => x.Usuario)
            .Include(x => x.Lote)
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.SucursalId == User.GetSucursalId())
            .OrderByDescending(x => x.FechaMovimiento)
            .Take(120)
            .Select(x => new InventarioMovimientoDto(
                x.Id,
                x.ProductoId,
                x.Producto!.Nombre,
                x.Tipo,
                x.Cantidad,
                x.SaldoAnterior,
                x.SaldoNuevo,
                x.CostoUnitario,
                x.Referencia,
                x.Motivo,
                x.Lote != null ? x.Lote.CodigoLote : null,
                x.Lote != null ? x.Lote.FechaVencimiento : null,
                x.Usuario!.NombreCompleto,
                x.FechaMovimiento))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost("movimientos")]
    [RequirePermission("Productos.Inventario.Mover")]
    public async Task<IActionResult> RegistrarMovimiento(RegistrarMovimientoInventarioRequest request, CancellationToken cancellationToken)
    {
        if (!TiposPermitidos.Contains(request.Tipo))
        {
            return BadRequest(new { message = "El tipo de movimiento no es valido." });
        }

        if (request.Cantidad <= 0)
        {
            return BadRequest(new { message = "La cantidad debe ser mayor que cero." });
        }

        if (request.CostoUnitario is < 0)
        {
            return BadRequest(new { message = "El costo unitario no puede ser negativo." });
        }

        if (Clean(request.Referencia)?.Length > MaxReferencia)
        {
            return BadRequest(new { message = $"La referencia no puede superar {MaxReferencia} caracteres." });
        }

        if (Clean(request.Motivo)?.Length > MaxMotivo)
        {
            return BadRequest(new { message = $"El motivo no puede superar {MaxMotivo} caracteres." });
        }

        if (TiposConMotivoObligatorio.Contains(request.Tipo) && string.IsNullOrWhiteSpace(request.Motivo))
        {
            return BadRequest(new { message = "Este tipo de movimiento requiere motivo." });
        }

        var producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == request.ProductoId &&
            x.EmpresaId == User.GetEmpresaId() &&
            x.ControlaInventario &&
            x.Estado,
            cancellationToken);

        if (producto is null)
        {
            return BadRequest(new { message = "El producto no existe, esta inactivo o no controla inventario." });
        }

        try
        {
            await inventarioService.RegistrarMovimientoAsync(
                User.GetEmpresaId(),
                User.GetSucursalId(),
                request.ProductoId,
                User.GetUsuarioId(),
                request.Tipo,
                request.Cantidad,
                request.CostoUnitario,
                request.Referencia,
                request.Motivo,
                null,
                null,
                null,
                null,
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("proveedores")]
    [RequirePermission("Productos.Inventario.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<ProveedorDto>>> Proveedores(CancellationToken cancellationToken)
    {
        var data = await db.Proveedores.AsNoTracking()
            .Where(x => x.EmpresaId == User.GetEmpresaId())
            .OrderBy(x => x.Nombre)
            .Select(x => new ProveedorDto(x.Id, x.Nombre, x.Nit, x.Telefono, x.Correo, x.Estado))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpPost("proveedores")]
    [RequirePermission("Productos.Inventario.Compras")]
    public async Task<ActionResult<ProveedorDto>> CrearProveedor(ProveedorDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { message = "El nombre del proveedor es obligatorio." });
        }

        var nombre = request.Nombre.Trim();
        var empresaId = User.GetEmpresaId();
        var exists = await db.Proveedores.AsNoTracking().AnyAsync(x => x.EmpresaId == empresaId && x.Nombre.ToUpper() == nombre.ToUpper(), cancellationToken);
        if (exists)
        {
            return BadRequest(new { message = "Ya existe un proveedor con ese nombre." });
        }

        var entity = new Proveedor
        {
            EmpresaId = empresaId,
            Nombre = nombre,
            Nit = Clean(request.Nit),
            Telefono = Clean(request.Telefono),
            Correo = Clean(request.Correo),
            Estado = request.Estado
        };
        db.Proveedores.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ProveedorDto(entity.Id, entity.Nombre, entity.Nit, entity.Telefono, entity.Correo, entity.Estado));
    }

    [HttpPost("compras")]
    [RequirePermission("Productos.Inventario.Compras")]
    public async Task<IActionResult> CrearCompra(CrearInventarioCompraRequest request, CancellationToken cancellationToken)
    {
        if (request.Detalles is null || !request.Detalles.Any())
        {
            return BadRequest(new { message = "La compra debe tener al menos un producto." });
        }

        if (request.Detalles.Any(x => x.Cantidad <= 0 || x.CostoUnitario < 0))
        {
            return BadRequest(new { message = "Las cantidades deben ser mayores que cero y los costos no pueden ser negativos." });
        }

        var empresaId = User.GetEmpresaId();
        if (request.ProveedorId.HasValue)
        {
            var proveedorExists = await db.Proveedores.AsNoTracking().AnyAsync(x => x.Id == request.ProveedorId.Value && x.EmpresaId == empresaId && x.Estado, cancellationToken);
            if (!proveedorExists)
            {
                return BadRequest(new { message = "El proveedor no existe o esta inactivo." });
            }
        }

        var productoIds = request.Detalles.Select(x => x.ProductoId).Distinct().ToList();
        var productos = await db.Productos.AsNoTracking()
            .Where(x => productoIds.Contains(x.Id) && x.EmpresaId == empresaId && x.Estado && x.ControlaInventario)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (productos.Count != productoIds.Count)
        {
            return BadRequest(new { message = "Todos los productos de la compra deben estar activos y controlar inventario." });
        }

        var compra = new InventarioCompra
        {
            EmpresaId = empresaId,
            SucursalId = User.GetSucursalId(),
            ProveedorId = request.ProveedorId,
            UsuarioId = User.GetUsuarioId(),
            NumeroFactura = Clean(request.NumeroFactura),
            FechaCompra = request.FechaCompra ?? DateTime.UtcNow,
            Observacion = Clean(request.Observacion)
        };

        db.InventarioCompras.Add(compra);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var detalle in request.Detalles)
        {
            var total = detalle.Cantidad * detalle.CostoUnitario;
            var loteCodigo = Clean(detalle.CodigoLote) ?? $"COMP-{compra.Id}-{detalle.ProductoId}";
            var lote = new InventarioLote
            {
                EmpresaId = empresaId,
                SucursalId = User.GetSucursalId(),
                ProductoId = detalle.ProductoId,
                ProveedorId = request.ProveedorId,
                CodigoLote = loteCodigo,
                FechaVencimiento = detalle.FechaVencimiento,
                CostoUnitario = detalle.CostoUnitario,
                CantidadActual = 0,
                Estado = true
            };
            db.InventarioLotes.Add(lote);
            await db.SaveChangesAsync(cancellationToken);

            db.InventarioCompraDetalles.Add(new InventarioCompraDetalle
            {
                CompraId = compra.Id,
                ProductoId = detalle.ProductoId,
                LoteId = lote.Id,
                Cantidad = detalle.Cantidad,
                CostoUnitario = detalle.CostoUnitario,
                CodigoLote = loteCodigo,
                FechaVencimiento = detalle.FechaVencimiento,
                Total = total
            });

            compra.Total += total;
            await inventarioService.RegistrarMovimientoAsync(
                empresaId,
                User.GetSucursalId(),
                detalle.ProductoId,
                User.GetUsuarioId(),
                "Entrada",
                detalle.Cantidad,
                detalle.CostoUnitario,
                compra.NumeroFactura,
                $"Compra {compra.NumeroFactura ?? compra.Id.ToString()}",
                null,
                null,
                lote.Id,
                compra.Id,
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("compras")]
    [RequirePermission("Productos.Inventario.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<InventarioCompraDto>>> Compras(CancellationToken cancellationToken)
    {
        var compras = await db.InventarioCompras.AsNoTracking()
            .Include(x => x.Proveedor)
            .Include(x => x.Usuario)
            .Include(x => x.Detalles)
            .ThenInclude(x => x.Producto)
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.SucursalId == User.GetSucursalId())
            .OrderByDescending(x => x.FechaCompra)
            .Take(80)
            .ToListAsync(cancellationToken);

        return Ok(compras.Select(x => new InventarioCompraDto(
            x.Id,
            x.Proveedor?.Nombre,
            x.NumeroFactura,
            x.FechaCompra,
            x.Total,
            x.Usuario?.NombreCompleto ?? "-",
            x.Detalles.Select(d => new InventarioCompraDetalleDto(d.ProductoId, d.Producto?.Nombre ?? "-", d.Cantidad, d.CostoUnitario, d.CodigoLote, d.FechaVencimiento, d.Total)).ToList())).ToList());
    }

    [HttpGet("lotes")]
    [RequirePermission("Productos.Inventario.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<InventarioLoteDto>>> Lotes(CancellationToken cancellationToken)
    {
        var hoy = DateTime.UtcNow.Date;
        var proximo = hoy.AddDays(30);
        var data = await db.InventarioLotes.AsNoTracking()
            .Include(x => x.Producto)
            .ThenInclude(x => x!.Categoria)
            .Include(x => x.Proveedor)
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.SucursalId == User.GetSucursalId() && x.CantidadActual > 0)
            .OrderBy(x => x.FechaVencimiento ?? DateTime.MaxValue)
            .ThenBy(x => x.Producto!.Nombre)
            .Select(x => new InventarioLoteDto(
                x.Id,
                x.ProductoId,
                x.Producto!.Nombre,
                x.Producto.Categoria != null ? x.Producto.Categoria.Nombre : "Sin categoria",
                x.CodigoLote,
                x.FechaVencimiento,
                x.CantidadActual,
                x.CostoUnitario,
                x.Proveedor != null ? x.Proveedor.Nombre : null,
                x.FechaVencimiento.HasValue && x.FechaVencimiento.Value.Date < hoy,
                x.FechaVencimiento.HasValue && x.FechaVencimiento.Value.Date >= hoy && x.FechaVencimiento.Value.Date <= proximo))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("reportes")]
    [RequirePermission("Productos.Inventario.Reportes")]
    public async Task<ActionResult<InventarioReporteDto>> Reportes(CancellationToken cancellationToken)
    {
        var desde = DateTime.UtcNow.Date;
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var itemsVendidos = await db.CuentaItems.AsNoTracking()
            .Include(x => x.Cuenta)
            .Where(x => x.Cuenta != null && x.Cuenta.EmpresaId == empresaId && x.Cuenta.SucursalId == sucursalId && x.Cuenta.FechaApertura >= desde && !x.Eliminado)
            .ToListAsync(cancellationToken);

        var productosVendidos = itemsVendidos
            .GroupBy(x => x.ProductoNombre)
            .Select(x => new BalanceDiaProductoDto(x.Key, x.Sum(i => i.Cantidad), x.Sum(i => i.Total)))
            .OrderByDescending(x => x.Total)
            .Take(20)
            .ToList();

        var movimientosPerdida = await db.InventarioMovimientos.AsNoTracking()
            .Include(x => x.Producto)
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && x.FechaMovimiento >= desde && (x.Tipo == "Rotura" || x.Tipo == "Vencimiento" || x.Tipo == "Dano"))
            .ToListAsync(cancellationToken);

        var perdidas = movimientosPerdida
            .GroupBy(x => x.Producto?.Nombre ?? "Sin producto")
            .Select(x => new BalanceDiaProductoDto(x.Key, x.Sum(i => i.Cantidad), x.Sum(i => i.Cantidad * (i.CostoUnitario ?? 0))))
            .OrderByDescending(x => x.Total)
            .ToList();

        var stockValorizado = await db.InventarioStocks.AsNoTracking()
            .Include(x => x.Producto)
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId)
            .SumAsync(x => x.CantidadActual * (x.Producto!.CostoEstimado ?? 0), cancellationToken);

        var comprasPeriodo = await db.InventarioCompras.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && x.FechaCompra >= desde)
            .SumAsync(x => x.Total, cancellationToken);

        return Ok(new InventarioReporteDto(productosVendidos, perdidas, stockValorizado, comprasPeriodo));
    }

    [HttpPut("stock-minimo")]
    [RequirePermission("Productos.Inventario.Editar")]
    public async Task<IActionResult> StockMinimo(ActualizarStockMinimoRequest request, CancellationToken cancellationToken)
    {
        if (request.CantidadMinima < 0)
        {
            return BadRequest(new { message = "El stock minimo no puede ser negativo." });
        }

        var productoExists = await db.Productos.AsNoTracking().AnyAsync(x =>
            x.Id == request.ProductoId &&
            x.EmpresaId == User.GetEmpresaId() &&
            x.ControlaInventario &&
            x.Estado,
            cancellationToken);

        if (!productoExists)
        {
            return BadRequest(new { message = "El producto no existe, esta inactivo o no controla inventario." });
        }

        var stock = await db.InventarioStocks.FirstOrDefaultAsync(x =>
            x.EmpresaId == User.GetEmpresaId() &&
            x.SucursalId == User.GetSucursalId() &&
            x.ProductoId == request.ProductoId,
            cancellationToken);

        if (stock is null)
        {
            stock = new InventarioStock
            {
                EmpresaId = User.GetEmpresaId(),
                SucursalId = User.GetSucursalId(),
                ProductoId = request.ProductoId
            };
            db.InventarioStocks.Add(stock);
        }

        stock.CantidadMinima = request.CantidadMinima;
        stock.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
