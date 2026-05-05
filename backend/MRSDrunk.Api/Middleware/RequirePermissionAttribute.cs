using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string codigo) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var user = context.HttpContext.User;
        var allowed = await permissionService.HasPermissionAsync(
            user.GetUsuarioId(),
            user.GetRolId(),
            codigo,
            context.HttpContext.RequestAborted);

        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}
