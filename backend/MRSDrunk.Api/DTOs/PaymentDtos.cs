namespace MRSDrunk.Api.DTOs;

public sealed record CreatePaymentRequest(
    int CuentaId,
    string MetodoPago,
    decimal Valor,
    string? Moneda);

public sealed record PaymentSessionDto(
    int PaymentId,
    int CuentaId,
    string MetodoPago,
    string Referencia,
    string Estado,
    decimal ValorEsperado,
    decimal ValorPagado,
    string Moneda,
    DateTime FechaCreacion,
    DateTime? FechaExpiracion,
    string? CheckoutUrl,
    string? Message);

public sealed record PaymentStatusDto(
    int PaymentId,
    int CuentaId,
    string MetodoPago,
    string Referencia,
    string Estado,
    decimal ValorEsperado,
    decimal ValorPagado,
    string Moneda,
    DateTime FechaCreacion,
    DateTime? FechaExpiracion,
    DateTime? FechaConfirmacion,
    string? CheckoutUrl,
    string Message);

public sealed record PaymentAdminDashboardDto(
    DateTime Desde,
    DateTime Hasta,
    IReadOnlyCollection<PaymentAdminListItemDto> Payments,
    IReadOnlyCollection<PaymentAdminOrphanConfirmationDto> OrphanConfirmations);

public sealed record PaymentAdminListItemDto(
    int Id,
    int CuentaId,
    string CuentaNumero,
    int MeseroId,
    string Mesero,
    string? Mesa,
    string? Cliente,
    string MetodoPago,
    string Estado,
    string Referencia,
    decimal ValorEsperado,
    decimal ValorPagado,
    string Moneda,
    string? TransaccionPayU,
    string? OrderIdPayU,
    DateTime FechaCreacion,
    DateTime? FechaExpiracion,
    DateTime? FechaConfirmacion,
    string? CheckoutUrl,
    bool CuentaCerrada,
    bool TienePagoAplicado,
    int Confirmaciones,
    int ConfirmacionesInvalidas,
    IReadOnlyCollection<string> Discrepancias,
    string? Observacion,
    string? MensajeError);

public sealed record PaymentAdminDetailDto(
    PaymentAdminListItemDto Payment,
    CuentaDto? Cuenta,
    IReadOnlyCollection<PaymentAdminConfirmationDto> Confirmaciones,
    IReadOnlyCollection<PaymentAdminCuentaPagoDuplicateDto> DuplicateCuentaPagos);

public sealed record PaymentAdminConfirmationDto(
    int Id,
    string Referencia,
    string? TransaccionPayU,
    string? EstadoRecibido,
    decimal? ValorRecibido,
    string? Moneda,
    string? FirmaRecibida,
    bool FirmaValida,
    bool Procesado,
    string? Observacion,
    DateTime FechaRecepcion,
    string PayloadCompleto);

public sealed record PaymentAdminOrphanConfirmationDto(
    int Id,
    string Referencia,
    string? TransaccionPayU,
    string? EstadoRecibido,
    decimal? ValorRecibido,
    string? Moneda,
    bool FirmaValida,
    bool Procesado,
    string? Observacion,
    DateTime FechaRecepcion,
    int? CuentaId,
    string? CuentaNumero,
    string? Mesa,
    string? Cliente,
    string? Usuario);

public sealed record AdminPaymentActionResultDto(
    int PaymentId,
    string Estado,
    string Message);

public sealed record PaymentAdminCuentaPagoDuplicateDto(
    int Id,
    int CuentaId,
    decimal Valor,
    decimal ValorPropina,
    string Estado,
    string MetodoPago,
    string Origen,
    string? Referencia,
    int? PagoPasarelaId,
    DateTime FechaPago,
    bool EsCanonico);

public sealed record PaymentReconciliationResultDto(
    int PaymentId,
    int CanonicalCuentaPagoId,
    int DuplicadosAnulados,
    string Estado,
    string Message);

public sealed record BulkPaymentReconcileRequest(
    IReadOnlyCollection<int> PaymentIds);

public sealed record BulkPaymentReconcileItemDto(
    int PaymentId,
    string Estado,
    bool Success,
    string Message,
    int CanonicalCuentaPagoId,
    int DuplicadosAnulados);

public sealed record BulkPaymentReconcileResultDto(
    int TotalSolicitados,
    int Procesados,
    int Exitos,
    int Fallidos,
    int DuplicadosAnulados,
    IReadOnlyCollection<BulkPaymentReconcileItemDto> Items);

public sealed record PaymentExecutiveReportDto(
    DateTime Desde,
    DateTime Hasta,
    PaymentExecutiveHeadlineDto Headline,
    IReadOnlyCollection<PaymentExecutiveMethodDto> Metodos,
    IReadOnlyCollection<PaymentExecutiveUserDto> Usuarios,
    IReadOnlyCollection<PaymentExecutiveTimelineDto> Timeline,
    IReadOnlyCollection<PaymentExecutiveIssueDto> Hallazgos,
    IReadOnlyCollection<PaymentAdminListItemDto> PagosConIncidencias);

public sealed record PaymentExecutiveHeadlineDto(
    int Intentos,
    int Aprobados,
    int Pendientes,
    int Fallidos,
    int Expirados,
    int Cancelados,
    int Discrepancias,
    int ConfirmacionesHuerfanas,
    decimal ValorEsperado,
    decimal ValorAprobado,
    decimal ValorPendiente,
    decimal TicketPromedioAprobado,
    decimal TasaAprobacion);

public sealed record PaymentExecutiveMethodDto(
    string MetodoPago,
    int Intentos,
    int Aprobados,
    int Pendientes,
    int Fallidos,
    decimal ValorEsperado,
    decimal ValorAprobado,
    decimal TasaAprobacion,
    int Discrepancias);

public sealed record PaymentExecutiveUserDto(
    int MeseroId,
    string Mesero,
    int Intentos,
    int Aprobados,
    int Pendientes,
    int Fallidos,
    decimal ValorEsperado,
    decimal ValorAprobado,
    decimal TasaAprobacion,
    int Discrepancias);

public sealed record PaymentExecutiveTimelineDto(
    DateTime Fecha,
    int Intentos,
    int Aprobados,
    int Pendientes,
    int Fallidos,
    decimal ValorEsperado,
    decimal ValorAprobado);

public sealed record PaymentExecutiveIssueDto(
    string Tipo,
    int Total,
    decimal ValorComprometido);
