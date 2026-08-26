using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/public/seguimiento")]
public sealed class PublicSeguimientoController(MrsDrunkDbContext db) : ControllerBase
{
    private static readonly TimeSpan VentanaPostCierre = TimeSpan.FromMinutes(20);

    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<SeguimientoPublicoCuentaDto>> Get(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NotFound();
        }

        var cuenta = await db.Cuentas.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.TokenPublico == token, cancellationToken);

        if (cuenta is null || cuenta.Estado == "Anulada")
        {
            return NotFound();
        }

        if (cuenta.Estado == "Cerrada" && cuenta.FechaCierre.HasValue &&
            DateTime.UtcNow > cuenta.FechaCierre.Value.Add(VentanaPostCierre))
        {
            return NotFound();
        }

        var cuentaItemIds = cuenta.Items.Select(x => x.Id).ToList();
        var detallesPorItem = await db.ComandaDetalles.AsNoTracking()
            .Include(x => x.Comanda)
            .Where(x => cuentaItemIds.Contains(x.CuentaItemId))
            .ToDictionaryAsync(x => x.CuentaItemId, cancellationToken);

        var items = cuenta.Items
            .OrderBy(x => x.FechaCreacion).ThenBy(x => x.Id)
            .Select(x =>
            {
                detallesPorItem.TryGetValue(x.Id, out var detalle);
                var estado = x.Eliminado ? "CANCELADO" : (detalle?.Estado ?? "SIN_COMANDA");
                return new SeguimientoPublicoItemDto(
                    x.ProductoNombre,
                    x.Cantidad,
                    x.Eliminado ? 0 : x.Total,
                    estado,
                    detalle?.Comanda?.Numero,
                    x.Eliminado);
            })
            .ToList();

        var total = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Total);
        var totalAplicado = OperacionController.EffectivePayments(cuenta.Pagos)
            .Sum(p => Math.Max(0, p.Valor - (p.IncluyePropina ? p.ValorPropina : 0)));
        var saldoPendiente = Math.Max(0, total - totalAplicado);

        return Ok(new SeguimientoPublicoCuentaDto(
            cuenta.Numero,
            cuenta.Mesa,
            cuenta.Estado,
            total,
            saldoPendiente,
            cuenta.Estado == "Cerrada",
            items));
    }
}
