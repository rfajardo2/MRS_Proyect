namespace MRSDrunk.Api.DTOs;

public sealed record LoginRequest(string UsuarioOCorreo, string Password);

public sealed record AuthUserDto(
    int UsuarioId,
    int EmpresaId,
    int RolId,
    string NombreUsuario,
    string NombreCompleto,
    string NombreRol,
    string Empresa,
    bool EsSuperUsuario);

public sealed record LoginResponse(string Token, DateTime ExpiraEn, AuthUserDto Usuario);

public sealed record SesionUsuarioDto(
    int Id,
    int UsuarioId,
    string NombreCompleto,
    string NombreUsuario,
    string Rol,
    string Empresa,
    string? IpAddress,
    string? UserAgent,
    DateTime FechaInicio,
    DateTime UltimaActividad,
    DateTime FechaExpiracion,
    bool EsSesionActual);

public sealed record SesionResumenUsuarioDto(
    int UsuarioId,
    string NombreCompleto,
    string NombreUsuario,
    string Rol,
    int SesionesActivas);
