namespace MRSDrunk.Api.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int usuarioId, int rolId, string codigo, CancellationToken cancellationToken);
}
