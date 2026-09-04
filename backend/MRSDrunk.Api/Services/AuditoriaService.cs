using MRSDrunk.Api.Data;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

public sealed class AuditoriaService(MrsDrunkDbContext db, ILogger<AuditoriaService> logger) : IAuditoriaService
{
    public async Task RegistrarAsync(
        int empresaId,
        int? sucursalId,
        int usuarioId,
        string entidad,
        string entidadId,
        string accion,
        string? detalle,
        CancellationToken cancellationToken)
    {
        try
        {
            db.RegistrosAuditoria.Add(new RegistroAuditoria
            {
                EmpresaId = empresaId,
                SucursalId = sucursalId,
                UsuarioId = usuarioId,
                Entidad = entidad,
                EntidadId = entidadId,
                Accion = accion,
                Detalle = detalle
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No fue posible registrar auditoria para {Entidad} {EntidadId} ({Accion}).", entidad, entidadId, accion);
        }
    }
}
