namespace MRSDrunk.Api.Services;

public interface IAuditoriaService
{
    // Registra un cambio en el log de auditoria. No lanza si falla el guardado
    // (un problema de auditoria no debe tumbar la operacion de negocio que la origino).
    Task RegistrarAsync(
        int empresaId,
        int? sucursalId,
        int usuarioId,
        string entidad,
        string entidadId,
        string accion,
        string? detalle,
        CancellationToken cancellationToken);
}
