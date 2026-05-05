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
public sealed class UsuariosController(MrsDrunkDbContext db, IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Seguridad.Usuarios.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<UsuarioDto>>> Get(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var rol = User.GetNombreRol();
        var query = db.Usuarios.AsNoTracking().Include(x => x.Empresa).Include(x => x.Rol).AsQueryable();

        if (rol != "SuperUsuario")
        {
            query = query.Where(x => x.EmpresaId == empresaId);
        }

        var users = await query.OrderBy(x => x.NombreCompleto).Select(x => ToDto(x)).ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("Seguridad.Usuarios.Consultar")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await db.Usuarios
            .AsNoTracking()
            .Include(x => x.Empresa)
            .Include(x => x.Sucursal)
            .Include(x => x.Rol)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var permisos = await db.RolPermisos
            .AsNoTracking()
            .Include(x => x.Permiso)
            .Include(x => x.Ventana!).ThenInclude(x => x.Modulo)
            .Where(x => x.RolId == user.RolId)
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

        return Ok(new
        {
            Usuario = ToDto(user),
            Sucursal = user.Sucursal?.Nombre,
            Permisos = permisos
        });
    }

    [HttpPost]
    [RequirePermission("Seguridad.Usuarios.Crear")]
    public async Task<ActionResult<UsuarioDto>> Post(UpsertUsuarioRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "La contrasena es obligatoria para crear usuarios." });
        }

        var entity = new Usuario
        {
            EmpresaId = request.EmpresaId,
            SucursalId = request.SucursalId,
            NombreCompleto = request.NombreCompleto,
            UsuarioNombre = request.Usuario,
            Correo = request.Correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RolId = request.RolId,
            Estado = request.Estado,
            UsuarioCreacion = User.GetUsuarioId()
        };

        db.Usuarios.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.Empresa).LoadAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.Rol).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Seguridad.Usuarios.Editar")]
    public async Task<IActionResult> Put(int id, UpsertUsuarioRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Usuarios.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.EmpresaId = request.EmpresaId;
        entity.SucursalId = request.SucursalId;
        entity.NombreCompleto = request.NombreCompleto;
        entity.UsuarioNombre = request.Usuario;
        entity.Correo = request.Correo;
        entity.RolId = request.RolId;
        entity.Estado = request.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        entity.UsuarioModificacion = User.GetUsuarioId();

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var canChangePassword = await permissionService.HasPermissionAsync(
                User.GetUsuarioId(),
                User.GetRolId(),
                "Seguridad.Usuarios.CambiarPassword",
                cancellationToken);

            if (!canChangePassword)
            {
                return Forbid();
            }

            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("Seguridad.Usuarios.Eliminar")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await db.Usuarios.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Estado = !entity.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        entity.UsuarioModificacion = User.GetUsuarioId();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static UsuarioDto ToDto(Usuario x) => new(
        x.Id,
        x.EmpresaId,
        x.SucursalId,
        x.NombreCompleto,
        x.UsuarioNombre,
        x.Correo,
        x.RolId,
        x.Rol?.Nombre ?? string.Empty,
        x.Empresa?.Nombre ?? string.Empty,
        x.Estado,
        x.FechaCreacion);
}
