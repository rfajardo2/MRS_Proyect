using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Controllers;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;
using Xunit;

namespace MRSDrunk.Api.Tests.Controllers;

// Cubre la validacion de solape de horario de ReservasController: es la
// unica regla de negocio real del modulo (evitar reservar dos veces la
// misma mesa en un horario que se cruza).
public class ReservasControllerTests
{
    private static MrsDrunkDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<MrsDrunkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MrsDrunkDbContext(options);
    }

    private static ReservasController CreateController(MrsDrunkDbContext db, int empresaId = 1, int usuarioId = 10)
    {
        var controller = new ReservasController(db);
        var identity = new ClaimsIdentity(
        [
            new Claim("empresaId", empresaId.ToString()),
            new Claim("usuarioId", usuarioId.ToString())
        ], authenticationType: "Test");

        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private static async Task<Mesa> SeedMesaAsync(MrsDrunkDbContext db, int empresaId = 1)
    {
        var mesa = new Mesa { EmpresaId = empresaId, Nombre = "Mesa 1", Capacidad = 4 };
        db.Mesas.Add(mesa);
        await db.SaveChangesAsync();
        return mesa;
    }

    [Fact]
    public async Task Post_ConHorarioLibre_CreaLaReserva()
    {
        await using var db = CreateDb();
        var mesa = await SeedMesaAsync(db);
        var controller = CreateController(db);

        var result = await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Juan Perez", null, 4, new DateTime(2026, 9, 4, 20, 0, 0), 90, null),
            default);

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        Assert.Equal(1, await db.Reservas.CountAsync());
    }

    [Fact]
    public async Task Post_ConHorarioQueSeCruzaConUnaReservaActiva_Rechaza()
    {
        await using var db = CreateDb();
        var mesa = await SeedMesaAsync(db);
        var controller = CreateController(db);

        await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Juan Perez", null, 4, new DateTime(2026, 9, 4, 20, 0, 0), 90, null),
            default);

        var result = await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Otro Cliente", null, 2, new DateTime(2026, 9, 4, 20, 30, 0), 60, null),
            default);

        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
        Assert.Equal(1, await db.Reservas.CountAsync());
    }

    [Fact]
    public async Task Post_ConHorarioDespuesDeQueTermineLaAnterior_Permite()
    {
        await using var db = CreateDb();
        var mesa = await SeedMesaAsync(db);
        var controller = CreateController(db);

        await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Juan Perez", null, 4, new DateTime(2026, 9, 4, 20, 0, 0), 90, null),
            default);

        var result = await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Otro Cliente", null, 2, new DateTime(2026, 9, 4, 21, 30, 0), 60, null),
            default);

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        Assert.Equal(2, await db.Reservas.CountAsync());
    }

    [Fact]
    public async Task Post_ConMesaCancelada_NoBloqueaElHorario()
    {
        await using var db = CreateDb();
        var mesa = await SeedMesaAsync(db);
        var controller = CreateController(db);

        var primera = await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Juan Perez", null, 4, new DateTime(2026, 9, 4, 20, 0, 0), 90, null),
            default);
        var creada = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(primera.Result);
        var dto = Assert.IsType<ReservaDto>(creada.Value);

        await controller.CambiarEstado(dto.Id, new CambiarEstadoReservaRequest("Cancelada"), default);

        var result = await controller.Post(
            new UpsertReservaRequest(mesa.Id, "Otro Cliente", null, 2, new DateTime(2026, 9, 4, 20, 30, 0), 60, null),
            default);

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    }
}
