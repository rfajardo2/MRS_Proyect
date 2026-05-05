using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;

namespace MRSDrunk.Api.Services;

public sealed class MenuService(MrsDrunkDbContext db) : IMenuService
{
    public async Task<IReadOnlyCollection<MenuModuloDto>> GetMenuAsync(int rolId, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().FirstAsync(x => x.Id == rolId, cancellationToken);

        var windowsQuery = db.Ventanas
            .AsNoTracking()
            .Include(x => x.Modulo)
            .Where(x => x.Estado && x.Modulo != null && x.Modulo.Estado);

        if (!role.EsSuperUsuario)
        {
            windowsQuery = windowsQuery.Where(x => x.RolPermisos.Any(rp =>
                rp.RolId == rolId &&
                rp.PuedeVer &&
                rp.Permiso != null &&
                rp.Permiso.Codigo.EndsWith(".Ver")));
        }

        var windows = await windowsQuery
            .OrderBy(x => x.Modulo!.Orden)
            .ThenBy(x => x.Orden)
            .Select(x => new
            {
                x.Id,
                x.Nombre,
                x.Ruta,
                x.Icono,
                x.Orden,
                ModuloId = x.Modulo!.Id,
                Modulo = x.Modulo!.Nombre,
                ModuloIcono = x.Modulo!.Icono,
                ModuloOrden = x.Modulo!.Orden
            })
            .ToListAsync(cancellationToken);

        return windows
            .GroupBy(x => new { x.ModuloId, x.Modulo, x.ModuloIcono, x.ModuloOrden })
            .OrderBy(x => x.Key.ModuloOrden)
            .Select(g => new MenuModuloDto(
                g.Key.ModuloId,
                g.Key.Modulo,
                g.Key.ModuloIcono,
                g.Key.ModuloOrden,
                g.Select(v => new MenuVentanaDto(v.Id, v.Nombre, v.Ruta, v.Icono, v.Orden)).ToList()))
            .ToList();
    }
}
