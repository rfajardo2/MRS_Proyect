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

    [HttpGet("stock")]
    [RequirePermission("Productos.Inventario.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<InventarioStockDto>>> Stock(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var productos = await db.Productos.AsNoTracking()
            .Include(x => x.Categoria)
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
            .Include(x => x.Usuario)
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

        var producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == request.ProductoId &&
            x.EmpresaId == User.GetEmpresaId() &&
            x.ControlaInventario,
            cancellationToken);

        if (producto is null)
        {
            return BadRequest(new { message = "El producto no existe o no controla inventario." });
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
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
            x.ControlaInventario,
            cancellationToken);

        if (!productoExists)
        {
            return BadRequest(new { message = "El producto no existe o no controla inventario." });
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
}
