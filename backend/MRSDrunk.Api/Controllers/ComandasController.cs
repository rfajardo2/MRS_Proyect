using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ComandasController(IComandaService comandaService) : ControllerBase
{
    [HttpGet("cuentas/{cuentaId:int}")]
    [RequirePermission("Operacion.Cuentas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<ComandaDto>>> GetPorCuenta(int cuentaId, CancellationToken cancellationToken)
    {
        var data = await comandaService.GetComandasPorCuentaAsync(User.GetEmpresaId(), User.GetUsuarioId(), cuentaId, cancellationToken);
        return Ok(data);
    }

    [HttpPost("cuentas/{cuentaId:int}/enviar")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<ActionResult<ComandaDto>> Enviar(int cuentaId, EnviarComandaRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "Debes agregar al menos un producto para enviar la comanda." });
        }

        try
        {
            var comanda = await comandaService.EnviarComandaAsync(
                User.GetEmpresaId(),
                User.GetUsuarioId(),
                cuentaId,
                request.Items,
                cancellationToken);

            if (comanda is null)
            {
                return NotFound();
            }

            return Ok(comanda);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("detalles/{detalleId:int}/entregar")]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<IActionResult> Entregar(int detalleId, CancellationToken cancellationToken)
    {
        try
        {
            await comandaService.EntregarAsync(User.GetEmpresaId(), User.GetUsuarioId(), detalleId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
