namespace MRSDrunk.Api.Configuration;

public sealed class PayUSettings
{
    public string Env { get; set; } = "sandbox";
    public string ApiUrlSandbox { get; set; } = "https://sandbox.api.payulatam.com/payments-api/4.0/service.cgi";
    public string ApiUrlProduction { get; set; } = "https://api.payulatam.com/payments-api/4.0/service.cgi";
    public string CheckoutUrlSandbox { get; set; } = "https://sandbox.checkout.payulatam.com/ppp-web-gateway-payu/";
    public string CheckoutUrlProduction { get; set; } = "https://checkout.payulatam.com/ppp-web-gateway-payu/";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiLogin { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Currency { get; set; } = "COP";
    public string ConfirmationUrl { get; set; } = string.Empty;
    public string ResponseUrl { get; set; } = string.Empty;
    public int PendingExpirationMinutes { get; set; } = 20;

    public bool IsSandbox => string.Equals(Env, "sandbox", StringComparison.OrdinalIgnoreCase);
    public string ApiUrl => IsSandbox ? ApiUrlSandbox : ApiUrlProduction;
    public string CheckoutUrl => IsSandbox ? CheckoutUrlSandbox : CheckoutUrlProduction;
    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ApiLogin) &&
        !string.IsNullOrWhiteSpace(MerchantId) &&
        !string.IsNullOrWhiteSpace(AccountId);
}
