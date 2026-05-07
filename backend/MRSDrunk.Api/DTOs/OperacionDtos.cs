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
    decimal PorcentajePropinaDefecto,
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
    decimal PorcentajePropinaDefecto,
    string HoraInicioDiaOperativo,
    string HoraCierreDiaOperativo);

public sealed record ProductoCategoriaDto(int Id, string Nombre, string? Descripcion, int Orden, bool Estado);
public sealed record UpsertProductoCategoriaRequest(string Nombre, string? Descripcion, int Orden, bool Estado);

public sealed record UnidadMedidaDto(int Id, string Codigo, string Nombre, int Decimales, bool Estado);
public sealed record ProveedorDto(int Id, string Nombre, string? Nit, string? Telefono, string? Correo, bool Estado);

public sealed record ProductoDto(
    int Id,
    int CategoriaId,
    string Categoria,
    string Nombre,
    string? Descripcion,
    decimal PrecioVenta,
    decimal? CostoEstimado,
    int? UnidadVentaId,
    string? UnidadVenta,
    int? UnidadInventarioId,
    string? UnidadInventario,
    decimal FactorConversionInventario,
    bool ControlaInventario,
    bool Estado);

public sealed record UpsertProductoRequest(
    int CategoriaId,
    string Nombre,
    string? Descripcion,
    decimal PrecioVenta,
    decimal? CostoEstimado,
    int? UnidadVentaId,
    int? UnidadInventarioId,
    decimal? FactorConversionInventario,
    bool ControlaInventario,
    bool Estado);

public sealed record ProductoRecetaDto(
    int Id,
    int ProductoVentaId,
    int InsumoProductoId,
    string Insumo,
    decimal Cantidad,
    int? UnidadMedidaId,
    string? UnidadMedida,
    bool Estado);

public sealed record UpsertProductoRecetaRequest(int InsumoProductoId, decimal Cantidad, int? UnidadMedidaId, bool Estado);

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

public sealed record CuentaPagoDto(
    int Id,
    string MetodoPago,
    decimal Valor,
    bool IncluyePropina,
    decimal ValorPropina,
    decimal ValorAplicadoCuenta,
    string? Referencia,
    DateTime FechaPago);

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
    decimal TotalPagado,
    decimal TotalPropina,
    decimal TotalAplicadoCuenta,
    decimal SaldoPendiente,
    decimal PagoEnExceso,
    IReadOnlyCollection<CuentaItemDto> Items,
    IReadOnlyCollection<CuentaPagoDto> Pagos);

public sealed record CrearCuentaRequest(string? Mesa, string? Cliente, string? Observacion);
public sealed record AgregarCuentaItemRequest(int ProductoId, decimal Cantidad, decimal? PrecioUnitario, decimal Descuento);
public sealed record EliminarCuentaItemRequest(string? Motivo);
public sealed record DividirCuentaRequest(bool Dividida);
public sealed record RegistrarPagoRequest(string MetodoPago, decimal Valor, bool IncluyePropina, decimal ValorPropina, string? Referencia);
public sealed record ResolverCierreCuentaRequest(bool Aprobar, string? Motivo);
public sealed record AnularCuentaRequest(string? Motivo);

public sealed record BalanceMeseroDto(int MeseroId, string Mesero, int CuentasAbiertas, int CuentasCerradas, decimal TotalVendido, decimal TotalPagado);

public sealed record BalanceDiaProductoDto(string Producto, decimal Cantidad, decimal Total);

public sealed record BalanceDiaDto(
    int MeseroId,
    string Mesero,
    int CuentasAbiertas,
    int CuentasCerradas,
    int CuentasPendientes,
    int CuentasRechazadas,
    decimal TotalVendido,
    decimal TotalPagado,
    decimal TotalPropinas,
    decimal SaldoPendiente,
    IReadOnlyCollection<CajaMetodoPagoDto> PagosPorMetodo,
    IReadOnlyCollection<BalanceDiaProductoDto> Productos,
    IReadOnlyCollection<CuentaDto> Cuentas);

public sealed record CajaMetodoPagoDto(string MetodoPago, decimal Total, int Cantidad);

public sealed record CajaCuentaResumenDto(
    int Id,
    string Numero,
    string Mesero,
    string Estado,
    decimal Total,
    decimal TotalPagado,
    DateTime FechaApertura,
    DateTime? FechaCierre);

public sealed record CajaTurnoDto(
    int Id,
    DateTime FechaOperativa,
    string Estado,
    string UsuarioApertura,
    string? UsuarioCierre,
    decimal SaldoInicial,
    decimal TotalVentas,
    decimal TotalPagos,
    decimal TotalEfectivo,
    decimal EfectivoEsperado,
    decimal? EfectivoReal,
    decimal? Diferencia,
    string? ObservacionApertura,
    string? ObservacionCierre,
    DateTime FechaApertura,
    DateTime? FechaCierre,
    IReadOnlyCollection<CajaMetodoPagoDto> PagosPorMetodo,
    IReadOnlyCollection<CajaCuentaResumenDto> Cuentas);

public sealed record AbrirCajaTurnoRequest(decimal SaldoInicial, string? Observacion);
public sealed record CerrarCajaTurnoRequest(decimal EfectivoReal, string? Observacion);

public sealed record InventarioStockDto(
    int ProductoId,
    string Producto,
    string Categoria,
    string? UnidadInventario,
    decimal CantidadActual,
    decimal CantidadMinima,
    bool BajoMinimo,
    bool Estado);

public sealed record InventarioMovimientoDto(
    int Id,
    int ProductoId,
    string Producto,
    string Tipo,
    decimal Cantidad,
    decimal SaldoAnterior,
    decimal SaldoNuevo,
    decimal? CostoUnitario,
    string? Referencia,
    string? Motivo,
    string? Lote,
    DateTime? FechaVencimiento,
    string Usuario,
    DateTime FechaMovimiento);

public sealed record RegistrarMovimientoInventarioRequest(
    int ProductoId,
    string Tipo,
    decimal Cantidad,
    decimal? CostoUnitario,
    string? Referencia,
    string? Motivo);

public sealed record ActualizarStockMinimoRequest(int ProductoId, decimal CantidadMinima);

public sealed record InventarioLoteDto(
    int Id,
    int ProductoId,
    string Producto,
    string Categoria,
    string CodigoLote,
    DateTime? FechaVencimiento,
    decimal CantidadActual,
    decimal CostoUnitario,
    string? Proveedor,
    bool Vencido,
    bool ProximoVencer);

public sealed record InventarioCompraDetalleRequest(int ProductoId, decimal Cantidad, decimal CostoUnitario, string? CodigoLote, DateTime? FechaVencimiento);
public sealed record CrearInventarioCompraRequest(int? ProveedorId, string? NumeroFactura, DateTime? FechaCompra, string? Observacion, IReadOnlyCollection<InventarioCompraDetalleRequest> Detalles);

public sealed record InventarioCompraDetalleDto(int ProductoId, string Producto, decimal Cantidad, decimal CostoUnitario, string? CodigoLote, DateTime? FechaVencimiento, decimal Total);
public sealed record InventarioCompraDto(int Id, string? Proveedor, string? NumeroFactura, DateTime FechaCompra, decimal Total, string Usuario, IReadOnlyCollection<InventarioCompraDetalleDto> Detalles);

public sealed record InventarioReporteDto(
    IReadOnlyCollection<BalanceDiaProductoDto> ProductosVendidos,
    IReadOnlyCollection<BalanceDiaProductoDto> Perdidas,
    decimal StockValorizado,
    decimal ComprasPeriodo);
