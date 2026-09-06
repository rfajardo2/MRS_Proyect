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
public sealed class AuditoriaController(MrsDrunkDbContext db) : ControllerBase
{
    private const int MaxRegistros = 200;

    [HttpGet]
    [RequirePermission("Auditoria.Registros.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<RegistroAuditoriaDto>>> Get(
        [FromQuery] string? entidad,
        [FromQuery] int? usuarioId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var query = db.RegistrosAuditoria.AsNoTracking()
            .Include(x => x.Usuario)
            .Where(x => x.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(entidad))
        {
            query = query.Where(x => x.Entidad == entidad);
        }

        if (usuarioId.HasValue)
        {
            query = query.Where(x => x.UsuarioId == usuarioId.Value);
        }

        if (desde.HasValue)
        {
            query = query.Where(x => x.FechaCreacion >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(x => x.FechaCreacion <= hasta.Value);
        }

        var data = await query
            .OrderByDescending(x => x.FechaCreacion)
            .Take(MaxRegistros)
            .Select(x => new RegistroAuditoriaDto(
                x.Id,
                x.UsuarioId,
                x.Usuario != null ? x.Usuario.NombreCompleto : null,
                x.Entidad,
                x.EntidadId,
                x.Accion,
                x.Detalle,
                x.FechaCreacion))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("entidades")]
    [RequirePermission("Auditoria.Registros.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetEntidades(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var data = await db.RegistrosAuditoria.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => x.Entidad)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
