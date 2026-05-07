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
        var roleExists = await db.Roles.AsNoTracking().AnyAsync(x => x.Id == rolId, cancellationToken);
        if (!roleExists)
        {
            return NotFound();
        }

        var permissionTemplates = await db.RolPermisos.AsNoTracking()
            .Include(x => x.Permiso)
            .Include(x => x.Ventana!).ThenInclude(x => x.Modulo)
            .Where(x => x.Permiso != null && x.Permiso.Estado && x.Ventana != null && x.Ventana.Estado && x.Ventana.Modulo != null && x.Ventana.Modulo.Estado)
            .Select(x => new
            {
                x.PermisoId,
                x.VentanaId,
                Modulo = x.Ventana!.Modulo!.Nombre,
                ModuloOrden = x.Ventana.Modulo.Orden,
                Ventana = x.Ventana.Nombre,
                VentanaOrden = x.Ventana.Orden,
                Permiso = x.Permiso!.Nombre,
                PermisoCodigo = x.Permiso.Codigo
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var assigned = await db.RolPermisos.AsNoTracking()
            .Where(x => x.RolId == rolId)
            .ToDictionaryAsync(x => $"{x.VentanaId}-{x.PermisoId}", cancellationToken);

        var data = permissionTemplates
            .OrderBy(x => x.ModuloOrden)
            .ThenBy(x => x.VentanaOrden)
            .ThenBy(x => x.PermisoCodigo)
            .Select(x =>
            {
                assigned.TryGetValue($"{x.VentanaId}-{x.PermisoId}", out var saved);
                return new RolPermisoDto(
                    saved?.Id ?? 0,
                    rolId,
                    x.PermisoId,
                    x.VentanaId,
                    x.Modulo,
                    x.Ventana,
                    x.Permiso,
                    x.PermisoCodigo,
                    GetPermissionAction(x.PermisoCodigo),
                    saved?.PuedeVer == true,
                    saved?.PuedeCrear == true,
                    saved?.PuedeConsultar == true,
                    saved?.PuedeEditar == true,
                    saved?.PuedeEliminar == true);
            })
            .ToList();
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

    private static string GetPermissionAction(string code)
    {
        var action = code.Split('.').LastOrDefault() ?? string.Empty;
        return action switch
        {
            "Ver" => "Ver",
            "Crear" => "Crear",
            "Consultar" => "Consultar",
            "Editar" => "Editar",
            "Eliminar" => "Eliminar",
            _ => action
        };
    }
}
