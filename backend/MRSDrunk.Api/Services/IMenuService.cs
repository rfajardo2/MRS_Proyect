using MRSDrunk.Api.DTOs;

namespace MRSDrunk.Api.Services;

public interface IMenuService
{
    Task<IReadOnlyCollection<MenuModuloDto>> GetMenuAsync(int rolId, CancellationToken cancellationToken);
}
