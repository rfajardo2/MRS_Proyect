using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

public sealed class InventarioService(MrsDrunkDbContext db) : IInventarioService
{
    private static readonly HashSet<string> Entradas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Entrada",
        "AjusteEntrada",
        "Devolucion"
    };

    public async Task RegistrarMovimientoAsync(
        int empresaId,
        int? sucursalId,
        int productoId,
        int usuarioId,
        string tipo,
        decimal cantidad,
        decimal? costoUnitario,
        string? referencia,
        string? motivo,
        int? cuentaId,
        int? cuentaItemId,
        int? loteId,
        int? compraId,
        CancellationToken cancellationToken)
    {
        if (cantidad <= 0)
        {
            throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
        }

        var stock = await db.InventarioStocks.FirstOrDefaultAsync(x =>
            x.EmpresaId == empresaId &&
            x.SucursalId == sucursalId &&
            x.ProductoId == productoId,
            cancellationToken);

        if (stock is null)
        {
            stock = new InventarioStock { EmpresaId = empresaId, SucursalId = sucursalId, ProductoId = productoId };
            db.InventarioStocks.Add(stock);
        }

        var saldoAnterior = stock.CantidadActual;
        var signedQuantity = Entradas.Contains(tipo) ? cantidad : -cantidad;
        if (stock.CantidadActual + signedQuantity < 0)
        {
            throw new InvalidOperationException("El movimiento dejaria el inventario en negativo. Revisa el stock disponible o registra primero una entrada.");
        }

        if (signedQuantity > 0 && loteId.HasValue)
        {
            var lote = await db.InventarioLotes.FirstOrDefaultAsync(x =>
                x.Id == loteId.Value &&
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId &&
                x.ProductoId == productoId,
                cancellationToken);

            if (lote is null)
            {
                throw new InvalidOperationException("El lote no existe para este producto.");
            }

            lote.CantidadActual += cantidad;
            lote.FechaModificacion = DateTime.UtcNow;
        }
        else if (signedQuantity < 0)
        {
            await DescontarLotesAsync(empresaId, sucursalId, productoId, cantidad, cancellationToken);
        }

        stock.CantidadActual += signedQuantity;
        stock.FechaModificacion = DateTime.UtcNow;

        db.InventarioMovimientos.Add(new InventarioMovimiento
        {
            EmpresaId = empresaId,
            SucursalId = sucursalId,
            ProductoId = productoId,
            UsuarioId = usuarioId,
            CuentaId = cuentaId,
            CuentaItemId = cuentaItemId,
            LoteId = loteId,
            CompraId = compraId,
            Tipo = tipo,
            Cantidad = cantidad,
            SaldoAnterior = saldoAnterior,
            SaldoNuevo = stock.CantidadActual,
            CostoUnitario = costoUnitario,
            Referencia = Clean(referencia),
            Motivo = Clean(motivo)
        });
    }

    public async Task AplicarSalidaVentaAsync(Cuenta cuenta, int usuarioId, CancellationToken cancellationToken)
    {
        var items = cuenta.Items.Where(x => !x.Eliminado && !x.InventarioAplicado).ToList();
        if (!items.Any())
        {
            return;
        }

        var productIds = items.Select(x => x.ProductoId).Distinct().ToList();
        var productos = await db.Productos.AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var recetas = await db.ProductoRecetas.AsNoTracking()
            .Include(x => x.InsumoProducto)
            .Where(x => x.EmpresaId == cuenta.EmpresaId && productIds.Contains(x.ProductoVentaId) && x.Estado)
            .ToListAsync(cancellationToken);
        var recetasPorProducto = recetas.GroupBy(x => x.ProductoVentaId).ToDictionary(x => x.Key, x => x.ToList());

        foreach (var item in items)
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto))
            {
                continue;
            }

            if (recetasPorProducto.TryGetValue(item.ProductoId, out var receta) && receta.Any())
            {
                foreach (var ingrediente in receta)
                {
                    if (ingrediente.InsumoProducto is null || !ingrediente.InsumoProducto.ControlaInventario)
                    {
                        continue;
                    }

                    await RegistrarMovimientoAsync(
                        cuenta.EmpresaId,
                        cuenta.SucursalId,
                        ingrediente.InsumoProductoId,
                        usuarioId,
                        "SalidaVenta",
                        item.Cantidad * ingrediente.Cantidad,
                        null,
                        cuenta.Numero,
                        $"Receta {producto.Nombre} - cuenta {cuenta.Numero}",
                        cuenta.Id,
                        item.Id,
                        null,
                        null,
                        cancellationToken);
                }
            }
            else if (producto.ControlaInventario)
            {
                await RegistrarMovimientoAsync(
                    cuenta.EmpresaId,
                    cuenta.SucursalId,
                    item.ProductoId,
                    usuarioId,
                    "SalidaVenta",
                    item.Cantidad * producto.FactorConversionInventario,
                    null,
                    cuenta.Numero,
                    $"Venta cuenta {cuenta.Numero}",
                    cuenta.Id,
                    item.Id,
                    null,
                    null,
                    cancellationToken);
            }

            item.InventarioAplicado = true;
        }
    }

    private async Task DescontarLotesAsync(int empresaId, int? sucursalId, int productoId, decimal cantidad, CancellationToken cancellationToken)
    {
        var pendiente = cantidad;
        var lotes = await db.InventarioLotes
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId &&
                x.ProductoId == productoId &&
                x.CantidadActual > 0)
            .OrderBy(x => x.FechaVencimiento ?? DateTime.MaxValue)
            .ThenBy(x => x.FechaCreacion)
            .ToListAsync(cancellationToken);

        foreach (var lote in lotes)
        {
            if (pendiente <= 0)
            {
                break;
            }

            var consumo = Math.Min(lote.CantidadActual, pendiente);
            lote.CantidadActual -= consumo;
            lote.Estado = lote.CantidadActual > 0;
            lote.FechaModificacion = DateTime.UtcNow;
            pendiente -= consumo;
        }

        if (pendiente > 0)
        {
            throw new InvalidOperationException("No hay lotes suficientes para cubrir la salida de inventario.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
