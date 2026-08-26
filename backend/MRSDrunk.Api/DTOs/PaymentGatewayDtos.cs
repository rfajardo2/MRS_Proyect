namespace MRSDrunk.Api.DTOs;

public sealed record GatewayCheckoutRequest(
    int PaymentId,
    int CuentaId,
    string CuentaNumero,
    string? CuentaCliente,
    string Referencia,
    string MetodoPago,
    decimal Valor,
    string Moneda);

public sealed record GatewayCheckoutResult(
    IReadOnlyDictionary<string, string> Fields,
    string Message,
    string Html);

public sealed record GatewayWebhookResult(
    string? Referencia,
    string? TransactionId,
    string? OrderId,
    string? EstadoRecibido,
    string InternalStatus,
    string Moneda,
    decimal ValorRecibido,
    string? Signature,
    bool SignatureValid,
    string? MensajeError);

public sealed record GatewayQueryResult(
    bool Success,
    string? InternalStatus,
    string? TransactionId,
    string? OrderId,
    decimal? ValorPagado,
    string? ResponsePayload,
    string? ErrorMessage);
