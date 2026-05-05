using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/administracion-cuentas")]
[Authorize]
public sealed class AdministracionCuentasController(MrsDrunkDbContext db) : ControllerBase
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

    [HttpPost("{cuentaId:int}/resolver-cierre")]
    [RequirePermission("AdministracionCuentas.Cuentas.Editar")]
    public async Task<IActionResult> ResolverCierre(int cuentaId, ResolverCierreCuentaRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await db.Cuentas.FirstOrDefaultAsync(x => x.Id == cuentaId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
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
}
