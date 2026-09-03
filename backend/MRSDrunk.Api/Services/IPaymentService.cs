using MRSDrunk.Api.DTOs;

namespace MRSDrunk.Api.Services;

public interface IPaymentService
{
    Task<PaymentSessionDto> CreateAsync(
        int empresaId,
        int? sucursalId,
        int usuarioId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken);

    Task<PaymentStatusDto?> GetStatusAsync(
        int empresaId,
        int usuarioId,
        int paymentId,
        CancellationToken cancellationToken);

    Task<PaymentStatusDto?> GetPublicStatusAsync(
        int paymentId,
        string? reference,
        CancellationToken cancellationToken);

    Task<string?> BuildCheckoutHtmlAsync(
        int empresaId,
        int paymentId,
        CancellationToken cancellationToken);

    Task<(bool Processed, string Message)> ProcessPayUConfirmationAsync(
        IDictionary<string, string> data,
        string payload,
        CancellationToken cancellationToken);

    Task<AdminPaymentActionResultDto> RefreshAdminStatusAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken);

    Task<AdminPaymentActionResultDto> ReprocessApprovedAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken);

    Task<AdminPaymentActionResultDto> CancelPendingAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken);

    Task<PaymentReconciliationResultDto> ReconcileDuplicateCuentaPagosAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken);

    Task<BulkPaymentReconcileResultDto> ReconcileDuplicateCuentaPagosBulkAsync(
        int empresaId,
        int? sucursalId,
        IReadOnlyCollection<int> paymentIds,
        CancellationToken cancellationToken);
}
