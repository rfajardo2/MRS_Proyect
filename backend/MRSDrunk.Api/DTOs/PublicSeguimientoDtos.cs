namespace MRSDrunk.Api.DTOs;

public sealed record SeguimientoPublicoItemDto(
    string ProductoNombre,
    decimal Cantidad,
    decimal Total,
    string Estado,
    string? ComandaNumero,
    bool Eliminado);

public sealed record SeguimientoPublicoCuentaDto(
    string Numero,
    string? Mesa,
    string Estado,
    decimal TotalActual,
    decimal SaldoPendiente,
    bool SoloLectura,
    IReadOnlyCollection<SeguimientoPublicoItemDto> Items);
