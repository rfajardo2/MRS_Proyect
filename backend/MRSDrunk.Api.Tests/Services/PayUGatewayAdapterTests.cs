using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRSDrunk.Api.Configuration;
using MRSDrunk.Api.Services;
using Xunit;

namespace MRSDrunk.Api.Tests.Services;

public class PayUGatewayAdapterTests
{
    private static readonly PayUSettings Settings = new()
    {
        Env = "sandbox",
        ApiKey = "test-api-key",
        ApiLogin = "test-api-login",
        MerchantId = "999999",
        AccountId = "888888",
        Currency = "COP",
        ConfirmationUrl = "https://example.test/api/payments/payu/confirmation",
        ResponseUrl = "https://example.test/payment/result"
    };

    private static PayUGatewayAdapter CreateAdapter() =>
        new(new HttpClient(), Options.Create(Settings), NullLogger<PayUGatewayAdapter>.Instance);

    private static string ComputeSignature(string referencia, decimal valor, string moneda, string statePol)
    {
        var raw = $"{Settings.ApiKey}~{Settings.MerchantId}~{referencia}~{valor.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}~{moneda}~{statePol}";
        using var md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    [Fact]
    public async Task ParseWebhookAsync_ConFirmaCorrecta_MarcaSignatureValidComoTrue()
    {
        var adapter = CreateAdapter();
        var referencia = "BAR-MESA-1-CUENTA-1-20260101000000000";
        var valor = 25000m;
        var moneda = "COP";
        var estado = "4"; // Aprobado

        var data = new Dictionary<string, string>
        {
            ["reference_sale"] = referencia,
            ["value"] = valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            ["currency"] = moneda,
            ["state_pol"] = estado,
            ["sign"] = ComputeSignature(referencia, valor, moneda, estado),
            ["transaction_id"] = "trx-1"
        };

        var result = await adapter.ParseWebhookAsync(data, rawPayload: "raw", cancellationToken: default);

        Assert.True(result.SignatureValid);
        Assert.Equal(PaymentGatewayStatuses.Approved, result.InternalStatus);
    }

    [Fact]
    public async Task ParseWebhookAsync_ConFirmaAlterada_MarcaSignatureValidComoFalse()
    {
        var adapter = CreateAdapter();
        var referencia = "BAR-MESA-1-CUENTA-1-20260101000000000";

        var data = new Dictionary<string, string>
        {
            ["reference_sale"] = referencia,
            ["value"] = "25000.00",
            ["currency"] = "COP",
            ["state_pol"] = "4",
            ["sign"] = "0123456789abcdef0123456789abcdef",
            ["transaction_id"] = "trx-1"
        };

        var result = await adapter.ParseWebhookAsync(data, rawPayload: "raw", cancellationToken: default);

        Assert.False(result.SignatureValid);
    }

    [Fact]
    public async Task ParseWebhookAsync_SiElValorRecibidoCambiaRespectoAlFirmado_LaFirmaQuedaInvalida()
    {
        // La firma cubre el valor: si alguien manipula "value" en el payload sin
        // recalcular "sign", la validacion debe rechazarla aunque el resto luzca bien.
        var adapter = CreateAdapter();
        var referencia = "BAR-MESA-1-CUENTA-1-20260101000000000";
        var firmaParaValorOriginal = ComputeSignature(referencia, 25000m, "COP", "4");

        var data = new Dictionary<string, string>
        {
            ["reference_sale"] = referencia,
            ["value"] = "1.00", // Valor manipulado, la firma sigue siendo la del valor original
            ["currency"] = "COP",
            ["state_pol"] = "4",
            ["sign"] = firmaParaValorOriginal
        };

        var result = await adapter.ParseWebhookAsync(data, rawPayload: "raw", cancellationToken: default);

        Assert.False(result.SignatureValid);
    }

    [Fact]
    public async Task ParseWebhookAsync_SinFirma_MarcaSignatureValidComoFalse()
    {
        var adapter = CreateAdapter();
        var data = new Dictionary<string, string>
        {
            ["reference_sale"] = "BAR-MESA-1-CUENTA-1-20260101000000000",
            ["value"] = "25000.00",
            ["currency"] = "COP",
            ["state_pol"] = "4"
        };

        var result = await adapter.ParseWebhookAsync(data, rawPayload: "raw", cancellationToken: default);

        Assert.False(result.SignatureValid);
    }

    [Theory]
    [InlineData("4", PaymentGatewayStatuses.Approved)]
    [InlineData("6", PaymentGatewayStatuses.Declined)]
    [InlineData("5", PaymentGatewayStatuses.Expired)]
    [InlineData("104", PaymentGatewayStatuses.Error)]
    [InlineData("7", PaymentGatewayStatuses.Pending)]
    [InlineData("otro-no-documentado", PaymentGatewayStatuses.Rejected)]
    public async Task ParseWebhookAsync_MapeaCadaCodigoDeEstadoPayUAlEstadoInterno(string statePol, string estadoInternoEsperado)
    {
        var adapter = CreateAdapter();
        var referencia = "BAR-MESA-1-CUENTA-1-20260101000000000";
        var data = new Dictionary<string, string>
        {
            ["reference_sale"] = referencia,
            ["value"] = "25000.00",
            ["currency"] = "COP",
            ["state_pol"] = statePol,
            ["sign"] = ComputeSignature(referencia, 25000m, "COP", statePol)
        };

        var result = await adapter.ParseWebhookAsync(data, rawPayload: "raw", cancellationToken: default);

        Assert.Equal(estadoInternoEsperado, result.InternalStatus);
    }

    [Theory]
    [InlineData("card", "CARD")]
    [InlineData(" pse ", "PSE")]
    [InlineData("qr_breb", "QR_BREB")]
    [InlineData("payment_link", "PAYMENT_LINK")]
    public void NormalizeMethod_AceptaLosMetodosSoportados(string entrada, string esperado)
    {
        var adapter = CreateAdapter();

        Assert.Equal(esperado, adapter.NormalizeMethod(entrada));
    }

    [Fact]
    public void NormalizeMethod_ConMetodoNoSoportado_Lanza()
    {
        var adapter = CreateAdapter();

        Assert.Throws<InvalidOperationException>(() => adapter.NormalizeMethod("BITCOIN"));
    }

    [Fact]
    public void NormalizeCurrency_SinValor_UsaLaMonedaPorDefectoDeLaConfiguracion()
    {
        var adapter = CreateAdapter();

        Assert.Equal("COP", adapter.NormalizeCurrency(null));
    }
}
