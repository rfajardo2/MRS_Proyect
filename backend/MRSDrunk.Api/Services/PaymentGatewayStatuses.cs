namespace MRSDrunk.Api.Services;

// Vocabulario de estados internos compartido entre el orquestador (PaymentService)
// y cada adaptador de pasarela (IPaymentGatewayAdapter), para que ambos hablen
// el mismo lenguaje sin acoplarse a los codigos de estado propios de un proveedor.
public static class PaymentGatewayStatuses
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Declined = "DECLINED";
    public const string Error = "ERROR";
    public const string Expired = "EXPIRED";
}
