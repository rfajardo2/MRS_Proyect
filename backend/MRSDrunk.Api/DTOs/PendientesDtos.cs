namespace MRSDrunk.Api.DTOs;

public sealed record PendientesResumenDto(
    int CuentasAbiertas,
    int ComandasPendientes,
    int ReservasHoySinConfirmar);
