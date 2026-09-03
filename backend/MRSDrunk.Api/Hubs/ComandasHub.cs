using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MRSDrunk.Api.Helpers;

namespace MRSDrunk.Api.Hubs;

[Authorize]
public sealed class ComandasHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var empresaId = Context.User!.GetEmpresaId();
        var usuarioId = Context.User!.GetUsuarioId();

        if (empresaId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PreparacionGroup(empresaId));
        }

        if (usuarioId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UsuarioGroup(usuarioId));
        }

        await base.OnConnectedAsync();
    }

    public static string PreparacionGroup(int empresaId) => $"empresa:{empresaId}:preparacion";

    public static string UsuarioGroup(int usuarioId) => $"usuario:{usuarioId}";
}
