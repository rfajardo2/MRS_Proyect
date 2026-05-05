using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Middleware;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class VentanasController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Seguridad.Permisos.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<VentanaDto>>> Get(CancellationToken cancellationToken)
    {
        var data = await db.Ventanas.AsNoTracking()
            .Include(x => x.Modulo)
            .Where(x => x.Estado && x.Modulo != null && x.Modulo.Estado)
            .OrderBy(x => x.Modulo!.Orden)
            .ThenBy(x => x.Orden)
            .Select(x => new VentanaDto(x.Id, x.ModuloId, x.Modulo!.Nombre, x.Nombre, x.Ruta, x.Icono, x.Orden, x.Estado))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
