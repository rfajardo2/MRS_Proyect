using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Controllers;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;
using Xunit;

namespace MRSDrunk.Api.Tests.Controllers;

public class PendientesControllerTests
{
    private static MrsDrunkDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<MrsDrunkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MrsDrunkDbContext(options);
    }

    private static PendientesController CreateController(MrsDrunkDbContext db, int empresaId)
    {
        var identity = new ClaimsIdentity([new Claim("empresaId", empresaId.ToString())], authenticationType: "Test");
        var controller = new PendientesController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        return controller;
    }

    [Fact]
    public async Task Get_CuentaSoloLoQueEstaAbiertoPendienteYSinConfirmarDeEsaEmpresa()
    {
        await using var db = CreateDb();

        // Empresa 1: lo que si debe contar.
        db.Cuentas.Add(new Cuenta { EmpresaId = 1, MeseroId = 1, Numero = "C-1", Estado = "Abierta" });
        db.Cuentas.Add(new Cuenta { EmpresaId = 1, MeseroId = 1, Numero = "C-2", Estado = "Cerrada" }); // no cuenta
        var comanda = new Comanda { EmpresaId = 1, CuentaId = 1, Numero = "CMD-1", MeseroId = 1, Estado = "PENDIENTE" };
        db.Comandas.Add(comanda);
        db.SaveChanges();
        db.ComandaDetalles.Add(new ComandaDetalle { ComandaId = comanda.Id, CuentaItemId = 1, ProductoId = 1, ProductoNombre = "Cerveza", Cantidad = 1, Estado = "PENDIENTE" });
        db.ComandaDetalles.Add(new ComandaDetalle { ComandaId = comanda.Id, CuentaItemId = 2, ProductoId = 1, ProductoNombre = "Cerveza", Cantidad = 1, Estado = "LISTO" }); // no cuenta
        db.Mesas.Add(new Mesa { Id = 1, EmpresaId = 1, Nombre = "Mesa 1" });
        db.Reservas.Add(new Reserva { EmpresaId = 1, MesaId = 1, Cliente = "Juan", FechaHora = DateTime.UtcNow, Estado = "Pendiente", UsuarioCreacionId = 1 });
        db.Reservas.Add(new Reserva { EmpresaId = 1, MesaId = 1, Cliente = "Ana", FechaHora = DateTime.UtcNow.AddDays(2), Estado = "Pendiente", UsuarioCreacionId = 1 }); // no es hoy

        // Empresa 2: no debe filtrarse hacia la empresa 1.
        db.Cuentas.Add(new Cuenta { EmpresaId = 2, MeseroId = 1, Numero = "C-3", Estado = "Abierta" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, empresaId: 1);

        var result = await controller.Get(default);

        var dto = Assert.IsType<PendientesResumenDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(1, dto.CuentasAbiertas);
        Assert.Equal(1, dto.ComandasPendientes);
        Assert.Equal(1, dto.ReservasHoySinConfirmar);
    }
}
