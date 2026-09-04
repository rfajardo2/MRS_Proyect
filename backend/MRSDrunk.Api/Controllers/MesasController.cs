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
public sealed class MesasController(MrsDrunkDbContext db) : ControllerBase
{
    private static readonly string[] EstadosValidos = ["Libre", "Ocupada", "Reservada", "FueraDeServicio"];
    private const int MaxNombre = 40;

    [HttpGet]
    [RequirePermission("Reservas.Mesas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<MesaDto>>> Get(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var data = await db.Mesas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Nombre)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost]
    [RequirePermission("Reservas.Mesas.Crear")]
    public async Task<ActionResult<MesaDto>> Post(UpsertMesaRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateMesa(null, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var entity = new Mesa
        {
            EmpresaId = User.GetEmpresaId(),
            SucursalId = User.GetSucursalId(),
            Nombre = request.Nombre.Trim(),
            Capacidad = request.Capacidad,
            PosicionX = request.PosicionX,
            PosicionY = request.PosicionY,
            Activa = request.Activa
        };

        db.Mesas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Reservas.Mesas.Editar")]
    public async Task<IActionResult> Put(int id, UpsertMesaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Mesas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var validation = await ValidateMesa(id, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        entity.Nombre = request.Nombre.Trim();
        entity.Capacidad = request.Capacidad;
        entity.PosicionX = request.PosicionX;
        entity.PosicionY = request.PosicionY;
        entity.Activa = request.Activa;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/estado")]
    [RequirePermission("Reservas.Mesas.Editar")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoMesaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Mesas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (!EstadosValidos.Contains(request.Estado))
        {
            return BadRequest(new { message = "Estado de mesa no valido." });
        }

        entity.Estado = request.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateMesa(int? id, UpsertMesaRequest request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return "El nombre de la mesa es obligatorio.";
        }

        if (nombre.Length > MaxNombre)
        {
            return $"El nombre de la mesa no puede superar {MaxNombre} caracteres.";
        }

        if (request.Capacidad <= 0)
        {
            return "La capacidad debe ser mayor que cero.";
        }

        var empresaId = User.GetEmpresaId();
        var nombreNormalizado = nombre.ToUpperInvariant();
        var duplicate = await db.Mesas.AsNoTracking().AnyAsync(x =>
            x.EmpresaId == empresaId &&
            x.Nombre.ToUpper() == nombreNormalizado &&
            (!id.HasValue || x.Id != id.Value),
            cancellationToken);

        return duplicate ? "Ya existe una mesa con ese nombre." : null;
    }

    private static MesaDto ToDto(Mesa x) => new(x.Id, x.Nombre, x.Capacidad, x.PosicionX, x.PosicionY, x.Estado, x.Activa);
}
