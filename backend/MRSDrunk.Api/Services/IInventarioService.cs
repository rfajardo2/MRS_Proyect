using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

public interface IInventarioService
{
    Task RegistrarMovimientoAsync(
        int empresaId,
        int? sucursalId,
        int productoId,
        int usuarioId,
        string tipo,
        decimal cantidad,
        decimal? costoUnitario,
        string? referencia,
        string? motivo,
        int? cuentaId,
        int? cuentaItemId,
        CancellationToken cancellationToken);

    Task AplicarSalidaVentaAsync(Cuenta cuenta, int usuarioId, CancellationToken cancellationToken);
}
