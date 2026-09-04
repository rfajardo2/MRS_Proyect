using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Services;
using Xunit;

namespace MRSDrunk.Api.Tests.Middleware;

public class RequirePermissionAttributeTests
{
    private static AuthorizationFilterContext BuildContext(ClaimsPrincipal user, IPermissionService permissionService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(permissionService);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = user
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static ClaimsPrincipal AuthenticatedUser(int usuarioId = 1, int rolId = 1)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("usuarioId", usuarioId.ToString()),
            new Claim("rolId", rolId.ToString())
        ], authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task OnAuthorizationAsync_SinAutenticar_DevuelveUnauthorized()
    {
        var permissionService = new Mock<IPermissionService>(MockBehavior.Strict);
        var context = BuildContext(new ClaimsPrincipal(new ClaimsIdentity()), permissionService.Object);
        var attribute = new RequirePermissionAttribute("Operacion.Cuentas.Editar");

        await attribute.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
        permissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnAuthorizationAsync_AutenticadoSinPermiso_DevuelveForbid()
    {
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.HasPermissionAsync(1, 1, "Operacion.Cuentas.Editar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var context = BuildContext(AuthenticatedUser(), permissionService.Object);
        var attribute = new RequirePermissionAttribute("Operacion.Cuentas.Editar");

        await attribute.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_AutenticadoConPermiso_NoEstableceResultado()
    {
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.HasPermissionAsync(1, 1, "Operacion.Cuentas.Editar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var context = BuildContext(AuthenticatedUser(), permissionService.Object);
        var attribute = new RequirePermissionAttribute("Operacion.Cuentas.Editar");

        await attribute.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_ConsultaElPermisoConElCodigoDeclaradoEnElAtributo()
    {
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.HasPermissionAsync(7, 3, "Operacion.PagosPayU.Conciliar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var context = BuildContext(AuthenticatedUser(usuarioId: 7, rolId: 3), permissionService.Object);
        var attribute = new RequirePermissionAttribute("Operacion.PagosPayU.Conciliar");

        await attribute.OnAuthorizationAsync(context);

        permissionService.Verify(
            x => x.HasPermissionAsync(7, 3, "Operacion.PagosPayU.Conciliar", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
