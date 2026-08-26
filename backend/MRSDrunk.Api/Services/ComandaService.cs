using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Hubs;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

public sealed class ComandaService(MrsDrunkDbContext db, IHubContext<ComandasHub> hub, IHubContext<SeguimientoPublicoHub> publicHub) : IComandaService
{
    public async Task<ComandaDto?> EnviarComandaAsync(
        int empresaId,
        int meseroId,
        int cuentaId,
        IReadOnlyCollection<EnviarComandaItemRequest> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Debes agregar al menos un producto para enviar la comanda.");
        }

        var cuenta = await db.Cuentas
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == cuentaId &&
                x.EmpresaId == empresaId &&
                x.MeseroId == meseroId &&
                (x.Estado == "Abierta" || x.Estado == "Rechazada"),
                cancellationToken);

        if (cuenta is null)
        {
            return null;
        }

        var productoIds = items.Select(x => x.ProductoId).Distinct().ToList();
        var productos = await db.Productos.AsNoTracking()
            .Include(x => x.AreaPreparacion)
            .Where(x => productoIds.Contains(x.Id) && x.EmpresaId == empresaId && x.Estado)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var detalles = new List<ComandaDetalle>();
        foreach (var request in items)
        {
            if (request.Cantidad <= 0)
            {
                throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
            }

            if (!productos.TryGetValue(request.ProductoId, out var producto))
            {
                throw new InvalidOperationException("Uno de los productos no existe o esta inactivo.");
            }

            var precio = request.PrecioUnitario ?? producto.PrecioVenta;
            var cuentaItem = new CuentaItem
            {
                CuentaId = cuenta.Id,
                ProductoId = producto.Id,
                ProductoNombre = producto.Nombre,
                Cantidad = request.Cantidad,
                PrecioUnitario = precio,
                Descuento = request.Descuento,
                Total = Math.Max(0, request.Cantidad * precio - request.Descuento),
                UsuarioCreacionId = meseroId
            };

            db.CuentaItems.Add(cuentaItem);
            cuenta.Items.Add(cuentaItem);

            detalles.Add(new ComandaDetalle
            {
                CuentaItem = cuentaItem,
                ProductoId = producto.Id,
                ProductoNombre = producto.Nombre,
                AreaPreparacionId = producto.AreaPreparacionId,
                Cantidad = request.Cantidad,
                RequierePreparacion = producto.RequierePreparacion,
                Estado = "PENDIENTE",
                Observacion = Clean(request.Observacion)
            });
        }

        Recalcular(cuenta);

        var comanda = new Comanda
        {
            EmpresaId = empresaId,
            SucursalId = cuenta.SucursalId,
            CuentaId = cuenta.Id,
            Numero = await NextNumero(empresaId, cancellationToken),
            MeseroId = meseroId,
            Estado = "PENDIENTE"
        };

        foreach (var detalle in detalles)
        {
            detalle.Comanda = comanda;
            comanda.Detalles.Add(detalle);
            db.ComandaDetalleEventos.Add(new ComandaDetalleEvento
            {
                ComandaDetalle = detalle,
                EstadoAnterior = null,
                EstadoNuevo = "PENDIENTE",
                UsuarioId = meseroId,
                Observacion = "Comanda enviada"
            });
        }

        db.Comandas.Add(comanda);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var detalle in comanda.Detalles)
        {
            detalle.AreaPreparacion = productos[detalle.ProductoId].AreaPreparacion;
        }

        await hub.Clients.Group(ComandasHub.PreparacionGroup(empresaId))
            .SendAsync("NuevaComanda", new { comandaId = comanda.Id, cuentaId = cuenta.Id }, cancellationToken);

        if (!string.IsNullOrEmpty(cuenta.TokenPublico))
        {
            await publicHub.Clients.Group(SeguimientoPublicoHub.PublicGroup(cuenta.TokenPublico))
                .SendAsync("ConsumoActualizado", cancellationToken);
        }

        return ToDto(comanda, meseroNombre: null);
    }

    public async Task<IReadOnlyCollection<ComandaDto>> GetComandasPorCuentaAsync(
        int empresaId,
        int meseroId,
        int cuentaId,
        CancellationToken cancellationToken)
    {
        var comandas = await db.Comandas.AsNoTracking()
            .Include(x => x.Mesero)
            .Include(x => x.Detalles).ThenInclude(x => x.AreaPreparacion)
            .Include(x => x.Detalles).ThenInclude(x => x.UsuarioToma)
            .Include(x => x.Detalles).ThenInclude(x => x.UsuarioListo)
            .Include(x => x.Detalles).ThenInclude(x => x.UsuarioDespacho)
            .Include(x => x.Detalles).ThenInclude(x => x.UsuarioEntrega)
            .Where(x => x.EmpresaId == empresaId && x.CuentaId == cuentaId && x.Cuenta!.MeseroId == meseroId)
            .OrderByDescending(x => x.FechaCreacion)
            .ToListAsync(cancellationToken);

        return comandas.Select(x => ToDto(x, x.Mesero?.NombreCompleto)).ToList();
    }

    public async Task<IReadOnlyCollection<AreaPreparacionDto>> GetAreasAsync(int empresaId, CancellationToken cancellationToken) =>
        await db.AreasPreparacion.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Estado)
            .OrderBy(x => x.Orden).ThenBy(x => x.Nombre)
            .Select(x => new AreaPreparacionDto(x.Id, x.Nombre, x.Descripcion, x.Orden))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PreparacionDetalleDto>> GetTableroAsync(int empresaId, int? areaId, CancellationToken cancellationToken)
    {
        var estadosTablero = new[] { "PENDIENTE", "EN_PREPARACION", "LISTO" };
        var comandaIdsActivas = db.ComandaDetalles.Where(x => estadosTablero.Contains(x.Estado)).Select(x => x.ComandaId);
        var query = db.ComandaDetalles.AsNoTracking()
            .Include(x => x.Comanda!).ThenInclude(x => x.Cuenta)
            .Include(x => x.Comanda!).ThenInclude(x => x.Mesero)
            .Include(x => x.AreaPreparacion)
            .Include(x => x.UsuarioToma)
            .Include(x => x.UsuarioListo)
            .Where(x => x.Comanda!.EmpresaId == empresaId &&
                (estadosTablero.Contains(x.Estado) || (x.Estado == "CANCELADO" && comandaIdsActivas.Contains(x.ComandaId))));

        if (areaId.HasValue)
        {
            query = query.Where(x => x.AreaPreparacionId == areaId.Value);
        }

        var detalles = await query.OrderBy(x => x.FechaCreacion).ToListAsync(cancellationToken);
        return detalles.Select(ToPreparacionDto).ToList();
    }

    public async Task<IReadOnlyCollection<HistorialEventoDto>> GetHistorialAsync(int empresaId, int? areaId, CancellationToken cancellationToken)
    {
        var query = db.ComandaDetalleEventos.AsNoTracking()
            .Include(x => x.ComandaDetalle!).ThenInclude(x => x.Comanda!).ThenInclude(x => x.Cuenta)
            .Include(x => x.Usuario)
            .Where(x => x.ComandaDetalle!.Comanda!.EmpresaId == empresaId);

        if (areaId.HasValue)
        {
            query = query.Where(x => x.ComandaDetalle!.AreaPreparacionId == areaId.Value);
        }

        var eventos = await query
            .OrderByDescending(x => x.Fecha)
            .Take(80)
            .ToListAsync(cancellationToken);

        return eventos.Select(ToHistorialDto).ToList();
    }

    private static HistorialEventoDto ToHistorialDto(ComandaDetalleEvento e) => new(
        e.Id,
        e.ComandaDetalle!.Comanda!.Numero,
        e.ComandaDetalle.Comanda.Cuenta?.Numero ?? string.Empty,
        e.ComandaDetalle.Comanda.Cuenta?.Mesa,
        e.ComandaDetalle.ProductoNombre,
        e.EstadoAnterior,
        e.EstadoNuevo,
        e.Usuario?.NombreCompleto ?? "Usuario",
        e.Fecha);

    public async Task TomarAsync(int empresaId, int usuarioId, int detalleId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await db.ComandaDetalles
            .Where(x => x.Id == detalleId && x.Comanda!.EmpresaId == empresaId && x.Estado == "PENDIENTE" && x.RequierePreparacion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, "EN_PREPARACION")
                .SetProperty(x => x.UsuarioTomaId, usuarioId)
                .SetProperty(x => x.FechaToma, now), cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException("El producto ya fue tomado por otro usuario o no esta pendiente.");
        }

        await RegistrarEventoYRecalcularAsync(detalleId, "PENDIENTE", "EN_PREPARACION", usuarioId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> TomarComandaAsync(int empresaId, int usuarioId, int comandaId, CancellationToken cancellationToken)
    {
        var ids = await db.ComandaDetalles.AsNoTracking()
            .Where(x => x.ComandaId == comandaId && x.Comanda!.EmpresaId == empresaId && x.Estado == "PENDIENTE" && x.RequierePreparacion)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            throw new InvalidOperationException("No hay productos pendientes que requieran preparacion en esta comanda.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await db.ComandaDetalles
            .Where(x => ids.Contains(x.Id) && x.Estado == "PENDIENTE")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, "EN_PREPARACION")
                .SetProperty(x => x.UsuarioTomaId, usuarioId)
                .SetProperty(x => x.FechaToma, now), cancellationToken);

        foreach (var id in ids)
        {
            await RegistrarEventoYRecalcularAsync(id, "PENDIENTE", "EN_PREPARACION", usuarioId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task<int> MarcarListoComandaAsync(int empresaId, int usuarioId, int comandaId, IReadOnlyCollection<int> detallesNoPreparar, CancellationToken cancellationToken)
    {
        var detalles = await db.ComandaDetalles.AsNoTracking()
            .Where(x => x.ComandaId == comandaId && x.Comanda!.EmpresaId == empresaId && x.Estado == "EN_PREPARACION")
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (detalles.Count == 0)
        {
            throw new InvalidOperationException("No hay productos en preparacion para esta comanda.");
        }

        var noPreparar = new HashSet<int>(detallesNoPreparar ?? Array.Empty<int>());
        var listoIds = detalles.Where(x => !noPreparar.Contains(x)).ToList();
        var cancelarIds = detalles.Where(x => noPreparar.Contains(x)).ToList();

        foreach (var id in cancelarIds)
        {
            await CancelarYEliminarItemAsync(id, usuarioId, "Eliminado por despacho de barra", cancellationToken);
        }

        if (listoIds.Count > 0)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTime.UtcNow;
            await db.ComandaDetalles
                .Where(x => listoIds.Contains(x.Id) && x.Estado == "EN_PREPARACION")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Estado, "LISTO")
                    .SetProperty(x => x.UsuarioListoId, usuarioId)
                    .SetProperty(x => x.FechaListo, now), cancellationToken);

            foreach (var id in listoIds)
            {
                await RegistrarEventoYRecalcularAsync(id, "EN_PREPARACION", "LISTO", usuarioId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return listoIds.Count;
    }

    public async Task<int> DespacharComandaAsync(int empresaId, int usuarioId, int comandaId, CancellationToken cancellationToken)
    {
        var ids = await db.ComandaDetalles.AsNoTracking()
            .Where(x => x.ComandaId == comandaId && x.Comanda!.EmpresaId == empresaId && x.Estado == "LISTO")
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            throw new InvalidOperationException("No hay productos listos para despachar en esta comanda.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await db.ComandaDetalles
            .Where(x => ids.Contains(x.Id) && x.Estado == "LISTO")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, "DESPACHADO")
                .SetProperty(x => x.UsuarioDespachoId, usuarioId)
                .SetProperty(x => x.FechaDespacho, now), cancellationToken);

        foreach (var id in ids)
        {
            await RegistrarEventoYRecalcularAsync(id, "LISTO", "DESPACHADO", usuarioId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    private async Task CancelarYEliminarItemAsync(int detalleId, int usuarioId, string motivo, CancellationToken cancellationToken)
    {
        var detalle = await db.ComandaDetalles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == detalleId, cancellationToken);

        if (detalle is null || detalle.Estado == "CANCELADO")
        {
            return;
        }

        var cuentaItem = await db.CuentaItems
            .Include(x => x.Cuenta!).ThenInclude(x => x.Items)
            .FirstAsync(x => x.Id == detalle.CuentaItemId, cancellationToken);

        if (!cuentaItem.Eliminado)
        {
            cuentaItem.Eliminado = true;
            cuentaItem.MotivoEliminacion = motivo;
            cuentaItem.UsuarioEliminacionId = usuarioId;
            cuentaItem.FechaEliminacion = DateTime.UtcNow;
            RecalcularCuenta(cuentaItem.Cuenta!);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.ComandaDetalles
            .Where(x => x.Id == detalleId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Estado, "CANCELADO"), cancellationToken);

        await RegistrarEventoYRecalcularAsync(detalleId, detalle.Estado, "CANCELADO", usuarioId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void RecalcularCuenta(Cuenta cuenta)
    {
        cuenta.Subtotal = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Cantidad * x.PrecioUnitario);
        cuenta.Descuento = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Descuento);
        cuenta.Total = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Total);
        cuenta.FechaModificacion = DateTime.UtcNow;
    }

    public async Task MarcarListoAsync(int empresaId, int usuarioId, int detalleId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await db.ComandaDetalles
            .Where(x => x.Id == detalleId && x.Comanda!.EmpresaId == empresaId && x.Estado == "EN_PREPARACION" && x.RequierePreparacion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, "LISTO")
                .SetProperty(x => x.UsuarioListoId, usuarioId)
                .SetProperty(x => x.FechaListo, now), cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException("El producto no esta en preparacion o ya cambio de estado.");
        }

        await RegistrarEventoYRecalcularAsync(detalleId, "EN_PREPARACION", "LISTO", usuarioId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DespacharAsync(int empresaId, int usuarioId, int detalleId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await db.ComandaDetalles
            .Where(x => x.Id == detalleId && x.Comanda!.EmpresaId == empresaId &&
                ((x.RequierePreparacion && x.Estado == "LISTO") || (!x.RequierePreparacion && x.Estado == "PENDIENTE")))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, "DESPACHADO")
                .SetProperty(x => x.UsuarioDespachoId, usuarioId)
                .SetProperty(x => x.FechaDespacho, now), cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException("El producto no esta listo para despachar o ya cambio de estado.");
        }

        var requierePreparacion = await db.ComandaDetalles.AsNoTracking()
            .Where(x => x.Id == detalleId)
            .Select(x => x.RequierePreparacion)
            .FirstAsync(cancellationToken);

        await RegistrarEventoYRecalcularAsync(detalleId, requierePreparacion ? "LISTO" : "PENDIENTE", "DESPACHADO", usuarioId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task EntregarAsync(int empresaId, int meseroId, int detalleId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await db.ComandaDetalles
            .Where(x => x.Id == detalleId &&
                x.Comanda!.EmpresaId == empresaId &&
                x.Comanda.MeseroId == meseroId &&
                x.Estado == "DESPACHADO")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, "ENTREGADO")
                .SetProperty(x => x.UsuarioEntregaId, meseroId)
                .SetProperty(x => x.FechaEntrega, now), cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException("El producto no esta despachado o ya fue entregado.");
        }

        await RegistrarEventoYRecalcularAsync(detalleId, "DESPACHADO", "ENTREGADO", meseroId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CancelarPorCuentaItemAsync(int cuentaItemId, int usuarioId, CancellationToken cancellationToken)
    {
        var detalle = await db.ComandaDetalles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CuentaItemId == cuentaItemId, cancellationToken);

        if (detalle is null || detalle.Estado == "CANCELADO")
        {
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.ComandaDetalles
            .Where(x => x.Id == detalle.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Estado, "CANCELADO"), cancellationToken);

        await RegistrarEventoYRecalcularAsync(detalle.Id, detalle.Estado, "CANCELADO", usuarioId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RegistrarEventoYRecalcularAsync(int detalleId, string estadoAnterior, string estadoNuevo, int usuarioId, CancellationToken cancellationToken)
    {
        db.ComandaDetalleEventos.Add(new ComandaDetalleEvento
        {
            ComandaDetalleId = detalleId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            UsuarioId = usuarioId
        });
        await db.SaveChangesAsync(cancellationToken);

        var comandaId = await db.ComandaDetalles.AsNoTracking()
            .Where(x => x.Id == detalleId)
            .Select(x => x.ComandaId)
            .FirstAsync(cancellationToken);

        var comandaInfo = await db.Comandas.AsNoTracking()
            .Where(x => x.Id == comandaId)
            .Select(x => new { x.Id, x.EmpresaId, x.MeseroId, x.CuentaId, TokenPublico = x.Cuenta!.TokenPublico })
            .FirstAsync(cancellationToken);

        var estados = await db.ComandaDetalles.AsNoTracking()
            .Where(x => x.ComandaId == comandaInfo.Id)
            .Select(x => x.Estado)
            .ToListAsync(cancellationToken);

        string nuevoEstadoComanda;
        if (estados.Contains("PENDIENTE")) { nuevoEstadoComanda = "PENDIENTE"; }
        else if (estados.Contains("EN_PREPARACION")) { nuevoEstadoComanda = "EN_PREPARACION"; }
        else if (estados.Contains("LISTO")) { nuevoEstadoComanda = "LISTO"; }
        else if (estados.Contains("DESPACHADO")) { nuevoEstadoComanda = "DESPACHADO"; }
        else if (estados.All(x => x == "CANCELADO")) { nuevoEstadoComanda = "CANCELADO"; }
        else { nuevoEstadoComanda = "ENTREGADO"; }

        await db.Comandas
            .Where(x => x.Id == comandaInfo.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, nuevoEstadoComanda)
                .SetProperty(x => x.FechaModificacion, DateTime.UtcNow), cancellationToken);

        var payload = new { comandaId = comandaInfo.Id, cuentaId = comandaInfo.CuentaId, detalleId, estado = estadoNuevo };
        await hub.Clients.Group(ComandasHub.PreparacionGroup(comandaInfo.EmpresaId)).SendAsync("ComandaActualizada", payload, cancellationToken);
        await hub.Clients.Group(ComandasHub.UsuarioGroup(comandaInfo.MeseroId)).SendAsync("ComandaActualizada", payload, cancellationToken);

        if (!string.IsNullOrEmpty(comandaInfo.TokenPublico))
        {
            await publicHub.Clients.Group(SeguimientoPublicoHub.PublicGroup(comandaInfo.TokenPublico))
                .SendAsync("ConsumoActualizado", cancellationToken);
        }
    }

    private static PreparacionDetalleDto ToPreparacionDto(ComandaDetalle d) => new(
        d.Id,
        d.ComandaId,
        d.Comanda!.Numero,
        d.Comanda.CuentaId,
        d.Comanda.Cuenta?.Numero ?? string.Empty,
        d.Comanda.Cuenta?.Mesa,
        d.Comanda.Mesero?.NombreCompleto ?? "Mesero",
        d.ProductoId,
        d.ProductoNombre,
        d.Cantidad,
        d.RequierePreparacion,
        d.Estado,
        d.Observacion,
        d.AreaPreparacionId,
        d.AreaPreparacion?.Nombre,
        d.FechaCreacion,
        d.FechaToma,
        d.UsuarioToma?.NombreCompleto,
        d.FechaListo,
        d.UsuarioListo?.NombreCompleto);

    private async Task<string> NextNumero(int empresaId, CancellationToken cancellationToken)
    {
        var prefix = $"CMD-{DateTime.UtcNow:yyyyMMdd}";
        var count = await db.Comandas.CountAsync(x => x.EmpresaId == empresaId && x.Numero.StartsWith(prefix), cancellationToken) + 1;
        return $"{prefix}-{count:000}";
    }

    private static void Recalcular(Cuenta cuenta)
    {
        cuenta.Subtotal = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Cantidad * x.PrecioUnitario);
        cuenta.Descuento = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Descuento);
        cuenta.Total = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Total);
        cuenta.FechaModificacion = DateTime.UtcNow;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ComandaDto ToDto(Comanda comanda, string? meseroNombre) => new(
        comanda.Id,
        comanda.CuentaId,
        comanda.Numero,
        comanda.Estado,
        comanda.MeseroId,
        meseroNombre ?? comanda.Mesero?.NombreCompleto ?? "Mesero",
        comanda.FechaCreacion,
        comanda.Detalles.OrderBy(x => x.Id).Select(ToDetalleDto).ToList());

    private static ComandaDetalleDto ToDetalleDto(ComandaDetalle d) => new(
        d.Id,
        d.CuentaItemId,
        d.ProductoId,
        d.ProductoNombre,
        d.AreaPreparacionId,
        d.AreaPreparacion?.Nombre,
        d.Cantidad,
        d.RequierePreparacion,
        d.Estado,
        d.Observacion,
        d.UsuarioTomaId,
        d.UsuarioToma?.NombreCompleto,
        d.FechaToma,
        d.UsuarioListoId,
        d.UsuarioListo?.NombreCompleto,
        d.FechaListo,
        d.UsuarioDespachoId,
        d.UsuarioDespacho?.NombreCompleto,
        d.FechaDespacho,
        d.UsuarioEntregaId,
        d.UsuarioEntrega?.NombreCompleto,
        d.FechaEntrega,
        d.FechaCreacion);
}
