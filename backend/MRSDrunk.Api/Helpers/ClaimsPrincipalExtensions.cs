using System.Security.Claims;

namespace MRSDrunk.Api.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUsuarioId(this ClaimsPrincipal principal) => GetInt(principal, "usuarioId");
    public static int GetEmpresaId(this ClaimsPrincipal principal) => GetInt(principal, "empresaId");
    public static int GetRolId(this ClaimsPrincipal principal) => GetInt(principal, "rolId");
    public static string GetNombreRol(this ClaimsPrincipal principal) => principal.FindFirstValue("nombreRol") ?? string.Empty;

    private static int GetInt(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        return int.TryParse(value, out var result) ? result : 0;
    }
}
