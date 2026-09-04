namespace MRSDrunk.Api.DTOs;

public sealed record RegistroAuditoriaDto(
    int Id,
    int UsuarioId,
    string? Usuario,
    string Entidad,
    string EntidadId,
    string Accion,
    string? Detalle,
    DateTime FechaCreacion);
