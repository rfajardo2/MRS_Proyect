using MRSDrunk.Api.DTOs;

namespace MRSDrunk.Api.Services;

public interface IComandaService
{
    Task<ComandaDto?> EnviarComandaAsync(
        int empresaId,
        int meseroId,
        int cuentaId,
        IReadOnlyCollection<EnviarComandaItemRequest> items,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ComandaDto>> GetComandasPorCuentaAsync(
        int empresaId,
        int meseroId,
        int cuentaId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AreaPreparacionDto>> GetAreasAsync(int empresaId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PreparacionDetalleDto>> GetTableroAsync(int empresaId, int? areaId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistorialEventoDto>> GetHistorialAsync(int empresaId, int? areaId, CancellationToken cancellationToken);

    Task TomarAsync(int empresaId, int usuarioId, int detalleId, CancellationToken cancellationToken);

    Task<int> TomarComandaAsync(int empresaId, int usuarioId, int comandaId, CancellationToken cancellationToken);

    Task<int> MarcarListoComandaAsync(int empresaId, int usuarioId, int comandaId, IReadOnlyCollection<int> detallesNoPreparar, CancellationToken cancellationToken);

    Task<int> DespacharComandaAsync(int empresaId, int usuarioId, int comandaId, CancellationToken cancellationToken);

    Task MarcarListoAsync(int empresaId, int usuarioId, int detalleId, CancellationToken cancellationToken);

    Task DespacharAsync(int empresaId, int usuarioId, int detalleId, CancellationToken cancellationToken);

    Task EntregarAsync(int empresaId, int meseroId, int detalleId, CancellationToken cancellationToken);

    Task CancelarPorCuentaItemAsync(int cuentaItemId, int usuarioId, CancellationToken cancellationToken);
}
