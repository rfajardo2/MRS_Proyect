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
            .Where(x => productIds.Contains(x.Id) && x.ControlaInventario)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var item in items)
        {
            if (!productos.ContainsKey(item.ProductoId))
            {
                continue;
            }

            await RegistrarMovimientoAsync(
                cuenta.EmpresaId,
                cuenta.SucursalId,
                item.ProductoId,
                usuarioId,
                "SalidaVenta",
                item.Cantidad,
                null,
                cuenta.Numero,
                $"Venta cuenta {cuenta.Numero}",
                cuenta.Id,
                item.Id,
                cancellationToken);

            item.InventarioAplicado = true;
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
