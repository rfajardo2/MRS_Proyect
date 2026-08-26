using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

// Orquesta el ciclo de vida de un pago (crear intento, consultar estado,
// conciliar duplicados, aplicar un pago aprobado a la cuenta) sin conocer
// los detalles de ningun proveedor especifico. Toda la mecanica propia de
// una pasarela (firma, formato de checkout, mapeo de estados) vive detras
// de IPaymentGatewayAdapter (ver PayUGatewayAdapter, el unico registrado hoy).
public sealed class PaymentService(
    MrsDrunkDbContext db,
    IInventarioService inventarioService,
    IPaymentGatewayAdapter gatewayAdapter,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const string StatusCancelled = "CANCELLED";
    private const string StatusDuplicateVoided = "ANULADO_DUPLICADO_PAYU";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public async Task<PaymentSessionDto> CreateAsync(
        int empresaId,
        int? sucursalId,
        int usuarioId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var metodo = gatewayAdapter.NormalizeMethod(request.MetodoPago);
        var moneda = gatewayAdapter.NormalizeCurrency(request.Moneda);
        var cuenta = await db.Cuentas
            .Include(x => x.Items)
            .Include(x => x.Pagos)
            .Include(x => x.Mesero)
            .FirstOrDefaultAsync(x =>
                x.Id == request.CuentaId &&
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId &&
                x.MeseroId == usuarioId &&
                x.Estado != "Cerrada" &&
                x.Estado != "Anulada",
                cancellationToken);
        if (cuenta is null)
        {
            throw new InvalidOperationException("La cuenta no existe o no puede recibir pagos.");
        }

        RecalculateAccount(cuenta);

        var approvedExternal = await db.PagoPasarelas.AsNoTracking()
            .Where(x => x.CuentaId == cuenta.Id && x.Estado == PaymentGatewayStatuses.Approved)
            .SumAsync(x => x.ValorPagado, cancellationToken);
        var manualConfirmed = cuenta.Pagos
            .Where(IsManualConfirmedPayment)
            .Sum(x => x.Valor - (x.IncluyePropina ? x.ValorPropina : 0));
        var saldoPendiente = Math.Max(0, cuenta.Total - (approvedExternal + manualConfirmed));
        if (saldoPendiente <= 0)
        {
            throw new InvalidOperationException("La cuenta ya se encuentra cubierta por pagos confirmados.");
        }

        if (request.Valor <= 0)
        {
            throw new InvalidOperationException("El valor del pago debe ser mayor que cero.");
        }

        if (Math.Round(request.Valor, 2) != Math.Round(saldoPendiente, 2))
        {
            throw new InvalidOperationException($"El valor debe coincidir con el saldo pendiente de la cuenta ({saldoPendiente.ToString("N0", CultureInfo.GetCultureInfo("es-CO"))}).");
        }

        var pendings = await db.PagoPasarelas
            .Where(x => x.CuentaId == cuenta.Id && x.Estado == PaymentGatewayStatuses.Pending)
            .ToListAsync(cancellationToken);
        foreach (var pending in pendings)
        {
            pending.Estado = StatusCancelled;
            pending.Observacion = "Cancelado por nuevo intento de pago.";
        }

        var referencia = BuildReference(cuenta);
        var payment = new PagoPasarela
        {
            EmpresaId = empresaId,
            SucursalId = sucursalId,
            CuentaId = cuenta.Id,
            MesaReferencia = Clean(cuenta.Mesa),
            Proveedor = gatewayAdapter.Proveedor,
            MetodoPago = metodo,
            ReferenciaUnica = referencia,
            ValorEsperado = request.Valor,
            Moneda = moneda,
            Estado = PaymentGatewayStatuses.Pending,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(Math.Max(5, gatewayAdapter.PendingExpirationMinutes)),
            UsuarioCreacionId = usuarioId,
            Observacion = "Pago preparado para generar checkout seguro.",
            CheckoutUrl = null,
            RequestPayload = JsonSerializer.Serialize(request, JsonOptions),
            ResponsePayload = null
        };

        db.PagoPasarelas.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var checkoutRequest = new GatewayCheckoutRequest(
            payment.Id,
            cuenta.Id,
            cuenta.Numero,
            cuenta.Cliente,
            referencia,
            metodo,
            request.Valor,
            moneda);
        var checkout = await gatewayAdapter.BuildCheckoutAsync(checkoutRequest, cancellationToken);
        payment.CheckoutUrl = $"/api/payments/{payment.Id}/checkout?empresaId={empresaId}";
        payment.Observacion = checkout.Message;
        payment.ResponsePayload = JsonSerializer.Serialize(new { checkout.Fields, checkout.Message }, JsonOptions);
        await db.SaveChangesAsync(cancellationToken);

        return ToSessionDto(payment, checkout.Message);
    }

    public async Task<PaymentStatusDto?> GetStatusAsync(
        int empresaId,
        int usuarioId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas
            .AsNoTracking()
            .Include(x => x.Cuenta)
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.EmpresaId == empresaId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        if (payment.Cuenta?.MeseroId != usuarioId)
        {
            return null;
        }

        if (ShouldRefreshStatus(payment))
        {
            await TryRefreshStatusAsync(payment.Id, cancellationToken);
            payment = await db.PagoPasarelas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paymentId && x.EmpresaId == empresaId, cancellationToken);
            if (payment is null)
            {
                return null;
            }
        }

        return ToStatusDto(payment);
    }

    public async Task<PaymentStatusDto?> GetPublicStatusAsync(
        int paymentId,
        string? reference,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(reference) &&
            !string.Equals(payment.ReferenciaUnica, reference, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (ShouldRefreshStatus(payment))
        {
            await TryRefreshStatusAsync(payment.Id, cancellationToken);
            payment = await db.PagoPasarelas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
            if (payment is null)
            {
                return null;
            }
        }

        return ToStatusDto(payment);
    }

    public async Task<string?> BuildCheckoutHtmlAsync(
        int empresaId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas.AsNoTracking()
            .Include(x => x.Cuenta)
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.EmpresaId == empresaId, cancellationToken);
        if (payment is null || payment.Estado != PaymentGatewayStatuses.Pending)
        {
            return null;
        }

        var cuenta = payment.Cuenta ?? throw new InvalidOperationException("La cuenta del pago no existe.");
        var checkoutRequest = new GatewayCheckoutRequest(
            payment.Id,
            cuenta.Id,
            cuenta.Numero,
            cuenta.Cliente,
            payment.ReferenciaUnica,
            payment.MetodoPago,
            payment.ValorEsperado,
            payment.Moneda);

        var checkout = await gatewayAdapter.BuildCheckoutAsync(checkoutRequest, cancellationToken);
        return checkout.Html;
    }

    public async Task<(bool Processed, string Message)> ProcessPayUConfirmationAsync(
        IDictionary<string, string> data,
        string payload,
        CancellationToken cancellationToken)
    {
        var parsed = await gatewayAdapter.ParseWebhookAsync(data, payload, cancellationToken);

        var log = new PagoConfirmacionPayU
        {
            Referencia = parsed.Referencia ?? string.Empty,
            TransaccionPayU = parsed.TransactionId,
            EstadoRecibido = parsed.EstadoRecibido,
            ValorRecibido = parsed.ValorRecibido,
            Moneda = parsed.Moneda,
            PayloadCompleto = payload,
            FirmaRecibida = parsed.Signature,
            FirmaValida = false,
            Procesado = false
        };

        db.PagoConfirmacionesPayU.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(parsed.Referencia))
        {
            log.Observacion = "No se recibio referencia en la confirmacion.";
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Missing reference");
        }

        var payment = await db.PagoPasarelas
            .Include(x => x.Cuenta)
            .ThenInclude(x => x!.Items)
            .Include(x => x.Cuenta)
            .ThenInclude(x => x!.Pagos)
            .FirstOrDefaultAsync(x => x.ReferenciaUnica == parsed.Referencia, cancellationToken);
        if (payment is null)
        {
            log.Observacion = "Referencia no encontrada en pagos locales.";
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Reference not found");
        }

        log.PagoPasarelaId = payment.Id;
        log.FirmaValida = parsed.SignatureValid;
        if (!log.FirmaValida)
        {
            log.Observacion = "Firma de PayU invalida.";
            payment.MensajeError = "Se recibio una confirmacion con firma invalida.";
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Invalid signature");
        }

        if (!string.Equals(payment.Moneda, parsed.Moneda, StringComparison.OrdinalIgnoreCase))
        {
            log.Observacion = "Moneda no coincide con la del pago esperado.";
            payment.MensajeError = "La confirmacion llego con una moneda distinta a la esperada.";
            payment.Estado = PaymentGatewayStatuses.Error;
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Currency mismatch");
        }

        if (Math.Round(payment.ValorEsperado, 2) != Math.Round(parsed.ValorRecibido, 2))
        {
            log.Observacion = "Valor recibido no coincide con el esperado.";
            payment.MensajeError = "La confirmacion llego con un valor distinto al esperado.";
            payment.Estado = PaymentGatewayStatuses.Error;
            payment.ValorPagado = parsed.ValorRecibido;
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Amount mismatch");
        }

        var internalStatus = parsed.InternalStatus;
        if (payment.Estado == PaymentGatewayStatuses.Approved && payment.TransaccionPayU == parsed.TransactionId)
        {
            log.Procesado = true;
            log.Observacion = "Confirmacion duplicada ignorada.";
            await db.SaveChangesAsync(cancellationToken);
            return (true, "Duplicated confirmation");
        }

        payment.TransaccionPayU = parsed.TransactionId;
        payment.OrderIdPayU = parsed.OrderId;
        payment.ValorPagado = parsed.ValorRecibido;
        payment.Estado = internalStatus;
        payment.FechaConfirmacion = internalStatus == PaymentGatewayStatuses.Approved ? DateTime.UtcNow : payment.FechaConfirmacion;
        payment.ResponsePayload = payload;
        payment.MensajeError = internalStatus == PaymentGatewayStatuses.Approved ? null : parsed.MensajeError;

        if (internalStatus == PaymentGatewayStatuses.Approved)
        {
            await ApplyApprovedPaymentAsync(payment, cancellationToken);
            log.Observacion = "Pago aprobado y aplicado a la cuenta.";
        }
        else
        {
            log.Observacion = $"Pago actualizado a estado {internalStatus}.";
        }

        log.Procesado = true;
        await db.SaveChangesAsync(cancellationToken);
        return (true, internalStatus);
    }

    public async Task<AdminPaymentActionResultDto> RefreshAdminStatusAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas.FirstOrDefaultAsync(x =>
            x.Id == paymentId &&
            x.EmpresaId == empresaId &&
            x.SucursalId == sucursalId,
            cancellationToken);
        if (payment is null)
        {
            throw new InvalidOperationException("El intento de pago no existe.");
        }

        if (payment.Estado == PaymentGatewayStatuses.Pending &&
            payment.FechaExpiracion.HasValue &&
            payment.FechaExpiracion.Value <= DateTime.UtcNow)
        {
            payment.Estado = PaymentGatewayStatuses.Expired;
            payment.MensajeError = "El intento expiro antes de recibir confirmacion oficial.";
            payment.Observacion = AppendObservation(payment.Observacion, "Marcado como expirado desde panel administrativo.");
            await db.SaveChangesAsync(cancellationToken);
            return new AdminPaymentActionResultDto(payment.Id, payment.Estado, "Intento marcado como expirado.");
        }

        if (!gatewayAdapter.IsConfigured)
        {
            return new AdminPaymentActionResultDto(payment.Id, payment.Estado, "No hay credenciales configuradas para consultar el estado oficial.");
        }

        await TryRefreshStatusAsync(payment.Id, cancellationToken);
        payment = await db.PagoPasarelas.AsNoTracking().FirstAsync(x => x.Id == paymentId, cancellationToken);
        return new AdminPaymentActionResultDto(payment.Id, payment.Estado, BuildStatusMessage(payment));
    }

    public async Task<AdminPaymentActionResultDto> ReprocessApprovedAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Items)
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Pagos)
            .FirstOrDefaultAsync(x =>
                x.Id == paymentId &&
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId,
                cancellationToken);
        if (payment is null)
        {
            throw new InvalidOperationException("El intento de pago no existe.");
        }

        if (payment.Estado != PaymentGatewayStatuses.Approved)
        {
            throw new InvalidOperationException("Solo se pueden reprocesar intentos aprobados.");
        }

        await ApplyApprovedPaymentAsync(payment, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new AdminPaymentActionResultDto(payment.Id, payment.Estado, "Pago aprobado reprocesado correctamente.");
    }

    public async Task<AdminPaymentActionResultDto> CancelPendingAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas.FirstOrDefaultAsync(x =>
            x.Id == paymentId &&
            x.EmpresaId == empresaId &&
            x.SucursalId == sucursalId,
            cancellationToken);
        if (payment is null)
        {
            throw new InvalidOperationException("El intento de pago no existe.");
        }

        if (payment.Estado != PaymentGatewayStatuses.Pending)
        {
            throw new InvalidOperationException("Solo se pueden cancelar intentos pendientes.");
        }

        payment.Estado = StatusCancelled;
        payment.Observacion = AppendObservation(payment.Observacion, "Cancelado desde panel administrativo.");
        await db.SaveChangesAsync(cancellationToken);
        return new AdminPaymentActionResultDto(payment.Id, payment.Estado, "Intento pendiente cancelado.");
    }

    public async Task<PaymentReconciliationResultDto> ReconcileDuplicateCuentaPagosAsync(
        int empresaId,
        int? sucursalId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Pagos)
            .FirstOrDefaultAsync(x =>
                x.Id == paymentId &&
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId,
                cancellationToken);
        if (payment is null)
        {
            throw new InvalidOperationException("El intento de pago no existe.");
        }

        var referencia = payment.ReferenciaUnica;
        if (string.IsNullOrWhiteSpace(referencia))
        {
            throw new InvalidOperationException("El pago no tiene referencia unica para conciliar.");
        }

        var duplicates = await db.CuentaPagos
            .Where(x =>
                x.CuentaId == payment.CuentaId &&
                x.Origen == gatewayAdapter.Proveedor &&
                x.Referencia == referencia &&
                x.Estado != StatusDuplicateVoided)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (duplicates.Count <= 1)
        {
            var canonical = duplicates.FirstOrDefault();
            if (canonical is not null && payment.CuentaPagoId != canonical.Id)
            {
                payment.CuentaPagoId = canonical.Id;
                if (canonical.PagoPasarelaId != payment.Id)
                {
                    canonical.PagoPasarelaId = payment.Id;
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            return new PaymentReconciliationResultDto(
                payment.Id,
                canonical?.Id ?? payment.CuentaPagoId ?? 0,
                0,
                payment.Estado,
                "No se encontraron duplicados historicos por conciliar.");
        }

        var canonicalPago = SelectCanonicalCuentaPago(payment, duplicates);
        payment.CuentaPagoId = canonicalPago.Id;
        if (canonicalPago.PagoPasarelaId != payment.Id)
        {
            canonicalPago.PagoPasarelaId = payment.Id;
        }

        var anulados = 0;
        foreach (var duplicate in duplicates.Where(x => x.Id != canonicalPago.Id))
        {
            duplicate.Estado = StatusDuplicateVoided;
            duplicate.PagoPasarelaId = payment.Id;
            duplicate.Referencia = referencia;
            anulados++;
        }

        payment.Observacion = AppendObservation(payment.Observacion, $"Conciliacion administrativa: {anulados} duplicado(s) de CuentaPagos anulados.");

        if (payment.Cuenta is not null)
        {
            payment.Cuenta.FechaModificacion = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (payment.Estado == PaymentGatewayStatuses.Approved)
        {
            await ReprocessApprovedAsync(empresaId, sucursalId, paymentId, cancellationToken);
        }

        return new PaymentReconciliationResultDto(
            payment.Id,
            canonicalPago.Id,
            anulados,
            payment.Estado,
            anulados > 0
                ? $"Se anularon {anulados} duplicado(s) y se dejo el pago {canonicalPago.Id} como canonico."
                : "No se requirieron cambios.");
    }

    public async Task<BulkPaymentReconcileResultDto> ReconcileDuplicateCuentaPagosBulkAsync(
        int empresaId,
        int? sucursalId,
        IReadOnlyCollection<int> paymentIds,
        CancellationToken cancellationToken)
    {
        var ids = (paymentIds ?? [])
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            throw new InvalidOperationException("Debes seleccionar al menos un intento para conciliar.");
        }

        var items = new List<BulkPaymentReconcileItemDto>();
        var successes = 0;
        var failures = 0;
        var duplicatedVoided = 0;

        foreach (var paymentId in ids)
        {
            try
            {
                var result = await ReconcileDuplicateCuentaPagosAsync(
                    empresaId,
                    sucursalId,
                    paymentId,
                    cancellationToken);

                items.Add(new BulkPaymentReconcileItemDto(
                    result.PaymentId,
                    result.Estado,
                    true,
                    result.Message,
                    result.CanonicalCuentaPagoId,
                    result.DuplicadosAnulados));

                successes++;
                duplicatedVoided += result.DuplicadosAnulados;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No fue posible conciliar el pago PayU {PaymentId} en proceso masivo.", paymentId);
                items.Add(new BulkPaymentReconcileItemDto(
                    paymentId,
                    PaymentGatewayStatuses.Error,
                    false,
                    ex.Message,
                    0,
                    0));
                failures++;
            }
        }

        return new BulkPaymentReconcileResultDto(
            ids.Count,
            items.Count,
            successes,
            failures,
            duplicatedVoided,
            items);
    }

    private async Task ApplyApprovedPaymentAsync(PagoPasarela payment, CancellationToken cancellationToken)
    {
        if (payment.Cuenta is null)
        {
            payment.Cuenta = await db.Cuentas
                .Include(x => x.Items)
                .Include(x => x.Pagos)
                .FirstAsync(x => x.Id == payment.CuentaId, cancellationToken);
        }

        CuentaPago? linkedPago = null;
        if (payment.CuentaPagoId.HasValue)
        {
            linkedPago = await db.CuentaPagos.FirstOrDefaultAsync(x => x.Id == payment.CuentaPagoId.Value, cancellationToken);
        }

        if (linkedPago is null)
        {
            linkedPago = await db.CuentaPagos
                .Where(x =>
                    x.CuentaId == payment.CuentaId &&
                    string.Equals(x.Origen, gatewayAdapter.Proveedor, StringComparison.OrdinalIgnoreCase) &&
                    (
                        x.PagoPasarelaId == payment.Id ||
                        (!x.PagoPasarelaId.HasValue && x.Referencia == payment.ReferenciaUnica)
                    ))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (linkedPago is not null)
        {
            if (linkedPago.PagoPasarelaId != payment.Id)
            {
                linkedPago.PagoPasarelaId = payment.Id;
            }

            payment.CuentaPagoId = linkedPago.Id;
        }
        else
        {
            linkedPago = new CuentaPago
            {
                CuentaId = payment.CuentaId,
                MetodoPago = $"{gatewayAdapter.Proveedor}:{payment.MetodoPago}",
                Origen = gatewayAdapter.Proveedor,
                Estado = "APLICADO",
                Valor = payment.ValorPagado <= 0 ? payment.ValorEsperado : payment.ValorPagado,
                IncluyePropina = false,
                ValorPropina = 0,
                Referencia = payment.ReferenciaUnica,
                UsuarioRegistroId = payment.UsuarioCreacionId,
                PagoPasarelaId = payment.Id
            };
            db.CuentaPagos.Add(linkedPago);
            await db.SaveChangesAsync(cancellationToken);
            payment.CuentaPagoId = linkedPago.Id;
        }

        var cobertura = payment.Cuenta.Pagos
            .Where(IsClosureEligiblePayment)
            .Sum(x => x.Valor - (x.IncluyePropina ? x.ValorPropina : 0));
        if (linkedPago is not null)
        {
            if (payment.Cuenta.Pagos.All(x => x.Id != linkedPago.Id))
            {
                payment.Cuenta.Pagos.Add(linkedPago);
                cobertura += linkedPago.Valor - (linkedPago.IncluyePropina ? linkedPago.ValorPropina : 0);
            }
        }

        RecalculateAccount(payment.Cuenta);
        if (cobertura + 0.01m < payment.Cuenta.Total)
        {
            return;
        }

        await CloseAccountAfterApprovedPaymentAsync(payment.Cuenta, payment.UsuarioCreacionId, cancellationToken);
    }

    private async Task CloseAccountAfterApprovedPaymentAsync(Cuenta cuenta, int usuarioId, CancellationToken cancellationToken)
    {
        if (cuenta.Estado == "Cerrada")
        {
            return;
        }

        cuenta.Estado = "Cerrada";
        cuenta.FechaSolicitudCierre = cuenta.FechaSolicitudCierre ?? DateTime.UtcNow;
        cuenta.FechaCierre = DateTime.UtcNow;
        cuenta.FechaModificacion = DateTime.UtcNow;

        try
        {
            await inventarioService.AplicarSalidaVentaAsync(cuenta, usuarioId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "El pago de la cuenta {CuentaId} fue aprobado, pero no se pudo aplicar la salida de inventario.", cuenta.Id);
            cuenta.Observacion = AppendObservation(
                cuenta.Observacion,
                "Pago aprobado por la pasarela. La cuenta se cerro, pero la salida de inventario quedo pendiente por falta de lotes o stock disponible.");
        }
    }

    private async Task TryRefreshStatusAsync(int paymentId, CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (payment is null || payment.Estado != PaymentGatewayStatuses.Pending || !gatewayAdapter.IsConfigured)
        {
            return;
        }

        payment.FechaUltimaConsulta = DateTime.UtcNow;

        var result = await gatewayAdapter.QueryStatusAsync(payment, cancellationToken);
        if (result is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        payment.ResponsePayload = result.ResponsePayload ?? payment.ResponsePayload;
        if (!result.Success)
        {
            payment.MensajeError = result.ErrorMessage ?? payment.MensajeError;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(result.InternalStatus))
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        payment.TransaccionPayU ??= result.TransactionId;
        payment.OrderIdPayU ??= result.OrderId;
        payment.ValorPagado = result.ValorPagado ?? payment.ValorPagado;
        payment.Estado = result.InternalStatus;
        if (payment.Estado == PaymentGatewayStatuses.Approved)
        {
            await db.Entry(payment).Reference(x => x.Cuenta).LoadAsync(cancellationToken);
            if (payment.Cuenta is not null)
            {
                await db.Entry(payment.Cuenta).Collection(x => x.Items).LoadAsync(cancellationToken);
                await db.Entry(payment.Cuenta).Collection(x => x.Pagos).LoadAsync(cancellationToken);
            }

            await ApplyApprovedPaymentAsync(payment, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private bool IsManualConfirmedPayment(CuentaPago pago) =>
        !string.Equals(pago.Estado, StatusDuplicateVoided, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(pago.Origen, gatewayAdapter.Proveedor, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(pago.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(pago.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);

    private bool IsClosureEligiblePayment(CuentaPago pago) =>
        (
            !string.Equals(pago.Estado, StatusDuplicateVoided, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pago.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pago.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase)
        ) ||
        (
            !string.Equals(pago.Estado, StatusDuplicateVoided, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pago.Origen, gatewayAdapter.Proveedor, StringComparison.OrdinalIgnoreCase) &&
            (
                string.Equals(pago.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pago.Estado, PaymentGatewayStatuses.Approved, StringComparison.OrdinalIgnoreCase)
            )
        );

    private static CuentaPago SelectCanonicalCuentaPago(PagoPasarela payment, IReadOnlyCollection<CuentaPago> duplicates)
    {
        if (payment.CuentaPagoId.HasValue)
        {
            var linked = duplicates.FirstOrDefault(x => x.Id == payment.CuentaPagoId.Value);
            if (linked is not null)
            {
                return linked;
            }
        }

        var byPaymentLink = duplicates.FirstOrDefault(x => x.PagoPasarelaId == payment.Id);
        if (byPaymentLink is not null)
        {
            return byPaymentLink;
        }

        var approvedMirror = duplicates
            .FirstOrDefault(x => string.Equals(x.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase));
        if (approvedMirror is not null)
        {
            return approvedMirror;
        }

        return duplicates.OrderBy(x => x.Id).First();
    }

    private static string BuildReference(Cuenta cuenta)
    {
        var mesa = string.IsNullOrWhiteSpace(cuenta.Mesa) ? "SINMESA" : new string(cuenta.Mesa.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(mesa))
        {
            mesa = "SINMESA";
        }
        return $"BAR-MESA-{mesa}-CUENTA-{cuenta.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    private static void RecalculateAccount(Cuenta cuenta)
    {
        cuenta.Subtotal = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Cantidad * x.PrecioUnitario);
        cuenta.Descuento = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Descuento);
        cuenta.Total = cuenta.Items.Where(x => !x.Eliminado).Sum(x => x.Total);
        cuenta.FechaModificacion = DateTime.UtcNow;
    }

    private bool ShouldRefreshStatus(PagoPasarela payment)
    {
        if (payment.Estado != PaymentGatewayStatuses.Pending || !gatewayAdapter.IsConfigured)
        {
            return false;
        }

        if (payment.FechaExpiracion.HasValue && payment.FechaExpiracion <= DateTime.UtcNow)
        {
            return false;
        }

        return !payment.FechaUltimaConsulta.HasValue || payment.FechaUltimaConsulta <= DateTime.UtcNow.AddSeconds(-20);
    }

    private PaymentSessionDto ToSessionDto(PagoPasarela payment, string? message)
    {
        return new PaymentSessionDto(
            payment.Id,
            payment.CuentaId,
            payment.MetodoPago,
            payment.ReferenciaUnica,
            payment.Estado,
            payment.ValorEsperado,
            payment.ValorPagado,
            payment.Moneda,
            payment.FechaCreacion,
            payment.FechaExpiracion,
            payment.CheckoutUrl,
            message);
    }

    private PaymentStatusDto ToStatusDto(PagoPasarela payment)
    {
        return new PaymentStatusDto(
            payment.Id,
            payment.CuentaId,
            payment.MetodoPago,
            payment.ReferenciaUnica,
            payment.Estado,
            payment.ValorEsperado,
            payment.ValorPagado,
            payment.Moneda,
            payment.FechaCreacion,
            payment.FechaExpiracion,
            payment.FechaConfirmacion,
            payment.Estado == PaymentGatewayStatuses.Pending ? payment.CheckoutUrl : null,
            BuildStatusMessage(payment));
    }

    private static string BuildStatusMessage(PagoPasarela payment)
    {
        return payment.Estado switch
        {
            PaymentGatewayStatuses.Approved => "Pago aprobado por la pasarela y aplicado a la cuenta.",
            PaymentGatewayStatuses.Rejected => "La pasarela rechazo el pago. Puedes generar un nuevo intento.",
            PaymentGatewayStatuses.Declined => "La transaccion fue declinada por la pasarela.",
            PaymentGatewayStatuses.Expired => "El intento de pago expiro. Genera uno nuevo.",
            StatusCancelled => "Este intento fue cancelado por un nuevo intento.",
            PaymentGatewayStatuses.Error => payment.MensajeError ?? "El pago no pudo validarse de forma segura.",
            _ => "Esperando confirmacion oficial de la pasarela."
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string AppendObservation(string? original, string message)
    {
        if (string.IsNullOrWhiteSpace(original))
        {
            return message;
        }

        return $"{original.Trim()} | {message}";
    }
}
