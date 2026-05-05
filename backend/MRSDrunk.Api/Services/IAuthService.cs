using MRSDrunk.Api.DTOs;

namespace MRSDrunk.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthUserDto?> GetMeAsync(int usuarioId, CancellationToken cancellationToken);
}
