using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SesionesController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Seguridad.Sesiones.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<SesionUsuarioDto>>> Get(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var currentSessionId = User.FindFirst("sessionId")?.Value;

        var data = await db.UsuarioSesiones.AsNoTracking()
            .Include(x => x.Usuario)!.ThenInclude(x => x!.Rol)
            .Include(x => x.Empresa)
            .Where(x => x.EmpresaId == empresaId && x.Estado && x.FechaExpiracion > DateTime.UtcNow)
            .OrderByDescending(x => x.UltimaActividad)
            .Select(x => new SesionUsuarioDto(
                x.Id,
                x.UsuarioId,
                x.Usuario!.NombreCompleto,
                x.Usuario.UsuarioNombre,
                x.Usuario.Rol!.Nombre,
                x.Empresa!.Nombre,
                x.IpAddress,
                x.UserAgent,
                x.FechaInicio,
                x.UltimaActividad,
                x.FechaExpiracion,
                x.SessionId == currentSessionId))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("resumen")]
    [RequirePermission("Seguridad.Sesiones.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<SesionResumenUsuarioDto>>> Resumen(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();

        var data = await db.UsuarioSesiones.AsNoTracking()
            .Include(x => x.Usuario)!.ThenInclude(x => x!.Rol)
            .Where(x => x.EmpresaId == empresaId && x.Estado && x.FechaExpiracion > DateTime.UtcNow)
            .GroupBy(x => new { x.UsuarioId, x.Usuario!.NombreCompleto, x.Usuario.UsuarioNombre, Rol = x.Usuario.Rol!.Nombre })
            .Select(g => new SesionResumenUsuarioDto(g.Key.UsuarioId, g.Key.NombreCompleto, g.Key.UsuarioNombre, g.Key.Rol, g.Count()))
            .OrderByDescending(x => x.SesionesActivas)
            .ThenBy(x => x.NombreCompleto)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost("{id:int}/cerrar")]
    [RequirePermission("Seguridad.Sesiones.Cerrar")]
    public async Task<IActionResult> Cerrar(int id, CancellationToken cancellationToken)
    {
        var currentSessionId = User.FindFirst("sessionId")?.Value;
        var session = await db.UsuarioSesiones.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        if (session.SessionId == currentSessionId)
        {
            return BadRequest(new { message = "No puedes cerrar tu sesion actual desde esta ventana." });
        }

        CloseSession(session, User.GetUsuarioId());
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("usuario/{usuarioId:int}/cerrar")]
    [RequirePermission("Seguridad.Sesiones.Cerrar")]
    public async Task<IActionResult> CerrarUsuario(int usuarioId, CancellationToken cancellationToken)
    {
        var currentSessionId = User.FindFirst("sessionId")?.Value;
        var sessions = await db.UsuarioSesiones
            .Where(x => x.UsuarioId == usuarioId && x.EmpresaId == User.GetEmpresaId() && x.Estado && x.SessionId != currentSessionId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            CloseSession(session, User.GetUsuarioId());
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("cerrar-todas")]
    [RequirePermission("Seguridad.Sesiones.CerrarTodas")]
    public async Task<IActionResult> CerrarTodas(CancellationToken cancellationToken)
    {
        var currentSessionId = User.FindFirst("sessionId")?.Value;
        var sessions = await db.UsuarioSesiones
            .Where(x => x.EmpresaId == User.GetEmpresaId() && x.Estado && x.SessionId != currentSessionId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            CloseSession(session, User.GetUsuarioId());
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static void CloseSession(Models.UsuarioSesion session, int closedBy)
    {
        session.Estado = false;
        session.FechaCierre = DateTime.UtcNow;
        session.CerradaPor = closedBy.ToString();
    }
}
