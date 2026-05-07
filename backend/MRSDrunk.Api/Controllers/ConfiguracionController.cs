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
public sealed class ConfiguracionController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet("ventas")]
    [RequirePermission("Configuracion.Ventas.Ver")]
    public async Task<ActionResult<ConfiguracionVentaDto>> GetVentas(CancellationToken cancellationToken)
    {
        var config = await GetOrCreate(User.GetEmpresaId(), cancellationToken);
        return Ok(ToDto(config));
    }

    [HttpGet("ventas-operacion")]
    [RequirePermission("Operacion.Cuentas.Ver")]
    public async Task<ActionResult<ConfiguracionVentaDto>> GetVentasOperacion(CancellationToken cancellationToken)
    {
        var config = await GetOrCreate(User.GetEmpresaId(), cancellationToken);
        return Ok(ToDto(config));
    }

    [HttpPut("ventas")]
    [RequirePermission("Configuracion.Ventas.Editar")]
    public async Task<IActionResult> UpdateVentas(UpsertConfiguracionVentaRequest request, CancellationToken cancellationToken)
    {
        var config = await GetOrCreate(User.GetEmpresaId(), cancellationToken);
        if (!TimeSpan.TryParse(request.HoraInicioDiaOperativo, out var inicio) || !TimeSpan.TryParse(request.HoraCierreDiaOperativo, out var cierre))
        {
            return BadRequest(new { message = "Las horas deben tener formato HH:mm." });
        }

        config.RequiereAprobacionCierre = request.RequiereAprobacionCierre;
        config.PermiteDividirCuenta = request.PermiteDividirCuenta;
        config.PermiteEliminarItems = request.PermiteEliminarItems;
        config.RequiereMotivoEliminarItem = request.RequiereMotivoEliminarItem;
        config.RequiereMotivoAnularCuenta = request.RequiereMotivoAnularCuenta;
        config.PorcentajeRepartoBase = request.PorcentajeRepartoBase;
        config.TarifaCuatroPorMil = request.TarifaCuatroPorMil;
        config.TarifaRetefuente = request.TarifaRetefuente;
        config.TarifaComisionDatafono = request.TarifaComisionDatafono;
        config.TarifaRetIca = request.TarifaRetIca;
        config.ComisionFijaDatafono = request.ComisionFijaDatafono;
        config.PorcentajePropinaDefecto = request.PorcentajePropinaDefecto;
        config.HoraInicioDiaOperativo = inicio;
        config.HoraCierreDiaOperativo = cierre;
        config.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ConfiguracionVenta> GetOrCreate(int empresaId, CancellationToken cancellationToken)
    {
        var config = await db.ConfiguracionesVenta.FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);
        if (config is not null)
        {
            return config;
        }

        config = new ConfiguracionVenta { EmpresaId = empresaId };
        db.ConfiguracionesVenta.Add(config);
        await db.SaveChangesAsync(cancellationToken);
        return config;
    }

    private static ConfiguracionVentaDto ToDto(ConfiguracionVenta x) => new(
        x.Id,
        x.RequiereAprobacionCierre,
        x.PermiteDividirCuenta,
        x.PermiteEliminarItems,
        x.RequiereMotivoEliminarItem,
        x.RequiereMotivoAnularCuenta,
        x.PorcentajeRepartoBase,
        x.TarifaCuatroPorMil,
        x.TarifaRetefuente,
        x.TarifaComisionDatafono,
        x.TarifaRetIca,
        x.ComisionFijaDatafono,
        x.PorcentajePropinaDefecto,
        x.HoraInicioDiaOperativo.ToString(@"hh\:mm"),
        x.HoraCierreDiaOperativo.ToString(@"hh\:mm"));
}
