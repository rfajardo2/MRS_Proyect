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
public sealed class ReservasController(MrsDrunkDbContext db) : ControllerBase
{
    private static readonly string[] EstadosValidos = ["Pendiente", "Confirmada", "Cancelada", "Completada", "NoShow"];
    private static readonly string[] EstadosActivos = ["Pendiente", "Confirmada"];
    private const int MaxCliente = 120;
    private const int MaxObservacion = 250;

    [HttpGet]
    [RequirePermission("Reservas.Reservas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<ReservaDto>>> Get(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? mesaId,
        CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var query = db.Reservas.AsNoTracking()
            .Include(x => x.Mesa)
            .Where(x => x.EmpresaId == empresaId);

        if (desde.HasValue)
        {
            query = query.Where(x => x.FechaHora >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(x => x.FechaHora <= hasta.Value);
        }

        if (mesaId.HasValue)
        {
            query = query.Where(x => x.MesaId == mesaId.Value);
        }

        var data = await query
            .OrderBy(x => x.FechaHora)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost]
    [RequirePermission("Reservas.Reservas.Crear")]
    public async Task<ActionResult<ReservaDto>> Post(UpsertReservaRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateReserva(null, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var entity = new Reserva
        {
            EmpresaId = User.GetEmpresaId(),
            SucursalId = User.GetSucursalId(),
            MesaId = request.MesaId,
            Cliente = request.Cliente.Trim(),
            Telefono = Clean(request.Telefono),
            NumeroPersonas = request.NumeroPersonas,
            FechaHora = request.FechaHora,
            DuracionMinutos = request.DuracionMinutos,
            Observacion = Clean(request.Observacion),
            UsuarioCreacionId = User.GetUsuarioId()
        };

        db.Reservas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.Mesa).LoadAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Reservas.Reservas.Editar")]
    public async Task<IActionResult> Put(int id, UpsertReservaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Reservas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var validation = await ValidateReserva(id, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        entity.MesaId = request.MesaId;
        entity.Cliente = request.Cliente.Trim();
        entity.Telefono = Clean(request.Telefono);
        entity.NumeroPersonas = request.NumeroPersonas;
        entity.FechaHora = request.FechaHora;
        entity.DuracionMinutos = request.DuracionMinutos;
        entity.Observacion = Clean(request.Observacion);
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/estado")]
    [RequirePermission("Reservas.Reservas.Editar")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoReservaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Reservas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (!EstadosValidos.Contains(request.Estado))
        {
            return BadRequest(new { message = "Estado de reserva no valido." });
        }

        entity.Estado = request.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateReserva(int? id, UpsertReservaRequest request, CancellationToken cancellationToken)
    {
        var cliente = request.Cliente?.Trim();
        if (string.IsNullOrWhiteSpace(cliente))
        {
            return "El nombre del cliente es obligatorio.";
        }

        if (cliente.Length > MaxCliente)
        {
            return $"El nombre del cliente no puede superar {MaxCliente} caracteres.";
        }

        if (Clean(request.Observacion)?.Length > MaxObservacion)
        {
            return $"La observacion no puede superar {MaxObservacion} caracteres.";
        }

        if (request.NumeroPersonas <= 0)
        {
            return "El numero de personas debe ser mayor que cero.";
        }

        if (request.DuracionMinutos <= 0)
        {
            return "La duracion debe ser mayor que cero.";
        }

        var empresaId = User.GetEmpresaId();
        var mesaExists = await db.Mesas.AsNoTracking().AnyAsync(x => x.Id == request.MesaId && x.EmpresaId == empresaId, cancellationToken);
        if (!mesaExists)
        {
            return "La mesa no existe.";
        }

        var inicio = request.FechaHora;
        var fin = inicio.AddMinutes(request.DuracionMinutos);
        var solapada = await db.Reservas.AsNoTracking().AnyAsync(x =>
            x.MesaId == request.MesaId &&
            (!id.HasValue || x.Id != id.Value) &&
            EstadosActivos.Contains(x.Estado) &&
            x.FechaHora < fin &&
            inicio < x.FechaHora.AddMinutes(x.DuracionMinutos),
            cancellationToken);

        return solapada ? "La mesa ya tiene una reserva activa que se cruza con ese horario." : null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ReservaDto ToDto(Reserva x) => new(
        x.Id,
        x.MesaId,
        x.Mesa?.Nombre ?? string.Empty,
        x.Cliente,
        x.Telefono,
        x.NumeroPersonas,
        x.FechaHora,
        x.DuracionMinutos,
        x.Estado,
        x.Observacion,
        x.FechaCreacion);
}
