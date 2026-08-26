using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

// Punto de extension para conectar cualquier pasarela de pago en linea.
// PaymentService (el orquestador) solo conoce esta interfaz; toda la mecanica
// propia de un proveedor (firma, mapeo de estados, HTML de checkout, consulta
// de estado) vive en la implementacion concreta (ver PayUGatewayAdapter).
//
// Hoy solo existe un proveedor registrado (PayU), por lo que PaymentService
// recibe una unica instancia de IPaymentGatewayAdapter. Si en el futuro se
// agrega un segundo proveedor real, ese es el momento de cambiar el registro
// en Program.cs a IEnumerable<IPaymentGatewayAdapter> con una busqueda por
// Proveedor; no se construye esa busqueda ahora para una sola implementacion.
public interface IPaymentGatewayAdapter
{
    // Debe coincidir con el valor que se guarda en PagoPasarela.Proveedor.
    string Proveedor { get; }

    // Indica si el proveedor tiene credenciales validas configuradas
    // (equivalente a lo que antes exponia PayUSettings.HasCredentials).
    bool IsConfigured { get; }

    // Minutos que un intento de pago permanece vigente antes de expirar.
    int PendingExpirationMinutes { get; }

    // Valida y normaliza el metodo de pago a la nomenclatura del proveedor.
    // Lanza InvalidOperationException si el metodo no es soportado.
    string NormalizeMethod(string? metodoPago);

    // Normaliza la moneda solicitada, usando la moneda por defecto del proveedor si no se especifica.
    string NormalizeCurrency(string? moneda);

    // Construye los campos de checkout y el HTML de redireccion hacia el proveedor.
    Task<GatewayCheckoutResult> BuildCheckoutAsync(GatewayCheckoutRequest request, CancellationToken cancellationToken);

    // Interpreta la notificacion (webhook/IPN) del proveedor: valida firma,
    // mapea el estado recibido al vocabulario interno y extrae los valores relevantes.
    Task<GatewayWebhookResult> ParseWebhookAsync(IDictionary<string, string> data, string rawPayload, CancellationToken cancellationToken);

    // Consulta el estado actual de un pago directamente contra el proveedor.
    // Devuelve null cuando la consulta no pudo completarse (se reintenta mas tarde).
    Task<GatewayQueryResult?> QueryStatusAsync(PagoPasarela payment, CancellationToken cancellationToken);
}
