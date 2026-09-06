using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;

namespace MRSDrunk.Api.Controllers;

// Efecto Zeigarnik: las tareas incompletas se recuerdan mejor cuando quedan
// visibles todo el tiempo (un contador en el sidebar), no solo al entrar a la
// pantalla especifica. Este endpoint solo agrega conteos ya cubiertos por
// permisos propios de cada modulo; el frontend solo muestra el contador si el
// usuario ya tiene acceso a ese modulo en el sidebar.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PendientesController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PendientesResumenDto>> Get(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var hoy = DateTime.UtcNow.Date;
        var manana = hoy.AddDays(1);

        var cuentasAbiertas = await db.Cuentas.AsNoTracking()
            .CountAsync(x => x.EmpresaId == empresaId && x.Estado == "Abierta", cancellationToken);

        var comandasPendientes = await db.ComandaDetalles.AsNoTracking()
            .CountAsync(x =>
                x.Comanda!.EmpresaId == empresaId &&
                (x.Estado == "PENDIENTE" || x.Estado == "EN_PREPARACION"),
                cancellationToken);

        var reservasHoySinConfirmar = await db.Reservas.AsNoTracking()
            .CountAsync(x =>
                x.EmpresaId == empresaId &&
                x.Estado == "Pendiente" &&
                x.FechaHora >= hoy && x.FechaHora < manana,
                cancellationToken);

        return Ok(new PendientesResumenDto(cuentasAbiertas, comandasPendientes, reservasHoySinConfirmar));
    }
}
