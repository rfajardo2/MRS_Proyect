using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Models;
using MRSDrunk.Api.Services;
using Xunit;

namespace MRSDrunk.Api.Tests.Services;

// Cubre ProcessPayUConfirmationAsync, el punto de entrada del webhook de PayU:
// es el flujo donde un error silencioso puede aprobar un pago que no debia,
// o dejar una cuenta sin cerrar pese a que ya se cobro.
public class PaymentServiceConfirmationTests
{
    private static MrsDrunkDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<MrsDrunkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MrsDrunkDbContext(options);
    }

    private static (Cuenta cuenta, PagoPasarela pago) SeedCuentaConPagoPendiente(MrsDrunkDbContext db, decimal total = 25000m)
    {
        var cuenta = new Cuenta
        {
            EmpresaId = 1,
            SucursalId = 1,
            MeseroId = 10,
            Numero = "C-0001",
            Mesa = "5",
            Estado = "Abierta",
            Total = total,
            Items =
            [
                new CuentaItem { ProductoId = 1, ProductoNombre = "Cerveza", Cantidad = 1, PrecioUnitario = total, Total = total }
            ]
        };
        db.Cuentas.Add(cuenta);
        db.SaveChanges();

        var pago = new PagoPasarela
        {
            EmpresaId = 1,
            SucursalId = 1,
            CuentaId = cuenta.Id,
            Proveedor = "PAYU",
            MetodoPago = "CARD",
            ReferenciaUnica = "BAR-MESA-5-CUENTA-1-REF",
            ValorEsperado = total,
            Moneda = "COP",
            Estado = PaymentGatewayStatuses.Pending,
            UsuarioCreacionId = 10
        };
        db.PagoPasarelas.Add(pago);
        db.SaveChanges();

        return (cuenta, pago);
    }

    private static PaymentService CreateService(MrsDrunkDbContext db, Mock<IPaymentGatewayAdapter> adapter, Mock<IInventarioService>? inventario = null)
    {
        inventario ??= new Mock<IInventarioService>();
        adapter.SetupGet(x => x.Proveedor).Returns("PAYU");
        return new PaymentService(db, inventario.Object, adapter.Object, NullLogger<PaymentService>.Instance);
    }

    [Fact]
    public async Task Confirmacion_ConFirmaInvalida_NoAprueboElPagoYQuedaMarcadoElError()
    {
        await using var db = CreateDb();
        var (_, pago) = SeedCuentaConPagoPendiente(db);
        var adapter = new Mock<IPaymentGatewayAdapter>();
        adapter.Setup(x => x.ParseWebhookAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayWebhookResult(
                pago.ReferenciaUnica, "trx-1", "order-1", "4", PaymentGatewayStatuses.Approved,
                "COP", pago.ValorEsperado, "firma-cualquiera", SignatureValid: false, MensajeError: null));
        var service = CreateService(db, adapter);

        var (processed, message) = await service.ProcessPayUConfirmationAsync(new Dictionary<string, string>(), "raw", default);

        Assert.False(processed);
        Assert.Equal("Invalid signature", message);
        var pagoActualizado = await db.PagoPasarelas.AsNoTracking().FirstAsync(x => x.Id == pago.Id);
        Assert.Equal(PaymentGatewayStatuses.Pending, pagoActualizado.Estado); // no se toco el estado del pago
    }

    [Fact]
    public async Task Confirmacion_ConMonedaDistinta_QuedaEnError()
    {
        await using var db = CreateDb();
        var (_, pago) = SeedCuentaConPagoPendiente(db);
        var adapter = new Mock<IPaymentGatewayAdapter>();
        adapter.Setup(x => x.ParseWebhookAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayWebhookResult(
                pago.ReferenciaUnica, "trx-1", "order-1", "4", PaymentGatewayStatuses.Approved,
                "USD", pago.ValorEsperado, "firma", SignatureValid: true, MensajeError: null));
        var service = CreateService(db, adapter);

        var (processed, message) = await service.ProcessPayUConfirmationAsync(new Dictionary<string, string>(), "raw", default);

        Assert.False(processed);
        Assert.Equal("Currency mismatch", message);
        var pagoActualizado = await db.PagoPasarelas.AsNoTracking().FirstAsync(x => x.Id == pago.Id);
        Assert.Equal(PaymentGatewayStatuses.Error, pagoActualizado.Estado);
    }

    [Fact]
    public async Task Confirmacion_ConValorDistintoAlEsperado_QuedaEnError()
    {
        await using var db = CreateDb();
        var (_, pago) = SeedCuentaConPagoPendiente(db, total: 25000m);
        var adapter = new Mock<IPaymentGatewayAdapter>();
        adapter.Setup(x => x.ParseWebhookAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayWebhookResult(
                pago.ReferenciaUnica, "trx-1", "order-1", "4", PaymentGatewayStatuses.Approved,
                "COP", ValorRecibido: 1m, "firma", SignatureValid: true, MensajeError: null));
        var service = CreateService(db, adapter);

        var (processed, message) = await service.ProcessPayUConfirmationAsync(new Dictionary<string, string>(), "raw", default);

        Assert.False(processed);
        Assert.Equal("Amount mismatch", message);
        var pagoActualizado = await db.PagoPasarelas.AsNoTracking().FirstAsync(x => x.Id == pago.Id);
        Assert.Equal(PaymentGatewayStatuses.Error, pagoActualizado.Estado);
    }

    [Fact]
    public async Task Confirmacion_ValidaYCompleta_AplicaElPagoYCierraLaCuenta()
    {
        await using var db = CreateDb();
        var (cuenta, pago) = SeedCuentaConPagoPendiente(db, total: 25000m);
        var adapter = new Mock<IPaymentGatewayAdapter>();
        adapter.Setup(x => x.ParseWebhookAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayWebhookResult(
                pago.ReferenciaUnica, "trx-1", "order-1", "4", PaymentGatewayStatuses.Approved,
                "COP", ValorRecibido: 25000m, "firma", SignatureValid: true, MensajeError: null));
        var inventario = new Mock<IInventarioService>();
        inventario
            .Setup(x => x.AplicarSalidaVentaAsync(It.IsAny<Cuenta>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService(db, adapter, inventario);

        var (processed, message) = await service.ProcessPayUConfirmationAsync(new Dictionary<string, string>(), "raw", default);

        Assert.True(processed);
        Assert.Equal(PaymentGatewayStatuses.Approved, message);

        var pagoActualizado = await db.PagoPasarelas.AsNoTracking().FirstAsync(x => x.Id == pago.Id);
        Assert.Equal(PaymentGatewayStatuses.Approved, pagoActualizado.Estado);
        Assert.NotNull(pagoActualizado.CuentaPagoId);

        var cuentaActualizada = await db.Cuentas.AsNoTracking().FirstAsync(x => x.Id == cuenta.Id);
        Assert.Equal("Cerrada", cuentaActualizada.Estado);

        inventario.Verify(x => x.AplicarSalidaVentaAsync(It.IsAny<Cuenta>(), 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirmacion_DuplicadaConLaMismaTransaccion_SeIgnoraSinReprocesar()
    {
        await using var db = CreateDb();
        var (_, pago) = SeedCuentaConPagoPendiente(db);
        pago.Estado = PaymentGatewayStatuses.Approved;
        pago.TransaccionPayU = "trx-1";
        await db.SaveChangesAsync();

        var adapter = new Mock<IPaymentGatewayAdapter>();
        adapter.Setup(x => x.ParseWebhookAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayWebhookResult(
                pago.ReferenciaUnica, "trx-1", "order-1", "4", PaymentGatewayStatuses.Approved,
                "COP", pago.ValorEsperado, "firma", SignatureValid: true, MensajeError: null));
        var inventario = new Mock<IInventarioService>(MockBehavior.Strict);
        var service = CreateService(db, adapter, inventario);

        var (processed, message) = await service.ProcessPayUConfirmationAsync(new Dictionary<string, string>(), "raw", default);

        Assert.True(processed);
        Assert.Equal("Duplicated confirmation", message);
        inventario.VerifyNoOtherCalls(); // no debe volver a aplicar inventario/salida
    }

    [Fact]
    public async Task Confirmacion_SinReferencia_NoEncuentraElPagoYNoProcesa()
    {
        await using var db = CreateDb();
        var adapter = new Mock<IPaymentGatewayAdapter>();
        adapter.Setup(x => x.ParseWebhookAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayWebhookResult(
                Referencia: null, "trx-1", "order-1", "4", PaymentGatewayStatuses.Approved,
                "COP", 25000m, "firma", SignatureValid: true, MensajeError: null));
        var service = CreateService(db, adapter);

        var (processed, message) = await service.ProcessPayUConfirmationAsync(new Dictionary<string, string>(), "raw", default);

        Assert.False(processed);
        Assert.Equal("Missing reference", message);
    }
}
