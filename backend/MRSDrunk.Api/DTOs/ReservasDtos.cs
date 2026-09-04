namespace MRSDrunk.Api.DTOs;

public sealed record MesaDto(
    int Id,
    string Nombre,
    int Capacidad,
    int PosicionX,
    int PosicionY,
    string Estado,
    bool Activa);

public sealed record UpsertMesaRequest(
    string Nombre,
    int Capacidad,
    int PosicionX,
    int PosicionY,
    bool Activa);

public sealed record CambiarEstadoMesaRequest(string Estado);

public sealed record ReservaDto(
    int Id,
    int MesaId,
    string Mesa,
    string Cliente,
    string? Telefono,
    int NumeroPersonas,
    DateTime FechaHora,
    int DuracionMinutos,
    string Estado,
    string? Observacion,
    DateTime FechaCreacion);

public sealed record UpsertReservaRequest(
    int MesaId,
    string Cliente,
    string? Telefono,
    int NumeroPersonas,
    DateTime FechaHora,
    int DuracionMinutos,
    string? Observacion);

public sealed record CambiarEstadoReservaRequest(string Estado);
