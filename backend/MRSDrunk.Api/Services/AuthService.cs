using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MRSDrunk.Api.Configuration;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Services;

public sealed class AuthService(MrsDrunkDbContext db, IOptions<JwtSettings> jwtOptions, IHttpContextAccessor httpContextAccessor) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Usuarios
            .Include(x => x.Empresa)
            .Include(x => x.Rol)
            .FirstOrDefaultAsync(x =>
                x.Estado &&
                (x.UsuarioNombre == request.UsuarioOCorreo || x.Correo == request.UsuarioOCorreo),
                cancellationToken);

        if (user?.Rol is null || user.Empresa is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);
        var sessionId = Guid.NewGuid().ToString("N");
        var http = httpContextAccessor.HttpContext;
        db.UsuarioSesiones.Add(new UsuarioSesion
        {
            UsuarioId = user.Id,
            EmpresaId = user.EmpresaId,
            SessionId = sessionId,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers["User-Agent"].ToString(),
            FechaExpiracion = expires,
            Estado = true
        });
        await db.SaveChangesAsync(cancellationToken);

        var dto = new AuthUserDto(
            user.Id,
            user.EmpresaId,
            user.RolId,
            user.UsuarioNombre,
            user.NombreCompleto,
            user.Rol.Nombre,
            user.Empresa.Nombre,
            user.Rol.EsSuperUsuario);

        return new LoginResponse(BuildToken(dto, expires, sessionId), expires, dto);
    }

    public async Task<AuthUserDto?> GetMeAsync(int usuarioId, CancellationToken cancellationToken)
    {
        return await db.Usuarios
            .AsNoTracking()
            .Include(x => x.Empresa)
            .Include(x => x.Rol)
            .Where(x => x.Id == usuarioId && x.Estado)
            .Select(x => new AuthUserDto(
                x.Id,
                x.EmpresaId,
                x.RolId,
                x.UsuarioNombre,
                x.NombreCompleto,
                x.Rol!.Nombre,
                x.Empresa!.Nombre,
                x.Rol!.EsSuperUsuario))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private string BuildToken(AuthUserDto user, DateTime expires, string sessionId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Jti, sessionId),
            new("sessionId", sessionId),
            new("usuarioId", user.UsuarioId.ToString()),
            new("empresaId", user.EmpresaId.ToString()),
            new("rolId", user.RolId.ToString()),
            new("nombreUsuario", user.NombreUsuario),
            new("nombreRol", user.NombreRol),
            new(ClaimTypes.Role, user.NombreRol)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expires, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
