using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.Services;
using Xunit;

namespace MRSDrunk.Api.Tests.Services;

public class AuditoriaServiceTests
{
    private static MrsDrunkDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<MrsDrunkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MrsDrunkDbContext(options);
    }

    [Fact]
    public async Task RegistrarAsync_GuardaUnRegistroConLosDatosRecibidos()
    {
        await using var db = CreateDb();
        var service = new AuditoriaService(db, NullLogger<AuditoriaService>.Instance);

        await service.RegistrarAsync(
            empresaId: 1, sucursalId: 2, usuarioId: 3,
            entidad: "Producto", entidadId: "10", accion: "Editar",
            detalle: "Cambio de precio", cancellationToken: default);

        var registro = await db.RegistrosAuditoria.SingleAsync();
        Assert.Equal(1, registro.EmpresaId);
        Assert.Equal(2, registro.SucursalId);
        Assert.Equal(3, registro.UsuarioId);
        Assert.Equal("Producto", registro.Entidad);
        Assert.Equal("10", registro.EntidadId);
        Assert.Equal("Editar", registro.Accion);
        Assert.Equal("Cambio de precio", registro.Detalle);
    }

    [Fact]
    public async Task RegistrarAsync_ConSucursalNula_LaGuardaComoNull()
    {
        await using var db = CreateDb();
        var service = new AuditoriaService(db, NullLogger<AuditoriaService>.Instance);

        await service.RegistrarAsync(
            empresaId: 1, sucursalId: null, usuarioId: 3,
            entidad: "Usuario", entidadId: "5", accion: "Crear",
            detalle: null, cancellationToken: default);

        var registro = await db.RegistrosAuditoria.SingleAsync();
        Assert.Null(registro.SucursalId);
        Assert.Null(registro.Detalle);
    }
}
