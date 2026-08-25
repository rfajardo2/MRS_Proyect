using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MRSDrunk.Api.Configuration;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

public sealed class PaymentService(
    MrsDrunkDbContext db,
    IInventarioService inventarioService,
    HttpClient httpClient,
    IOptions<PayUSettings> payuOptions,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const string ProviderPayU = "PAYU";
    private const string StatusPending = "PENDING";
    private const string StatusApproved = "APPROVED";
    private const string StatusRejected = "REJECTED";
    private const string StatusDeclined = "DECLINED";
    private const string StatusError = "ERROR";
    private const string StatusExpired = "EXPIRED";
    private const string StatusCancelled = "CANCELLED";
    private const string StatusDuplicateVoided = "ANULADO_DUPLICADO_PAYU";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly PayUSettings _settings = payuOptions.Value;

    public async Task<PaymentSessionDto> CreateAsync(
        int empresaId,
        int? sucursalId,
        int usuarioId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var metodo = NormalizeMethod(request.MetodoPago);
        var moneda = NormalizeCurrency(request.Moneda);
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
            .Where(x => x.CuentaId == cuenta.Id && x.Estado == StatusApproved)
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
            .Where(x => x.CuentaId == cuenta.Id && x.Estado == StatusPending)
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
            Proveedor = ProviderPayU,
            MetodoPago = metodo,
            ReferenciaUnica = referencia,
            ValorEsperado = request.Valor,
            Moneda = moneda,
            Estado = StatusPending,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(Math.Max(5, _settings.PendingExpirationMinutes)),
            UsuarioCreacionId = usuarioId,
            Observacion = "Pago preparado para generar checkout seguro.",
            CheckoutUrl = null,
            RequestPayload = JsonSerializer.Serialize(request, JsonOptions),
            ResponsePayload = null
        };

        db.PagoPasarelas.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var descriptor = BuildCheckoutDescriptor(cuenta, payment.Id, referencia, metodo, request.Valor, moneda);
        payment.CheckoutUrl = $"/api/payments/{payment.Id}/checkout?empresaId={empresaId}";
        payment.Observacion = descriptor.Message;
        payment.ResponsePayload = JsonSerializer.Serialize(descriptor, JsonOptions);
        await db.SaveChangesAsync(cancellationToken);

        return ToSessionDto(payment, descriptor.Message);
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
            await TryRefreshFromPayUQueryAsync(payment.Id, cancellationToken);
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
            await TryRefreshFromPayUQueryAsync(payment.Id, cancellationToken);
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
        if (payment is null || payment.Estado != StatusPending)
        {
            return null;
        }

        var descriptor = BuildCheckoutDescriptor(
            payment.Cuenta ?? throw new InvalidOperationException("La cuenta del pago no existe."),
            payment.Id,
            payment.ReferenciaUnica,
            payment.MetodoPago,
            payment.ValorEsperado,
            payment.Moneda);

        var fields = descriptor.Fields
            .Select(x => $"<input type=\"hidden\" name=\"{HtmlEncode(x.Key)}\" value=\"{HtmlEncode(x.Value)}\" />");
        return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Redirigiendo a PayU</title>
  <style>
    body { font-family: Arial, sans-serif; background: #111216; color: #f7f7f8; display:flex; align-items:center; justify-content:center; min-height:100vh; margin:0; }
    .card { max-width: 460px; padding: 32px; border:1px solid #2b2d36; border-radius: 18px; background:#17181d; box-shadow: 0 12px 40px rgba(0,0,0,.35);}
    h1 { margin-top:0; font-size: 24px; }
    p { color:#c9c9d1; line-height:1.5; }
    button { background:#ef233c; border:none; color:white; font-weight:700; padding:14px 18px; border-radius:12px; cursor:pointer; width:100%; }
  </style>
</head>
<body onload="document.forms[0].submit()">
  <div class="card">
    <h1>Conectando con PayU</h1>
    <p>Estamos abriendo el flujo seguro del pago para la cuenta {{HtmlEncode(payment.Cuenta?.Numero ?? payment.CuentaId.ToString(CultureInfo.InvariantCulture))}}.</p>
    <form method="post" action="{{HtmlEncode(_settings.CheckoutUrl)}}">
      {{string.Join(Environment.NewLine, fields)}}
      <button type="submit">Continuar con PayU</button>
    </form>
  </div>
</body>
</html>
""";
    }

    public async Task<(bool Processed, string Message)> ProcessPayUConfirmationAsync(
        IDictionary<string, string> data,
        string payload,
        CancellationToken cancellationToken)
    {
        var referencia = GetValue(data, "reference_sale", "referenceCode", "reference_pol");
        var transactionId = GetValue(data, "transaction_id", "transactionId");
        var estadoRecibido = GetValue(data, "state_pol", "transactionState", "lapTransactionState");
        var moneda = NormalizeCurrency(GetValue(data, "currency", "currency_pol"));
        var valorRecibido = ParseDecimal(GetValue(data, "value", "TX_VALUE"));
        var signature = GetValue(data, "sign", "signature");

        var log = new PagoConfirmacionPayU
        {
            Referencia = referencia ?? string.Empty,
            TransaccionPayU = transactionId,
            EstadoRecibido = estadoRecibido,
            ValorRecibido = valorRecibido,
            Moneda = moneda,
            PayloadCompleto = payload,
            FirmaRecibida = signature,
            FirmaValida = false,
            Procesado = false
        };

        db.PagoConfirmacionesPayU.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(referencia))
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
            .FirstOrDefaultAsync(x => x.ReferenciaUnica == referencia, cancellationToken);
        if (payment is null)
        {
            log.Observacion = "Referencia no encontrada en pagos locales.";
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Reference not found");
        }

        log.PagoPasarelaId = payment.Id;
        log.FirmaValida = ValidateConfirmationSignature(data, payment);
        if (!log.FirmaValida)
        {
            log.Observacion = "Firma de PayU invalida.";
            payment.MensajeError = "Se recibio una confirmacion con firma invalida.";
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Invalid signature");
        }

        if (!string.Equals(payment.Moneda, moneda, StringComparison.OrdinalIgnoreCase))
        {
            log.Observacion = "Moneda no coincide con la del pago esperado.";
            payment.MensajeError = "La confirmacion llego con una moneda distinta a la esperada.";
            payment.Estado = StatusError;
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Currency mismatch");
        }

        if (Math.Round(payment.ValorEsperado, 2) != Math.Round(valorRecibido, 2))
        {
            log.Observacion = "Valor recibido no coincide con el esperado.";
            payment.MensajeError = "La confirmacion llego con un valor distinto al esperado.";
            payment.Estado = StatusError;
            payment.ValorPagado = valorRecibido;
            await db.SaveChangesAsync(cancellationToken);
            return (false, "Amount mismatch");
        }

        var internalStatus = MapPayUStatus(estadoRecibido);
        if (payment.Estado == StatusApproved && payment.TransaccionPayU == transactionId)
        {
            log.Procesado = true;
            log.Observacion = "Confirmacion duplicada ignorada.";
            await db.SaveChangesAsync(cancellationToken);
            return (true, "Duplicated confirmation");
        }

        payment.TransaccionPayU = transactionId;
        payment.OrderIdPayU = GetValue(data, "reference_pol", "transaction_order_id");
        payment.ValorPagado = valorRecibido;
        payment.Estado = internalStatus;
        payment.FechaConfirmacion = internalStatus == StatusApproved ? DateTime.UtcNow : payment.FechaConfirmacion;
        payment.ResponsePayload = payload;
        payment.MensajeError = internalStatus == StatusApproved ? null : GetValue(data, "response_message_pol", "lapResponseCode");

        if (internalStatus == StatusApproved)
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

        if (payment.Estado == StatusPending &&
            payment.FechaExpiracion.HasValue &&
            payment.FechaExpiracion.Value <= DateTime.UtcNow)
        {
            payment.Estado = StatusExpired;
            payment.MensajeError = "El intento expiro antes de recibir confirmacion oficial.";
            payment.Observacion = AppendObservation(payment.Observacion, "Marcado como expirado desde panel administrativo.");
            await db.SaveChangesAsync(cancellationToken);
            return new AdminPaymentActionResultDto(payment.Id, payment.Estado, "Intento marcado como expirado.");
        }

        if (!_settings.HasCredentials)
        {
            return new AdminPaymentActionResultDto(payment.Id, payment.Estado, "No hay credenciales PayU configuradas para consultar el estado oficial.");
        }

        await TryRefreshFromPayUQueryAsync(payment.Id, cancellationToken);
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

        if (payment.Estado != StatusApproved)
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

        if (payment.Estado != StatusPending)
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
                x.Origen == ProviderPayU &&
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

        if (payment.Estado == StatusApproved)
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
                    StatusError,
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
                    string.Equals(x.Origen, ProviderPayU, StringComparison.OrdinalIgnoreCase) &&
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
                MetodoPago = $"PAYU:{payment.MetodoPago}",
                Origen = ProviderPayU,
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
            logger.LogError(ex, "El pago PayU de la cuenta {CuentaId} fue aprobado, pero no se pudo aplicar la salida de inventario.", cuenta.Id);
            cuenta.Observacion = AppendObservation(
                cuenta.Observacion,
                "Pago aprobado por PayU. La cuenta se cerro, pero la salida de inventario quedo pendiente por falta de lotes o stock disponible.");
        }
    }

    private async Task TryRefreshFromPayUQueryAsync(int paymentId, CancellationToken cancellationToken)
    {
        var payment = await db.PagoPasarelas.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (payment is null || payment.Estado != StatusPending || !_settings.HasCredentials)
        {
            return;
        }

        try
        {
            payment.FechaUltimaConsulta = DateTime.UtcNow;
            var requestBody = new
            {
                language = "es",
                command = "ORDER_DETAIL_BY_REFERENCE_CODE",
                merchant = new
                {
                    apiLogin = _settings.ApiLogin,
                    apiKey = _settings.ApiKey
                },
                details = new
                {
                    referenceCode = payment.ReferenciaUnica
                },
                test = _settings.IsSandbox
            };

            using var response = await httpClient.PostAsJsonAsync(_settings.ApiUrl, requestBody, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            payment.ResponsePayload = payload;
            if (!response.IsSuccessStatusCode)
            {
                payment.MensajeError = $"Consulta PayU respondio {(int)response.StatusCode}.";
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            using var document = JsonDocument.Parse(payload);
            var transaction = document.RootElement
                .GetProperty("result")
                .GetProperty("payload")
                .GetProperty("transactions")
                .EnumerateArray()
                .FirstOrDefault();
            if (transaction.ValueKind == JsonValueKind.Undefined)
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            var state = transaction.GetProperty("transactionResponse").GetProperty("state").GetString();
            if (string.IsNullOrWhiteSpace(state))
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            payment.TransaccionPayU ??= transaction.TryGetProperty("transactionId", out var trxId) ? trxId.GetString() : null;
            payment.OrderIdPayU ??= document.RootElement
                .GetProperty("result")
                .GetProperty("payload")
                .TryGetProperty("id", out var orderId) ? orderId.GetRawText() : null;
            payment.ValorPagado = transaction.GetProperty("transactionResponse").TryGetProperty("totalAmount", out var totalAmount)
                ? ParseDecimal(totalAmount.GetRawText())
                : payment.ValorPagado;
            payment.Estado = MapQueryStatus(state);
            if (payment.Estado == StatusApproved)
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible consultar el estado PayU para el pago {PaymentId}", paymentId);
        }
    }

    private CheckoutDescriptor BuildCheckoutDescriptor(Cuenta cuenta, int paymentId, string referencia, string metodo, decimal valor, string moneda)
    {
        var description = $"Pago cuenta {cuenta.Numero} - MRS Drunk";
        var signature = BuildCheckoutSignature(referencia, valor, moneda);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["merchantId"] = _settings.MerchantId,
            ["accountId"] = _settings.AccountId,
            ["description"] = description,
            ["referenceCode"] = referencia,
            ["amount"] = valor.ToString("0.00", CultureInfo.InvariantCulture),
            ["tax"] = "0",
            ["taxReturnBase"] = "0",
            ["currency"] = moneda,
            ["signature"] = signature,
            ["test"] = _settings.IsSandbox ? "1" : "0",
            ["responseUrl"] = _settings.ResponseUrl,
            ["confirmationUrl"] = _settings.ConfirmationUrl,
            ["extra1"] = cuenta.Id.ToString(CultureInfo.InvariantCulture),
            ["extra2"] = cuenta.Numero,
            ["extra3"] = paymentId.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(cuenta.Cliente))
        {
            fields["buyerFullName"] = cuenta.Cliente!;
        }

        switch (metodo)
        {
            case "QR_BREB":
                fields["selectedPaymentMethod"] = "REDEBAN_INTEROPERABLE";
                fields["paymentMethods"] = "REDEBAN_INTEROPERABLE";
                break;
            case "PSE":
                fields["selectedPaymentMethod"] = "PSE";
                fields["paymentMethods"] = "PSE";
                break;
            case "CARD":
                fields["selectedPaymentMethod"] = "VISA";
                fields["paymentMethods"] = "VISA,MASTERCARD,AMEX,DINERS,CODENSA,VISA_DEBIT,MASTERCARD_DEBIT";
                break;
            default:
                fields["paymentMethods"] = "REDEBAN_INTEROPERABLE,PSE,VISA,MASTERCARD,AMEX,DINERS,CODENSA,VISA_DEBIT,MASTERCARD_DEBIT";
                break;
        }

        return new CheckoutDescriptor(fields, $"Pago {metodo} preparado para PayU.");
    }

    private string BuildCheckoutSignature(string referencia, decimal valor, string moneda)
    {
        var raw = $"{_settings.ApiKey}~{_settings.MerchantId}~{referencia}~{valor.ToString("0.00", CultureInfo.InvariantCulture)}~{moneda}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private bool ValidateConfirmationSignature(IDictionary<string, string> data, PagoPasarela payment)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return false;
        }

        var statePol = GetValue(data, "state_pol", "transactionState") ?? string.Empty;
        var reference = payment.ReferenciaUnica;
        var value = ParseDecimal(GetValue(data, "value", "TX_VALUE"));
        var currency = NormalizeCurrency(GetValue(data, "currency", "currency_pol"));
        var signature = GetValue(data, "sign", "signature");
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var raw = $"{_settings.ApiKey}~{_settings.MerchantId}~{reference}~{value.ToString("0.0", CultureInfo.InvariantCulture)}~{currency}~{statePol}";
        using var md5 = MD5.Create();
        var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return string.Equals(hash, signature.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    private static bool IsManualConfirmedPayment(CuentaPago pago) =>
        !string.Equals(pago.Estado, StatusDuplicateVoided, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(pago.Origen, ProviderPayU, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(pago.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(pago.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);

    private static bool IsClosureEligiblePayment(CuentaPago pago) =>
        (
            !string.Equals(pago.Estado, StatusDuplicateVoided, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pago.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pago.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase)
        ) ||
        (
            !string.Equals(pago.Estado, StatusDuplicateVoided, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pago.Origen, ProviderPayU, StringComparison.OrdinalIgnoreCase) &&
            (
                string.Equals(pago.Estado, "APLICADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pago.Estado, StatusApproved, StringComparison.OrdinalIgnoreCase)
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

    private static string NormalizeMethod(string? method)
    {
        var value = (method ?? string.Empty).Trim().ToUpperInvariant();
        return value switch
        {
            "QR_BREB" => value,
            "CARD" => value,
            "PSE" => value,
            "PAYMENT_LINK" => value,
            _ => throw new InvalidOperationException("El metodo de pago no es valido para PayU.")
        };
    }

    private string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? _settings.Currency.ToUpperInvariant() : currency.Trim().ToUpperInvariant();
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
        if (payment.Estado != StatusPending || !_settings.HasCredentials)
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
            payment.Estado == StatusPending ? payment.CheckoutUrl : null,
            BuildStatusMessage(payment));
    }

    private static string BuildStatusMessage(PagoPasarela payment)
    {
        return payment.Estado switch
        {
            StatusApproved => "Pago aprobado por PayU y aplicado a la cuenta.",
            StatusRejected => "PayU rechazo el pago. Puedes generar un nuevo intento.",
            StatusDeclined => "La transaccion fue declinada por PayU.",
            StatusExpired => "El intento de pago expiro. Genera uno nuevo.",
            StatusCancelled => "Este intento fue cancelado por un nuevo intento.",
            StatusError => payment.MensajeError ?? "El pago no pudo validarse de forma segura.",
            _ => "Esperando confirmacion oficial de PayU."
        };
    }

    private static string MapPayUStatus(string? statePol)
    {
        return statePol switch
        {
            "4" => StatusApproved,
            "5" => StatusExpired,
            "6" => StatusDeclined,
            "104" => StatusError,
            "7" => StatusPending,
            _ => StatusRejected
        };
    }

    private static string MapQueryStatus(string? state)
    {
        return (state ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "APPROVED" => StatusApproved,
            "DECLINED" => StatusDeclined,
            "ERROR" => StatusError,
            "EXPIRED" => StatusExpired,
            "PENDING" => StatusPending,
            _ => StatusRejected
        };
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var normalized = value.Replace(",", ".", StringComparison.Ordinal);
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? decimal.Round(parsed, 2)
            : 0;
    }

    private static string? GetValue(IDictionary<string, string> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

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

    private sealed record CheckoutDescriptor(
        IReadOnlyDictionary<string, string> Fields,
        string Message);
}
