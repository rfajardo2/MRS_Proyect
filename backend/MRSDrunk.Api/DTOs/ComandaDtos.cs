namespace MRSDrunk.Api.DTOs;

public sealed record EnviarComandaItemRequest(int ProductoId, decimal Cantidad, decimal? PrecioUnitario, decimal Descuento, string? Observacion);
public sealed record EnviarComandaRequest(IReadOnlyCollection<EnviarComandaItemRequest> Items);
public sealed record MarcarListoComandaRequest(IReadOnlyCollection<int> DetallesNoPreparar);

public sealed record ComandaDetalleDto(
    int Id,
    int CuentaItemId,
    int ProductoId,
    string ProductoNombre,
    int? AreaPreparacionId,
    string? AreaPreparacionNombre,
    decimal Cantidad,
    bool RequierePreparacion,
    string Estado,
    string? Observacion,
    int? UsuarioTomaId,
    string? UsuarioToma,
    DateTime? FechaToma,
    int? UsuarioListoId,
    string? UsuarioListo,
    DateTime? FechaListo,
    int? UsuarioDespachoId,
    string? UsuarioDespacho,
    DateTime? FechaDespacho,
    int? UsuarioEntregaId,
    string? UsuarioEntrega,
    DateTime? FechaEntrega,
    DateTime FechaCreacion);

public sealed record ComandaDto(
    int Id,
    int CuentaId,
    string Numero,
    string Estado,
    int MeseroId,
    string Mesero,
    DateTime FechaCreacion,
    IReadOnlyCollection<ComandaDetalleDto> Detalles);

public sealed record AreaPreparacionDto(int Id, string Nombre, string? Descripcion, int Orden);

public sealed record PreparacionDetalleDto(
    int Id,
    int ComandaId,
    string ComandaNumero,
    int CuentaId,
    string CuentaNumero,
    string? Mesa,
    string Mesero,
    int ProductoId,
    string ProductoNombre,
    decimal Cantidad,
    bool RequierePreparacion,
    string Estado,
    string? Observacion,
    int? AreaPreparacionId,
    string? AreaPreparacionNombre,
    DateTime FechaCreacion,
    DateTime? FechaToma,
    string? UsuarioToma,
    DateTime? FechaListo,
    string? UsuarioListo);

public sealed record HistorialEventoDto(
    int Id,
    string ComandaNumero,
    string CuentaNumero,
    string? Mesa,
    string ProductoNombre,
    string? EstadoAnterior,
    string EstadoNuevo,
    string Usuario,
    DateTime Fecha);
