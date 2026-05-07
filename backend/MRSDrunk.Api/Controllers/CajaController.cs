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
[Route("api/caja")]
[Authorize]
public sealed class CajaController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet("turno/actual")]
    [RequirePermission("Operacion.Caja.Ver")]
    public async Task<ActionResult<CajaTurnoDto?>> Actual(CancellationToken cancellationToken)
    {
        var turno = await GetTurnoAbierto(cancellationToken);
        if (turno is null)
        {
            return Ok(null);
        }

        return Ok(await ToDto(turno, cancellationToken));
    }

    [HttpGet("turnos")]
    [RequirePermission("Operacion.Caja.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<CajaTurnoDto>>> Turnos(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var turnos = await db.CajaTurnos.AsNoTracking()
            .Include(x => x.UsuarioApertura)
            .Include(x => x.UsuarioCierre)
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId)
            .OrderByDescending(x => x.FechaApertura)
            .Take(30)
            .ToListAsync(cancellationToken);

        var data = new List<CajaTurnoDto>();
        foreach (var turno in turnos)
        {
            data.Add(await ToDto(turno, cancellationToken));
        }

        return Ok(data);
    }

    [HttpPost("turnos")]
    [RequirePermission("Operacion.Caja.Crear")]
    public async Task<ActionResult<CajaTurnoDto>> Abrir(AbrirCajaTurnoRequest request, CancellationToken cancellationToken)
    {
        if (request.SaldoInicial < 0)
        {
            return BadRequest(new { message = "El saldo inicial no puede ser negativo." });
        }

        var existente = await GetTurnoAbierto(cancellationToken);
        if (existente is not null)
        {
            return BadRequest(new { message = "Ya hay una caja abierta para esta sede." });
        }

        var turno = new CajaTurno
        {
            EmpresaId = User.GetEmpresaId(),
            SucursalId = User.GetSucursalId(),
            UsuarioAperturaId = User.GetUsuarioId(),
            FechaOperativa = DateTime.UtcNow.Date,
            SaldoInicial = request.SaldoInicial,
            ObservacionApertura = Clean(request.Observacion)
        };

        db.CajaTurnos.Add(turno);
        await db.SaveChangesAsync(cancellationToken);
        turno.UsuarioApertura = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == turno.UsuarioAperturaId, cancellationToken);
        return Ok(await ToDto(turno, cancellationToken));
    }

    [HttpPost("turnos/{turnoId:int}/cerrar")]
    [RequirePermission("Operacion.Caja.Cerrar")]
    public async Task<IActionResult> Cerrar(int turnoId, CerrarCajaTurnoRequest request, CancellationToken cancellationToken)
    {
        if (request.EfectivoReal < 0)
        {
            return BadRequest(new { message = "El efectivo real no puede ser negativo." });
        }

        var turno = await db.CajaTurnos
            .FirstOrDefaultAsync(x =>
                x.Id == turnoId &&
                x.EmpresaId == User.GetEmpresaId() &&
                x.SucursalId == User.GetSucursalId() &&
                x.Estado == "Abierta",
                cancellationToken);

        if (turno is null)
        {
            return NotFound();
        }

        var resumen = await CalcularResumen(turno, cancellationToken);
        turno.TotalVentas = resumen.TotalVentas;
        turno.TotalPagos = resumen.TotalPagos;
        turno.TotalEfectivo = resumen.TotalEfectivo;
        turno.EfectivoEsperado = turno.SaldoInicial + resumen.TotalEfectivo;
        turno.EfectivoReal = request.EfectivoReal;
        turno.Diferencia = request.EfectivoReal - turno.EfectivoEsperado;
        turno.ObservacionCierre = Clean(request.Observacion);
        turno.UsuarioCierreId = User.GetUsuarioId();
        turno.FechaCierre = DateTime.UtcNow;
        turno.FechaModificacion = DateTime.UtcNow;
        turno.Estado = "Cerrada";

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<CajaTurno?> GetTurnoAbierto(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        return await db.CajaTurnos
            .Include(x => x.UsuarioApertura)
            .Include(x => x.UsuarioCierre)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && x.Estado == "Abierta", cancellationToken);
    }

    private async Task<CajaTurnoDto> ToDto(CajaTurno turno, CancellationToken cancellationToken)
    {
        var resumen = await CalcularResumen(turno, cancellationToken);
        var efectivoEsperado = turno.Estado == "Cerrada" ? turno.EfectivoEsperado : turno.SaldoInicial + resumen.TotalEfectivo;

        return new CajaTurnoDto(
            turno.Id,
            turno.FechaOperativa,
            turno.Estado,
            turno.UsuarioApertura?.NombreCompleto ?? "Usuario",
            turno.UsuarioCierre?.NombreCompleto,
            turno.SaldoInicial,
            turno.Estado == "Cerrada" ? turno.TotalVentas : resumen.TotalVentas,
            turno.Estado == "Cerrada" ? turno.TotalPagos : resumen.TotalPagos,
            turno.Estado == "Cerrada" ? turno.TotalEfectivo : resumen.TotalEfectivo,
            efectivoEsperado,
            turno.EfectivoReal,
            turno.Estado == "Cerrada" ? turno.Diferencia : null,
            turno.ObservacionApertura,
            turno.ObservacionCierre,
            turno.FechaApertura,
            turno.FechaCierre,
            resumen.PagosPorMetodo,
            resumen.Cuentas);
    }

    private async Task<CajaResumen> CalcularResumen(CajaTurno turno, CancellationToken cancellationToken)
    {
        var cuentas = await db.Cuentas.AsNoTracking()
            .Include(x => x.Mesero)
            .Include(x => x.Pagos)
            .Where(x =>
                x.EmpresaId == turno.EmpresaId &&
                x.SucursalId == turno.SucursalId &&
                x.FechaApertura >= turno.FechaApertura &&
                (turno.FechaCierre == null || x.FechaApertura <= turno.FechaCierre) &&
                x.Estado != "Anulada")
            .OrderByDescending(x => x.FechaApertura)
            .ToListAsync(cancellationToken);

        var pagos = cuentas.SelectMany(x => x.Pagos).ToList();
        var pagosPorMetodo = pagos
            .GroupBy(x => string.IsNullOrWhiteSpace(x.MetodoPago) ? "Sin metodo" : x.MetodoPago)
            .Select(g => new CajaMetodoPagoDto(g.Key, g.Sum(x => x.Valor), g.Count()))
            .OrderBy(x => x.MetodoPago)
            .ToList();

        return new CajaResumen(
            cuentas.Where(x => x.Estado == "Cerrada").Sum(x => x.Total),
            pagos.Sum(x => x.Valor),
            pagos.Where(x => x.MetodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Valor),
            pagosPorMetodo,
            cuentas.Select(x => new CajaCuentaResumenDto(
                x.Id,
                x.Numero,
                x.Mesero?.NombreCompleto ?? "Mesero",
                x.Estado,
                x.Total,
                x.Pagos.Sum(p => p.Valor),
                x.FechaApertura,
                x.FechaCierre)).ToList());
    }

    private sealed record CajaResumen(
        decimal TotalVentas,
        decimal TotalPagos,
        decimal TotalEfectivo,
        IReadOnlyCollection<CajaMetodoPagoDto> PagosPorMetodo,
        IReadOnlyCollection<CajaCuentaResumenDto> Cuentas);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
