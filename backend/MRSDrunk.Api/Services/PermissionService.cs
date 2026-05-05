using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;

namespace MRSDrunk.Api.Services;

public sealed class PermissionService(MrsDrunkDbContext db) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(int usuarioId, int rolId, string codigo, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rolId && x.Estado, cancellationToken);
        if (role?.EsSuperUsuario == true)
        {
            return true;
        }

        var action = codigo.Split('.').LastOrDefault() ?? "Ver";
        var permission = await db.RolPermisos
            .AsNoTracking()
            .Include(x => x.Permiso)
            .FirstOrDefaultAsync(x => x.RolId == rolId && x.Permiso != null && x.Permiso.Codigo == codigo, cancellationToken);

        return action switch
        {
            "Ver" => permission?.PuedeVer == true,
            "Crear" => permission?.PuedeCrear == true,
            "Consultar" => permission?.PuedeConsultar == true,
            "Editar" => permission?.PuedeEditar == true,
            "Eliminar" => permission?.PuedeEliminar == true,
            _ => permission?.PuedeVer == true
        };
    }
}
