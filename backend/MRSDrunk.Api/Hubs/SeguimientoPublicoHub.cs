using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;

namespace MRSDrunk.Api.Hubs;

public sealed class SeguimientoPublicoHub(MrsDrunkDbContext db) : Hub
{
    private static readonly TimeSpan VentanaPostCierre = TimeSpan.FromMinutes(20);

    public async Task JoinCuenta(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var cuenta = await db.Cuentas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenPublico == token);

        if (cuenta is null || cuenta.Estado == "Anulada")
        {
            return;
        }

        if (cuenta.Estado == "Cerrada" && cuenta.FechaCierre.HasValue &&
            DateTime.UtcNow > cuenta.FechaCierre.Value.Add(VentanaPostCierre))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, PublicGroup(token));
    }

    public static string PublicGroup(string token) => $"cuenta:{token}";
}
