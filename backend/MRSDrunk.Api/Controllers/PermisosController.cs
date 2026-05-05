using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PermisosController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Seguridad.Permisos.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<PermisoDto>>> Get(CancellationToken cancellationToken)
    {
        var data = await db.Permisos.AsNoTracking()
            .OrderBy(x => x.Codigo)
            .Select(x => new PermisoDto(x.Id, x.Codigo, x.Nombre, x.Descripcion, x.Estado))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("rol/{rolId:int}")]
    [RequirePermission("Seguridad.Permisos.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<RolPermisoDto>>> GetByRol(int rolId, CancellationToken cancellationToken)
    {
        var data = await db.RolPermisos.AsNoTracking()
            .Include(x => x.Permiso)
            .Include(x => x.Ventana!).ThenInclude(x => x.Modulo)
            .Where(x => x.RolId == rolId)
            .OrderBy(x => x.Ventana!.Modulo!.Orden)
            .ThenBy(x => x.Ventana!.Orden)
            .Select(x => new RolPermisoDto(
                x.Id,
                x.RolId,
                x.PermisoId,
                x.VentanaId,
                x.Ventana!.Modulo!.Nombre,
                x.Ventana.Nombre,
                x.Permiso!.Nombre,
                x.PuedeVer,
                x.PuedeCrear,
                x.PuedeConsultar,
                x.PuedeEditar,
                x.PuedeEliminar))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpPost("asignar")]
    [RequirePermission("Seguridad.Permisos.Editar")]
    public async Task<IActionResult> Asignar(int rolId, AsignarPermisoRequest request, CancellationToken cancellationToken)
    {
        return await UpsertRolPermiso(rolId, request, cancellationToken);
    }

    [HttpPut("rol/{rolId:int}")]
    [RequirePermission("Seguridad.Permisos.Editar")]
    public async Task<IActionResult> PutRol(int rolId, GuardarPermisosRolRequest request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rolId, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        if (role.EsSuperUsuario)
        {
            return BadRequest(new { message = "El SuperUsuario siempre tiene todos los permisos." });
        }

        foreach (var item in request.Permisos)
        {
            await UpsertRolPermiso(rolId, item, cancellationToken, save: false);
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> UpsertRolPermiso(int rolId, AsignarPermisoRequest request, CancellationToken cancellationToken, bool save = true)
    {
        var entity = await db.RolPermisos.FirstOrDefaultAsync(x =>
            x.RolId == rolId &&
            x.PermisoId == request.PermisoId &&
            x.VentanaId == request.VentanaId,
            cancellationToken);

        if (entity is null)
        {
            entity = new RolPermiso { RolId = rolId, PermisoId = request.PermisoId, VentanaId = request.VentanaId };
            db.RolPermisos.Add(entity);
        }

        entity.PuedeVer = request.PuedeVer;
        entity.PuedeCrear = request.PuedeCrear;
        entity.PuedeConsultar = request.PuedeConsultar;
        entity.PuedeEditar = request.PuedeEditar;
        entity.PuedeEliminar = request.PuedeEliminar;

        if (save)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }
}
