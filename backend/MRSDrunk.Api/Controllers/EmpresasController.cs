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
public sealed class EmpresasController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Configuracion.Empresas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<EmpresaDto>>> Get(CancellationToken cancellationToken)
    {
        var data = await db.Empresas.AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new EmpresaDto(x.Id, x.Nombre, x.Nit, x.LogoUrl, x.Estado))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpPost]
    [RequirePermission("Configuracion.Empresas.Crear")]
    public async Task<ActionResult<EmpresaDto>> Post(UpsertEmpresaRequest request, CancellationToken cancellationToken)
    {
        var entity = new Empresa { Nombre = request.Nombre, Nit = request.Nit, LogoUrl = request.LogoUrl, Estado = request.Estado };
        db.Empresas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, new EmpresaDto(entity.Id, entity.Nombre, entity.Nit, entity.LogoUrl, entity.Estado));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Configuracion.Empresas.Editar")]
    public async Task<IActionResult> Put(int id, UpsertEmpresaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Empresas.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Nombre = request.Nombre;
        entity.Nit = request.Nit;
        entity.LogoUrl = request.LogoUrl;
        entity.Estado = request.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
