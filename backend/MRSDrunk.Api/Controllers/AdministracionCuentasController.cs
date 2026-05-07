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
[Route("api/administracion-cuentas")]
[Authorize]
public sealed class AdministracionCuentasController(MrsDrunkDbContext db, IInventarioService inventarioService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("AdministracionCuentas.Cuentas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<CuentaDto>>> Get(CancellationToken cancellationToken)
    {
        var cuentas = await db.Cuentas.AsNoTracking()
            .Include(x => x.Mesero)
            .Include(x => x.Items)
            .Include(x => x.Pagos)
            .Where(x => x.EmpresaId == User.GetEmpresaId())
            .OrderByDescending(x => x.FechaApertura)
            .Take(120)
            .ToListAsync(cancellationToken);

        var data = cuentas.Select(OperacionController.ToDto).ToList();
        return Ok(data);
    }

    [HttpGet("usuarios-cuentas")]
    [RequirePermission("AdministracionCuentas.Usuarios.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<CuentaDto>>> UsuariosCuentas(CancellationToken cancellationToken)
    {
        var cuentas = await db.Cuentas.AsNoTracking()
            .Include(x => x.Mesero)
            .Include(x => x.Items)
            .Include(x => x.Pagos)
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.Estado != "Anulada")
            .OrderByDescending(x => x.FechaApertura)
            .Take(200)
            .ToListAsync(cancellationToken);

        var data = cuentas.Select(OperacionController.ToDto).ToList();
        return Ok(data);
    }

    [HttpPost("usuarios-cuentas/{cuentaId:int}/items")]
    [RequirePermission("AdministracionCuentas.Usuarios.Editar")]
    public async Task<IActionResult> AgregarItemUsuario(int cuentaId, AgregarCuentaItemRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaEditable(cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        if (request.Cantidad <= 0)
        {
            return BadRequest(new { message = "La cantidad debe ser mayor que cero." });
        }

        var producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == request.ProductoId &&
            x.EmpresaId == User.GetEmpresaId() &&
            x.Estado,
            cancellationToken);
        if (producto is null)
        {
            return BadRequest(new { message = "El producto no existe o esta inactivo." });
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

    [HttpDelete("usuarios-cuentas/{cuentaId:int}/items/{itemId:int}")]
    [RequirePermission("AdministracionCuentas.Usuarios.Eliminar")]
    public async Task<IActionResult> EliminarItemUsuario(int cuentaId, int itemId, EliminarCuentaItemRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaEditable(cuentaId, cancellationToken);
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

    [HttpPost("usuarios-cuentas/{cuentaId:int}/pagos")]
    [RequirePermission("AdministracionCuentas.Usuarios.Editar")]
    public async Task<IActionResult> RegistrarPagoUsuario(int cuentaId, RegistrarPagoRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaEditable(cuentaId, cancellationToken);
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

    [HttpDelete("usuarios-cuentas/{cuentaId:int}/pagos/{pagoId:int}")]
    [RequirePermission("AdministracionCuentas.Usuarios.Editar")]
    public async Task<IActionResult> EliminarPagoUsuario(int cuentaId, int pagoId, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaEditable(cuentaId, cancellationToken);
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

    [HttpPost("usuarios-cuentas/{cuentaId:int}/dividir")]
    [RequirePermission("AdministracionCuentas.Usuarios.Editar")]
    public async Task<IActionResult> DividirCuentaUsuario(int cuentaId, DividirCuentaRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await GetCuentaEditable(cuentaId, cancellationToken);
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

    [HttpPost("{cuentaId:int}/resolver-cierre")]
    [RequirePermission("AdministracionCuentas.Cuentas.Editar")]
    public async Task<IActionResult> ResolverCierre(int cuentaId, ResolverCierreCuentaRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await db.Cuentas
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cuentaId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        if (cuenta.Estado != "PendienteAprobacion")
        {
            return BadRequest(new { message = "La cuenta no esta pendiente de aprobacion." });
        }

        cuenta.Estado = request.Aprobar ? "Cerrada" : "Rechazada";
        cuenta.AdministradorCierreId = User.GetUsuarioId();
        cuenta.FechaCierre = request.Aprobar ? DateTime.UtcNow : null;
        cuenta.MotivoRechazo = request.Aprobar ? null : request.Motivo;
        cuenta.FechaModificacion = DateTime.UtcNow;
        if (request.Aprobar)
        {
            try
            {
                await inventarioService.AplicarSalidaVentaAsync(cuenta, User.GetUsuarioId(), cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{cuentaId:int}/anular")]
    [RequirePermission("AdministracionCuentas.Cuentas.Eliminar")]
    public async Task<IActionResult> Anular(int cuentaId, AnularCuentaRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await db.Cuentas.FirstOrDefaultAsync(x => x.Id == cuentaId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (cuenta is null)
        {
            return NotFound();
        }

        cuenta.Estado = "Anulada";
        cuenta.MotivoAnulacion = request.Motivo;
        cuenta.AdministradorCierreId = User.GetUsuarioId();
        cuenta.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("balance-meseros")]
    [RequirePermission("AdministracionCuentas.Balance.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<BalanceMeseroDto>>> BalanceMeseros(CancellationToken cancellationToken)
    {
        var desde = DateTime.UtcNow.Date;
        var cuentas = await db.Cuentas.AsNoTracking()
            .Include(x => x.Mesero)
            .Include(x => x.Pagos)
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.FechaApertura >= desde)
            .ToListAsync(cancellationToken);

        var data = cuentas
            .GroupBy(x => new { x.MeseroId, Mesero = x.Mesero?.NombreCompleto ?? "Mesero" })
            .Select(g => new BalanceMeseroDto(
                g.Key.MeseroId,
                g.Key.Mesero,
                g.Count(x => x.Estado == "Abierta" || x.Estado == "PendienteAprobacion"),
                g.Count(x => x.Estado == "Cerrada"),
                g.Where(x => x.Estado == "Cerrada").Sum(x => x.Total),
                g.SelectMany(x => x.Pagos).Sum(x => x.Valor)))
            .OrderBy(x => x.Mesero)
            .ToList();

        return Ok(data);
    }

    [HttpGet("balance-general-dia")]
    [RequirePermission("AdministracionCuentas.Balance.Ver")]
    public async Task<ActionResult<BalanceDiaDto>> BalanceGeneralDia(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var turno = await db.CajaTurnos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && x.Estado == "Abierta")
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(cancellationToken);
        var desde = turno?.FechaApertura ?? DateTime.UtcNow.Date;

        var cuentas = await db.Cuentas.AsNoTracking()
            .Include(x => x.Mesero)
            .Include(x => x.Items)
            .Include(x => x.Pagos)
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId &&
                x.FechaApertura >= desde &&
                x.Estado != "Anulada")
            .OrderByDescending(x => x.FechaApertura)
            .ToListAsync(cancellationToken);

        var pagos = cuentas.SelectMany(x => x.Pagos).ToList();
        var pagosPorMetodo = pagos
            .GroupBy(x => string.IsNullOrWhiteSpace(x.MetodoPago) ? "Sin metodo" : x.MetodoPago)
            .Select(g => new CajaMetodoPagoDto(g.Key, g.Sum(x => x.Valor), g.Count()))
            .OrderBy(x => x.MetodoPago)
            .ToList();
        var productos = cuentas
            .SelectMany(x => x.Items)
            .Where(x => !x.Eliminado)
            .GroupBy(x => x.ProductoNombre)
            .Select(g => new BalanceDiaProductoDto(g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Total)))
            .OrderByDescending(x => x.Total)
            .ToList();
        var cuentaDtos = cuentas.Select(OperacionController.ToDto).ToList();

        return Ok(new BalanceDiaDto(
            0,
            "General",
            cuentas.Count(x => x.Estado == "Abierta" || x.Estado == "PendienteAprobacion"),
            cuentas.Count(x => x.Estado == "Cerrada"),
            cuentas.Count(x => x.Estado == "PendienteAprobacion"),
            cuentas.Count(x => x.Estado == "Rechazada"),
            cuentas.Where(x => x.Estado == "Cerrada").Sum(x => x.Total),
            pagos.Sum(x => x.Valor),
            pagos.Where(x => x.IncluyePropina).Sum(x => x.ValorPropina),
            cuentaDtos.Sum(x => x.SaldoPendiente),
            pagosPorMetodo,
            productos,
            cuentaDtos));
    }

    private async Task<Cuenta?> GetCuentaEditable(int cuentaId, CancellationToken cancellationToken) =>
        await db.Cuentas
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == cuentaId &&
                x.EmpresaId == User.GetEmpresaId() &&
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

    private static void Recalcular(Cuenta cuenta)
    {
        cuenta.Subtotal = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Cantidad * x.PrecioUnitario);
        cuenta.Descuento = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Descuento);
        cuenta.Total = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Total);
        cuenta.FechaModificacion = DateTime.UtcNow;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
