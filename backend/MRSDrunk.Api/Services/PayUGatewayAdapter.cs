using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MRSDrunk.Api.Configuration;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

// Implementacion concreta de IPaymentGatewayAdapter para PayU Latam.
// Toda la mecanica propia de PayU (firma MD5, formato de campos del checkout,
// nombres de campos del webhook, codigos de estado, API de consulta de ordenes)
// vive aqui; PaymentService no conoce ninguno de estos detalles.
public sealed class PayUGatewayAdapter(
    HttpClient httpClient,
    IOptions<PayUSettings> payuOptions,
    ILogger<PayUGatewayAdapter> logger) : IPaymentGatewayAdapter
{
    private readonly PayUSettings _settings = payuOptions.Value;

    public string Proveedor => "PAYU";

    public bool IsConfigured => _settings.HasCredentials;

    public int PendingExpirationMinutes => _settings.PendingExpirationMinutes;

    public string NormalizeMethod(string? metodoPago)
    {
        var value = (metodoPago ?? string.Empty).Trim().ToUpperInvariant();
        return value switch
        {
            "QR_BREB" => value,
            "CARD" => value,
            "PSE" => value,
            "PAYMENT_LINK" => value,
            _ => throw new InvalidOperationException("El metodo de pago no es valido para PayU.")
        };
    }

    public string NormalizeCurrency(string? moneda)
    {
        return string.IsNullOrWhiteSpace(moneda) ? _settings.Currency.ToUpperInvariant() : moneda.Trim().ToUpperInvariant();
    }

    public Task<GatewayCheckoutResult> BuildCheckoutAsync(GatewayCheckoutRequest request, CancellationToken cancellationToken)
    {
        var fields = BuildCheckoutFields(request);
        var message = $"Pago {request.MetodoPago} preparado para PayU.";
        var html = BuildCheckoutHtml(request, fields);
        return Task.FromResult(new GatewayCheckoutResult(fields, message, html));
    }

    public Task<GatewayWebhookResult> ParseWebhookAsync(IDictionary<string, string> data, string rawPayload, CancellationToken cancellationToken)
    {
        var referencia = GetValue(data, "reference_sale", "referenceCode", "reference_pol");
        var transactionId = GetValue(data, "transaction_id", "transactionId");
        var orderId = GetValue(data, "reference_pol", "transaction_order_id");
        var estadoRecibido = GetValue(data, "state_pol", "transactionState", "lapTransactionState");
        var moneda = NormalizeCurrency(GetValue(data, "currency", "currency_pol"));
        var valorRecibido = ParseDecimal(GetValue(data, "value", "TX_VALUE"));
        var signature = GetValue(data, "sign", "signature");

        var internalStatus = MapPayUStatus(estadoRecibido);
        var signatureValid = IsSignatureValid(referencia, valorRecibido, moneda, estadoRecibido, signature);
        var mensajeError = internalStatus == PaymentGatewayStatuses.Approved
            ? null
            : GetValue(data, "response_message_pol", "lapResponseCode");

        return Task.FromResult(new GatewayWebhookResult(
            referencia,
            transactionId,
            orderId,
            estadoRecibido,
            internalStatus,
            moneda,
            valorRecibido,
            signature,
            signatureValid,
            mensajeError));
    }

    public async Task<GatewayQueryResult?> QueryStatusAsync(PagoPasarela payment, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
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
            if (!response.IsSuccessStatusCode)
            {
                return new GatewayQueryResult(false, null, null, null, null, payload, $"Consulta PayU respondio {(int)response.StatusCode}.");
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
                return new GatewayQueryResult(true, null, null, null, null, payload, null);
            }

            var state = transaction.GetProperty("transactionResponse").GetProperty("state").GetString();
            if (string.IsNullOrWhiteSpace(state))
            {
                return new GatewayQueryResult(true, null, null, null, null, payload, null);
            }

            var transactionId = transaction.TryGetProperty("transactionId", out var trxId) ? trxId.GetString() : null;
            var orderId = document.RootElement
                .GetProperty("result")
                .GetProperty("payload")
                .TryGetProperty("id", out var orderIdEl) ? orderIdEl.GetRawText() : null;
            var valorPagado = transaction.GetProperty("transactionResponse").TryGetProperty("totalAmount", out var totalAmount)
                ? ParseDecimal(totalAmount.GetRawText())
                : (decimal?)null;

            return new GatewayQueryResult(true, MapQueryStatus(state), transactionId, orderId, valorPagado, payload, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible consultar el estado PayU para el pago {PaymentId}", payment.Id);
            return null;
        }
    }

    private Dictionary<string, string> BuildCheckoutFields(GatewayCheckoutRequest request)
    {
        var description = $"Pago cuenta {request.CuentaNumero} - MRS Drunk";
        var signature = BuildCheckoutSignature(request.Referencia, request.Valor, request.Moneda);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["merchantId"] = _settings.MerchantId,
            ["accountId"] = _settings.AccountId,
            ["description"] = description,
            ["referenceCode"] = request.Referencia,
            ["amount"] = request.Valor.ToString("0.00", CultureInfo.InvariantCulture),
            ["tax"] = "0",
            ["taxReturnBase"] = "0",
            ["currency"] = request.Moneda,
            ["signature"] = signature,
            ["test"] = _settings.IsSandbox ? "1" : "0",
            ["responseUrl"] = _settings.ResponseUrl,
            ["confirmationUrl"] = _settings.ConfirmationUrl,
            ["extra1"] = request.CuentaId.ToString(CultureInfo.InvariantCulture),
            ["extra2"] = request.CuentaNumero,
            ["extra3"] = request.PaymentId.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(request.CuentaCliente))
        {
            fields["buyerFullName"] = request.CuentaCliente!;
        }

        switch (request.MetodoPago)
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

        return fields;
    }

    private string BuildCheckoutHtml(GatewayCheckoutRequest request, IReadOnlyDictionary<string, string> fields)
    {
        var hiddenInputs = fields
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
    <p>Estamos abriendo el flujo seguro del pago para la cuenta {{HtmlEncode(request.CuentaNumero)}}.</p>
    <form method="post" action="{{HtmlEncode(_settings.CheckoutUrl)}}">
      {{string.Join(Environment.NewLine, hiddenInputs)}}
      <button type="submit">Continuar con PayU</button>
    </form>
  </div>
</body>
</html>
""";
    }

    private string BuildCheckoutSignature(string referencia, decimal valor, string moneda)
    {
        var raw = $"{_settings.ApiKey}~{_settings.MerchantId}~{referencia}~{valor.ToString("0.00", CultureInfo.InvariantCulture)}~{moneda}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private bool IsSignatureValid(string? referencia, decimal valor, string moneda, string? statePol, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var raw = $"{_settings.ApiKey}~{_settings.MerchantId}~{referencia}~{valor.ToString("0.0", CultureInfo.InvariantCulture)}~{moneda}~{statePol}";
        using var md5 = MD5.Create();
        var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return string.Equals(hash, signature.Trim().ToLowerInvariant(), StringComparison.Ordinal);
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

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

    private static string MapPayUStatus(string? statePol)
    {
        return statePol switch
        {
            "4" => PaymentGatewayStatuses.Approved,
            "5" => PaymentGatewayStatuses.Expired,
            "6" => PaymentGatewayStatuses.Declined,
            "104" => PaymentGatewayStatuses.Error,
            "7" => PaymentGatewayStatuses.Pending,
            _ => PaymentGatewayStatuses.Rejected
        };
    }

    private static string MapQueryStatus(string? state)
    {
        return (state ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "APPROVED" => PaymentGatewayStatuses.Approved,
            "DECLINED" => PaymentGatewayStatuses.Declined,
            "ERROR" => PaymentGatewayStatuses.Error,
            "EXPIRED" => PaymentGatewayStatuses.Expired,
            "PENDING" => PaymentGatewayStatuses.Pending,
            _ => PaymentGatewayStatuses.Rejected
        };
    }
}
