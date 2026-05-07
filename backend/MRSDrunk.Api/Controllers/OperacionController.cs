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
[Route("api/[controller]")]
[Authorize]
public sealed class OperacionController(MrsDrunkDbContext db, IInventarioService inventarioService) : ControllerBase
{
    [HttpGet("cuentas/mias")]
    [RequirePermission("Operacion.Cuentas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<CuentaDto>>> MisCuentas(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var usuarioId = User.GetUsuarioId();
        var cuentas = await BaseCuentasQuery()
            .Where(x => x.EmpresaId == empresaId && x.MeseroId == usuarioId && x.Estado != "Anulada")
            .OrderByDescending(x => x.FechaApertura)
            .Take(80)
            .ToListAsync(cancellationToken);

        var data = cuentas.Select(ToDto).ToList();
        return Ok(data);
    }

    [HttpPost("cuentas")]
    [RequirePermission("Operacion.Cuentas.Crear")]
    public async Task<ActionResult<CuentaDto>> CrearCuenta(CrearCuentaRequest request, CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var entity = new Cuenta
        {
            EmpresaId = empresaId,
            SucursalId = User.GetSucursalId(),
            MeseroId = User.GetUsuarioId(),
            Numero = await NextNumero(empresaId, cancellationToken),
            Mesa = Clean(request.Mesa),
            Cliente = Clean(request.Cliente),
            Observacion = Clean(request.Observacion),
            Estado = "Abierta"
        };

        db.Cuentas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        entity.Mesero = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.MeseroId, cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPost("cuentas/{cuentaId:int}/items")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<IActionResult> AgregarItem(int cuentaId, AgregarCuentaItemRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaPropiaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        var producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ProductoId && x.EmpresaId == User.GetEmpresaId() && x.Estado, cancellationToken);
        if (producto is null)
        {
            return BadRequest(new { message = "El producto no existe o esta inactivo." });
        }

        if (request.Cantidad <= 0)
        {
            return BadRequest(new { message = "La cantidad debe ser mayor que cero." });
        }

        var precio = request.PrecioUnitario ?? producto.PrecioVenta;
        var item = new CuentaItem
        {
            CuentaId = cuenta.Id,
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            Cantidad = request.Cantidad,
            PrecioUnitario = precio,
            Descuento = request.Descuento,
            Total = Math.Max(0, request.Cantidad * precio - request.Descuento),
            UsuarioCreacionId = User.GetUsuarioId()
        };

        db.CuentaItems.Add(item);
        cuenta.Items.Add(item);
        Recalcular(cuenta);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("cuentas/{cuentaId:int}/items/{itemId:int}")]
    [RequirePermission("Operacion.Cuentas.Eliminar")]
    public async Task<IActionResult> EliminarItem(int cuentaId, int itemId, EliminarCuentaItemRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaPropiaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        var config = await GetConfiguracion(cancellationToken);
        if (!config.PermiteEliminarItems)
        {
            return BadRequest(new { message = "La configuracion actual no permite eliminar items." });
        }

        if (config.RequiereMotivoEliminarItem && string.IsNullOrWhiteSpace(request.Motivo))
        {
            return BadRequest(new { message = "Debe indicar el motivo de eliminacion." });
        }

        var item = cuenta.Items.FirstOrDefault(x => x.Id == itemId && !x.Eliminado);
        if (item is null)
        {
            return NotFound();
        }

        item.Eliminado = true;
        item.MotivoEliminacion = Clean(request.Motivo);
        item.UsuarioEliminacionId = User.GetUsuarioId();
        item.FechaEliminacion = DateTime.UtcNow;
        Recalcular(cuenta);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("cuentas/{cuentaId:int}/dividir")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<IActionResult> DividirCuenta(int cuentaId, DividirCuentaRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaPropiaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        var config = await GetConfiguracion(cancellationToken);
        if (!config.PermiteDividirCuenta)
        {
            return BadRequest(new { message = "La configuracion actual no permite dividir cuentas." });
        }

        cuenta.Dividida = request.Dividida;
        cuenta.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("cuentas/{cuentaId:int}/pagos")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<IActionResult> RegistrarPago(int cuentaId, RegistrarPagoRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaPropiaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        if (request.Valor <= 0)
        {
            return BadRequest(new { message = "El valor del pago debe ser mayor que cero." });
        }

        if (request.ValorPropina < 0)
        {
            return BadRequest(new { message = "La propina no puede ser negativa." });
        }

        if (request.ValorPropina > request.Valor)
        {
            return BadRequest(new { message = "La propina no puede ser mayor que el valor recibido." });
        }

        db.CuentaPagos.Add(new CuentaPago
        {
            CuentaId = cuenta.Id,
            MetodoPago = string.IsNullOrWhiteSpace(request.MetodoPago) ? "Efectivo" : request.MetodoPago.Trim(),
            Valor = request.Valor,
            IncluyePropina = request.IncluyePropina || request.ValorPropina > 0,
            ValorPropina = request.IncluyePropina || request.ValorPropina > 0 ? request.ValorPropina : 0,
            Referencia = Clean(request.Referencia),
            UsuarioRegistroId = User.GetUsuarioId()
        });
        cuenta.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("cuentas/{cuentaId:int}/pagos/{pagoId:int}")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<IActionResult> EliminarPago(int cuentaId, int pagoId, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaPropiaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        var pago = await db.CuentaPagos.FirstOrDefaultAsync(x => x.Id == pagoId && x.CuentaId == cuenta.Id, cancellationToken);
        if (pago is null)
        {
            return NotFound();
        }

        db.CuentaPagos.Remove(pago);
        cuenta.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("cuentas/{cuentaId:int}/solicitar-cierre")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<IActionResult> SolicitarCierre(int cuentaId, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaPropiaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        Recalcular(cuenta);
        var config = await GetConfiguracion(cancellationToken);
        if (config.RequiereAprobacionCierre)
        {
            cuenta.Estado = "PendienteAprobacion";
            cuenta.FechaSolicitudCierre = DateTime.UtcNow;
        }
        else
        {
            cuenta.Estado = "Cerrada";
            cuenta.FechaCierre = DateTime.UtcNow;
            await inventarioService.AplicarSalidaVentaAsync(cuenta, User.GetUsuarioId(), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("balance-dia")]
    [RequirePermission("Operacion.Balance.Ver")]
    public async Task<ActionResult<BalanceMeseroDto>> BalanceDia(CancellationToken cancellationToken)
    {
        var usuarioId = User.GetUsuarioId();
        var desde = DateTime.UtcNow.Date;
        var cuentas = await db.Cuentas.AsNoTracking()
            .Include(x => x.Pagos)
            .Include(x => x.Mesero)
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.MeseroId == usuarioId && x.FechaApertura >= desde)
            .ToListAsync(cancellationToken);

        var mesero = cuentas.FirstOrDefault()?.Mesero?.NombreCompleto ?? User.Identity?.Name ?? "Mesero";
        return Ok(new BalanceMeseroDto(
            usuarioId,
            mesero,
            cuentas.Count(x => x.Estado == "Abierta" || x.Estado == "PendienteAprobacion"),
            cuentas.Count(x => x.Estado == "Cerrada"),
            cuentas.Where(x => x.Estado == "Cerrada").Sum(x => x.Total),
            cuentas.SelectMany(x => x.Pagos).Sum(x => x.Valor)));
    }

    private IQueryable<Cuenta> BaseCuentasQuery() => db.Cuentas.AsNoTracking()
        .Include(x => x.Mesero)
        .Include(x => x.Items)
        .Include(x => x.Pagos);

    private async Task<Cuenta?> GetCuentaPropiaEditable(int cuentaId, CancellationToken cancellationToken) =>
        await db.Cuentas
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == cuentaId &&
                x.EmpresaId == User.GetEmpresaId() &&
                x.MeseroId == User.GetUsuarioId() &&
                (x.Estado == "Abierta" || x.Estado == "Rechazada"),
                cancellationToken);

    private async Task<ConfiguracionVenta> GetConfiguracion(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var config = await db.ConfiguracionesVenta.FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);
        if (config is not null)
        {
            return config;
        }

        config = new ConfiguracionVenta { EmpresaId = empresaId };
        db.ConfiguracionesVenta.Add(config);
        await db.SaveChangesAsync(cancellationToken);
        return config;
    }

    private async Task<string> NextNumero(int empresaId, CancellationToken cancellationToken)
    {
        var prefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.Cuentas.CountAsync(x => x.EmpresaId == empresaId && x.Numero.StartsWith(prefix), cancellationToken) + 1;
        return $"{prefix}-{count:000}";
    }

    private static void Recalcular(Cuenta cuenta)
    {
        cuenta.Subtotal = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Cantidad * x.PrecioUnitario);
        cuenta.Descuento = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Descuento);
        cuenta.Total = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Total);
        cuenta.FechaModificacion = DateTime.UtcNow;
    }

    internal static CuentaDto ToDto(Cuenta x)
    {
        var activeItems = x.Items.Where(i => !i.Eliminado).ToList();
        var subtotal = activeItems.Sum(i => i.Cantidad * i.PrecioUnitario);
        var descuento = activeItems.Sum(i => i.Descuento);
        var total = activeItems.Sum(i => i.Total);
        var pagos = x.Pagos.OrderBy(i => i.Id)
            .Select(i =>
            {
                var valorPropina = i.IncluyePropina ? i.ValorPropina : 0;
                return new CuentaPagoDto(
                    i.Id,
                    i.MetodoPago,
                    i.Valor,
                    i.IncluyePropina,
                    valorPropina,
                    Math.Max(0, i.Valor - valorPropina),
                    i.Referencia,
                    i.FechaPago);
            })
            .ToList();

        var totalPagado = pagos.Sum(i => i.Valor);
        var totalPropina = pagos.Sum(i => i.ValorPropina);
        var totalAplicadoCuenta = pagos.Sum(i => i.ValorAplicadoCuenta);
        return new CuentaDto(
            x.Id,
            x.Numero,
            x.Mesa,
            x.Cliente,
            x.Estado,
            x.Dividida,
            x.MeseroId,
            x.Mesero?.NombreCompleto ?? "Mesero",
            x.FechaApertura,
            x.FechaSolicitudCierre,
            x.FechaCierre,
            subtotal,
            descuento,
            total,
            totalPagado,
            totalPropina,
            totalAplicadoCuenta,
            Math.Max(0, total - totalAplicadoCuenta),
            Math.Max(0, totalAplicadoCuenta - total),
            x.Items.OrderBy(i => i.Id).Select(i => new CuentaItemDto(i.Id, i.ProductoId, i.ProductoNombre, i.Cantidad, i.PrecioUnitario, i.Descuento, i.Total, i.Eliminado, i.MotivoEliminacion)).ToList(),
            pagos);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
