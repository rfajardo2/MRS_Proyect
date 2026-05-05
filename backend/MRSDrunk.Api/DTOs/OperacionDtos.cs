namespace MRSDrunk.Api.DTOs;

public sealed record ConfiguracionVentaDto(
    int Id,
    bool RequiereAprobacionCierre,
    bool PermiteDividirCuenta,
    bool PermiteEliminarItems,
    bool RequiereMotivoEliminarItem,
    bool RequiereMotivoAnularCuenta,
    decimal PorcentajeRepartoBase,
    decimal TarifaCuatroPorMil,
    decimal TarifaRetefuente,
    decimal TarifaComisionDatafono,
    decimal TarifaRetIca,
    decimal ComisionFijaDatafono,
    string HoraInicioDiaOperativo,
    string HoraCierreDiaOperativo);

public sealed record UpsertConfiguracionVentaRequest(
    bool RequiereAprobacionCierre,
    bool PermiteDividirCuenta,
    bool PermiteEliminarItems,
    bool RequiereMotivoEliminarItem,
    bool RequiereMotivoAnularCuenta,
    decimal PorcentajeRepartoBase,
    decimal TarifaCuatroPorMil,
    decimal TarifaRetefuente,
    decimal TarifaComisionDatafono,
    decimal TarifaRetIca,
    decimal ComisionFijaDatafono,
    string HoraInicioDiaOperativo,
    string HoraCierreDiaOperativo);

public sealed record ProductoCategoriaDto(int Id, string Nombre, string? Descripcion, int Orden, bool Estado);
public sealed record UpsertProductoCategoriaRequest(string Nombre, string? Descripcion, int Orden, bool Estado);

public sealed record ProductoDto(
    int Id,
    int CategoriaId,
    string Categoria,
    string Nombre,
    string? Descripcion,
    decimal PrecioVenta,
    decimal? CostoEstimado,
    bool ControlaInventario,
    bool Estado);

public sealed record UpsertProductoRequest(
    int CategoriaId,
    string Nombre,
    string? Descripcion,
    decimal PrecioVenta,
    decimal? CostoEstimado,
    bool ControlaInventario,
    bool Estado);

public sealed record CuentaItemDto(
    int Id,
    int ProductoId,
    string ProductoNombre,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Total,
    bool Eliminado,
    string? MotivoEliminacion);

public sealed record CuentaPagoDto(int Id, string MetodoPago, decimal Valor, string? Referencia, DateTime FechaPago);

public sealed record CuentaDto(
    int Id,
    string Numero,
    string? Mesa,
    string? Cliente,
    string Estado,
    bool Dividida,
    int MeseroId,
    string Mesero,
    DateTime FechaApertura,
    DateTime? FechaSolicitudCierre,
    DateTime? FechaCierre,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    IReadOnlyCollection<CuentaItemDto> Items,
    IReadOnlyCollection<CuentaPagoDto> Pagos);

public sealed record CrearCuentaRequest(string? Mesa, string? Cliente, string? Observacion);
public sealed record AgregarCuentaItemRequest(int ProductoId, decimal Cantidad, decimal? PrecioUnitario, decimal Descuento);
public sealed record EliminarCuentaItemRequest(string? Motivo);
public sealed record DividirCuentaRequest(bool Dividida);
public sealed record RegistrarPagoRequest(string MetodoPago, decimal Valor, string? Referencia);
public sealed record ResolverCierreCuentaRequest(bool Aprobar, string? Motivo);
public sealed record AnularCuentaRequest(string? Motivo);

public sealed record BalanceMeseroDto(int MeseroId, string Mesero, int CuentasAbiertas, int CuentasCerradas, decimal TotalVendido, decimal TotalPagado);
