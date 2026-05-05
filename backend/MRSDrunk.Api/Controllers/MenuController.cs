using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var menu = await menuService.GetMenuAsync(User.GetRolId(), cancellationToken);
        return Ok(menu);
    }
}
