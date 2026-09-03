using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/preparacion")]
[Authorize]
public sealed class PreparacionController(IComandaService comandaService) : ControllerBase
{
    [HttpGet("areas")]
    [RequirePermission("Preparacion.Comandas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<AreaPreparacionDto>>> GetAreas(CancellationToken cancellationToken)
    {
        var data = await comandaService.GetAreasAsync(User.GetEmpresaId(), cancellationToken);
        return Ok(data);
    }

    [HttpGet("tablero")]
    [RequirePermission("Preparacion.Comandas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<PreparacionDetalleDto>>> GetTablero([FromQuery] int? areaId, CancellationToken cancellationToken)
    {
        var data = await comandaService.GetTableroAsync(User.GetEmpresaId(), areaId, cancellationToken);
        return Ok(data);
    }

    [HttpGet("historial")]
    [RequirePermission("Preparacion.Comandas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<HistorialEventoDto>>> GetHistorial([FromQuery] int? areaId, CancellationToken cancellationToken)
    {
        var data = await comandaService.GetHistorialAsync(User.GetEmpresaId(), areaId, cancellationToken);
        return Ok(data);
    }

    [HttpPost("comandas/{comandaId:int}/tomar")]
    [RequirePermission("Preparacion.Comandas.Gestionar")]
    public async Task<IActionResult> TomarComanda(int comandaId, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.TomarComandaAsync(User.GetEmpresaId(), User.GetUsuarioId(), comandaId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("comandas/{comandaId:int}/listo")]
    [RequirePermission("Preparacion.Comandas.Gestionar")]
    public async Task<IActionResult> MarcarListoComanda(int comandaId, MarcarListoComandaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.MarcarListoComandaAsync(User.GetEmpresaId(), User.GetUsuarioId(), comandaId, request.DetallesNoPreparar ?? Array.Empty<int>(), cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("comandas/{comandaId:int}/despachar")]
    [RequirePermission("Preparacion.Comandas.Gestionar")]
    public async Task<IActionResult> DespacharComanda(int comandaId, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.DespacharComandaAsync(User.GetEmpresaId(), User.GetUsuarioId(), comandaId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("detalles/{detalleId:int}/tomar")]
    [RequirePermission("Preparacion.Comandas.Gestionar")]
    public async Task<IActionResult> Tomar(int detalleId, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.TomarAsync(User.GetEmpresaId(), User.GetUsuarioId(), detalleId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("detalles/{detalleId:int}/listo")]
    [RequirePermission("Preparacion.Comandas.Gestionar")]
    public async Task<IActionResult> MarcarListo(int detalleId, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.MarcarListoAsync(User.GetEmpresaId(), User.GetUsuarioId(), detalleId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("detalles/{detalleId:int}/despachar")]
    [RequirePermission("Preparacion.Comandas.Gestionar")]
    public async Task<IActionResult> Despachar(int detalleId, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.DespacharAsync(User.GetEmpresaId(), User.GetUsuarioId(), detalleId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
