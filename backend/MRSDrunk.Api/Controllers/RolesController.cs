using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RolesController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Seguridad.Roles.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<RolDto>>> Get(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var rol = User.GetNombreRol();
        var query = db.Roles.AsNoTracking().AsQueryable();
        if (rol != "SuperUsuario")
        {
            query = query.Where(x => x.EmpresaId == empresaId || x.EmpresaId == null);
        }

        var data = await query.OrderBy(x => x.Nombre)
            .Select(x => new RolDto(x.Id, x.EmpresaId, x.Nombre, x.Descripcion, x.EsSuperUsuario, x.Estado))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("Seguridad.Roles.Ver")]
    public async Task<ActionResult<RolDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return role is null ? NotFound() : Ok(new RolDto(role.Id, role.EmpresaId, role.Nombre, role.Descripcion, role.EsSuperUsuario, role.Estado));
    }

    [HttpPost]
    [RequirePermission("Seguridad.Roles.Crear")]
    public async Task<ActionResult<RolDto>> Post(UpsertRolRequest request, CancellationToken cancellationToken)
    {
        var entity = new Rol { EmpresaId = request.EmpresaId, Nombre = request.Nombre, Descripcion = request.Descripcion, Estado = request.Estado };
        db.Roles.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new RolDto(entity.Id, entity.EmpresaId, entity.Nombre, entity.Descripcion, entity.EsSuperUsuario, entity.Estado));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Seguridad.Roles.Editar")]
    public async Task<IActionResult> Put(int id, UpsertRolRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Roles.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (entity.EsSuperUsuario)
        {
            return BadRequest(new { message = "El rol SuperUsuario no se edita desde esta pantalla." });
        }

        entity.EmpresaId = request.EmpresaId;
        entity.Nombre = request.Nombre;
        entity.Descripcion = request.Descripcion;
        entity.Estado = request.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("Seguridad.Roles.Eliminar")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await db.Roles.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (entity.EsSuperUsuario)
        {
            return BadRequest(new { message = "El rol SuperUsuario no puede inactivarse." });
        }

        entity.Estado = !entity.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
