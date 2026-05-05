using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService, MrsDrunkDbContext db) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new { message = "Usuario o contrasena incorrectos." })
            : Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await authService.GetMeAsync(User.GetUsuarioId(), cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpGet("permissions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<string>>> Permissions(CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == User.GetRolId(), cancellationToken);
        if (role is null)
        {
            return Unauthorized();
        }

        if (role.EsSuperUsuario)
        {
            var all = await db.Permisos.AsNoTracking()
                .Where(x => x.Estado)
                .Select(x => x.Codigo)
                .ToListAsync(cancellationToken);
            return Ok(all);
        }

        var permissions = await db.RolPermisos.AsNoTracking()
            .Include(x => x.Permiso)
            .Where(x => x.RolId == role.Id && x.Permiso != null && x.Permiso.Estado)
            .Where(x =>
                x.PuedeVer ||
                x.PuedeCrear ||
                x.PuedeConsultar ||
                x.PuedeEditar ||
                x.PuedeEliminar)
            .Select(x => x.Permiso!.Codigo)
            .Distinct()
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public IActionResult RefreshToken()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Refresh token preparado para una siguiente iteracion." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirst("sessionId")?.Value;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await db.UsuarioSesiones.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
            if (session is not null)
            {
                session.Estado = false;
                session.FechaCierre = DateTime.UtcNow;
                session.CerradaPor = User.GetUsuarioId().ToString();
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return NoContent();
    }
}
