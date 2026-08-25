using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;
using MRSDrunk.Api.Services;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed partial class PaymentsController(IPaymentService paymentService, MrsDrunkDbContext db) : ControllerBase
{
    private static readonly Regex CuentaIdRegex = new("-CUENTA-(\\d+)-", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [HttpPost("create")]
    [Authorize]
    [RequirePermission("Operacion.Cuentas.Editar")]
    public async Task<ActionResult<PaymentSessionDto>> Create(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var session = await paymentService.CreateAsync(
            User.GetEmpresaId(),
            User.GetSucursalId(),
            User.GetUsuarioId(),
            request,
            cancellationToken);
        return Ok(session);
    }

    [HttpGet("{paymentId:int}/status")]
    [Authorize]
    [RequirePermission("Operacion.Cuentas.Ver")]
    public async Task<ActionResult<PaymentStatusDto>> Status(int paymentId, CancellationToken cancellationToken)
    {
        var status = await paymentService.GetStatusAsync(User.GetEmpresaId(), User.GetUsuarioId(), paymentId, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpGet("public/{paymentId:int}/status")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentStatusDto>> PublicStatus(int paymentId, [FromQuery] string? reference, CancellationToken cancellationToken)
    {
        var status = await paymentService.GetPublicStatusAsync(paymentId, reference, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpGet("{paymentId:int}/checkout")]
    [AllowAnonymous]
    public async Task<IActionResult> Checkout(int paymentId, [FromQuery] int? empresaId, CancellationToken cancellationToken)
    {
        var resolvedEmpresaId = empresaId.GetValueOrDefault();
        if (resolvedEmpresaId <= 0 && User.Identity?.IsAuthenticated == true)
        {
            resolvedEmpresaId = User.GetEmpresaId();
        }

        if (resolvedEmpresaId <= 0)
        {
            return BadRequest("Debes indicar la empresa del pago.");
        }

        var html = await paymentService.BuildCheckoutHtmlAsync(resolvedEmpresaId, paymentId, cancellationToken);
        if (html is null)
        {
            return NotFound();
        }

        return Content(html, "text/html", Encoding.UTF8);
    }

    [HttpGet("admin")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Ver")]
    public async Task<ActionResult<PaymentAdminDashboardDto>> AdminDashboard(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? usuarioId,
        [FromQuery] string? estado,
        [FromQuery] string? metodo,
        [FromQuery] string? texto,
        [FromQuery] bool soloDiscrepancias,
        CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var turno = await db.CajaTurnos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && x.Estado == "Abierta")
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(cancellationToken);

        var resolvedDesde = NormalizeRangeStart(desde ?? turno?.FechaApertura ?? DateTime.UtcNow.Date.AddDays(-7));
        var resolvedHasta = NormalizeRangeEnd(hasta ?? DateTime.UtcNow);
        if (resolvedHasta < resolvedDesde)
        {
            return BadRequest("La fecha final no puede ser menor que la fecha inicial.");
        }

        var payments = await db.PagoPasarelas.AsNoTracking()
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Mesero)
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Pagos)
            .Include(x => x.CuentaPago)
            .Include(x => x.Confirmaciones)
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId &&
                x.FechaCreacion >= resolvedDesde &&
                x.FechaCreacion <= resolvedHasta)
            .OrderByDescending(x => x.FechaCreacion)
            .ToListAsync(cancellationToken);

        var recentOrphans = await db.PagoConfirmacionesPayU.AsNoTracking()
            .Where(x => x.PagoPasarelaId == null && x.FechaRecepcion >= resolvedDesde && x.FechaRecepcion <= resolvedHasta)
            .OrderByDescending(x => x.FechaRecepcion)
            .Take(120)
            .ToListAsync(cancellationToken);

        var orphanCuentaIds = recentOrphans
            .Select(x => TryParseCuentaIdFromReference(x.Referencia))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var orphanAccounts = orphanCuentaIds.Count == 0
            ? new Dictionary<int, Cuenta>()
            : await db.Cuentas.AsNoTracking()
                .Include(x => x.Mesero)
                .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && orphanCuentaIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var orphanDtos = recentOrphans
            .Select(log =>
            {
                var cuentaId = TryParseCuentaIdFromReference(log.Referencia);
                orphanAccounts.TryGetValue(cuentaId ?? 0, out var cuenta);
                return new PaymentAdminOrphanConfirmationDto(
                    log.Id,
                    log.Referencia,
                    log.TransaccionPayU,
                    log.EstadoRecibido,
                    log.ValorRecibido,
                    log.Moneda,
                    log.FirmaValida,
                    log.Procesado,
                    log.Observacion,
                    log.FechaRecepcion,
                    cuenta?.Id,
                    cuenta?.Numero,
                    cuenta?.Mesa,
                    cuenta?.Cliente,
                    cuenta?.Mesero?.NombreCompleto);
            })
            .Where(x => x.CuentaId.HasValue)
            .ToList();

        var data = payments
            .Select(ToAdminListItem)
            .Where(x => MatchesFilters(x, usuarioId, estado, metodo, texto, soloDiscrepancias))
            .ToList();
        var filteredOrphans = orphanDtos.Where(x => MatchesOrphanFilter(x, texto)).ToList();

        return Ok(new PaymentAdminDashboardDto(resolvedDesde, resolvedHasta, data, filteredOrphans));
    }

    [HttpGet("admin/{paymentId:int}")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Ver")]
    public async Task<ActionResult<PaymentAdminDetailDto>> AdminDetail(int paymentId, CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var payment = await db.PagoPasarelas.AsNoTracking()
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Mesero)
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Items)
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Pagos)
            .Include(x => x.Confirmaciones)
            .Include(x => x.CuentaPago)
            .FirstOrDefaultAsync(x =>
                x.Id == paymentId &&
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId,
                cancellationToken);
        if (payment is null)
        {
            return NotFound();
        }

        var detail = new PaymentAdminDetailDto(
            ToAdminListItem(payment),
            payment.Cuenta is null ? null : OperacionController.ToDto(payment.Cuenta),
            payment.Confirmaciones
                .OrderByDescending(x => x.FechaRecepcion)
                .Select(x => new PaymentAdminConfirmationDto(
                    x.Id,
                    x.Referencia,
                    x.TransaccionPayU,
                    x.EstadoRecibido,
                    x.ValorRecibido,
                    x.Moneda,
                    x.FirmaRecibida,
                    x.FirmaValida,
                    x.Procesado,
                    x.Observacion,
                    x.FechaRecepcion,
                    x.PayloadCompleto))
                .ToList(),
            BuildDuplicateCuentaPagoDtos(payment));

        return Ok(detail);
    }

    [HttpGet("admin/export")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Ver")]
    public async Task<IActionResult> AdminExport(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? usuarioId,
        [FromQuery] string? estado,
        [FromQuery] string? metodo,
        [FromQuery] string? texto,
        [FromQuery] bool soloDiscrepancias,
        CancellationToken cancellationToken)
    {
        var result = await AdminDashboard(desde, hasta, usuarioId, estado, metodo, texto, soloDiscrepancias, cancellationToken);
        if (result.Result is not null)
        {
            return result.Result;
        }

        var data = result.Value ?? new PaymentAdminDashboardDto(DateTime.UtcNow.Date, DateTime.UtcNow, [], []);
        var csv = new StringBuilder();
        csv.AppendLine("FechaCreacion,Cuenta,Usuario,Mesa,Metodo,Estado,Referencia,ValorEsperado,ValorPagado,Confirmaciones,Discrepancias");
        foreach (var payment in data.Payments)
        {
            csv.AppendLine(string.Join(",",
                Csv(payment.FechaCreacion.ToString("s")),
                Csv(payment.CuentaNumero),
                Csv(payment.Mesero),
                Csv(payment.Mesa),
                Csv(payment.MetodoPago),
                Csv(payment.Estado),
                Csv(payment.Referencia),
                Csv(payment.ValorEsperado.ToString("0.00")),
                Csv(payment.ValorPagado.ToString("0.00")),
                Csv(payment.Confirmaciones.ToString()),
                Csv(string.Join(" | ", payment.Discrepancias))));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"payu-trazabilidad-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("admin/executive")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Reportes")]
    public async Task<ActionResult<PaymentExecutiveReportDto>> AdminExecutive(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? usuarioId,
        [FromQuery] string? metodo,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await BuildExecutiveReportAsync(desde, hasta, usuarioId, metodo, estado, cancellationToken);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("admin/executive/export")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Reportes")]
    public async Task<IActionResult> AdminExecutiveExport(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? usuarioId,
        [FromQuery] string? metodo,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await BuildExecutiveReportAsync(desde, hasta, usuarioId, metodo, estado, cancellationToken);
            using var workbook = BuildExecutiveWorkbook(report);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"payu-reporte-ejecutivo-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("admin/{paymentId:int}/refresh-status")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Conciliar")]
    public async Task<ActionResult<AdminPaymentActionResultDto>> AdminRefreshStatus(int paymentId, CancellationToken cancellationToken)
    {
        var result = await paymentService.RefreshAdminStatusAsync(User.GetEmpresaId(), User.GetSucursalId(), paymentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("admin/{paymentId:int}/reprocess-approved")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Conciliar")]
    public async Task<ActionResult<AdminPaymentActionResultDto>> AdminReprocessApproved(int paymentId, CancellationToken cancellationToken)
    {
        var result = await paymentService.ReprocessApprovedAsync(User.GetEmpresaId(), User.GetSucursalId(), paymentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("admin/{paymentId:int}/cancel-pending")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Conciliar")]
    public async Task<ActionResult<AdminPaymentActionResultDto>> AdminCancelPending(int paymentId, CancellationToken cancellationToken)
    {
        var result = await paymentService.CancelPendingAsync(User.GetEmpresaId(), User.GetSucursalId(), paymentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("admin/{paymentId:int}/reconcile-duplicates")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Conciliar")]
    public async Task<ActionResult<PaymentReconciliationResultDto>> AdminReconcileDuplicates(int paymentId, CancellationToken cancellationToken)
    {
        var result = await paymentService.ReconcileDuplicateCuentaPagosAsync(User.GetEmpresaId(), User.GetSucursalId(), paymentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("admin/reconcile-duplicates-bulk")]
    [Authorize]
    [RequirePermission("Operacion.PagosPayU.Conciliar")]
    public async Task<ActionResult<BulkPaymentReconcileResultDto>> AdminReconcileDuplicatesBulk(
        BulkPaymentReconcileRequest request,
        CancellationToken cancellationToken)
    {
        var ids = request.PaymentIds ?? [];
        var result = await paymentService.ReconcileDuplicateCuentaPagosBulkAsync(
            User.GetEmpresaId(),
            User.GetSucursalId(),
            ids,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("payu/confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> PayUConfirmation(CancellationToken cancellationToken)
    {
        var payload = await ReadRawBodyAsync();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            foreach (var item in form)
            {
                values[item.Key] = item.Value.ToString();
            }
        }
        else
        {
            foreach (var pair in Request.Query)
            {
                values[pair.Key] = pair.Value.ToString();
            }
        }

        var result = await paymentService.ProcessPayUConfirmationAsync(values, payload, cancellationToken);
        return Ok(new { processed = result.Processed, message = result.Message });
    }

    private async Task<string> ReadRawBodyAsync()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return payload;
    }

    private static PaymentAdminListItemDto ToAdminListItem(PagoPasarela payment)
    {
        var discrepancies = BuildDiscrepancies(payment);
        var cuentaCerrada = string.Equals(payment.Cuenta?.Estado, "Cerrada", StringComparison.OrdinalIgnoreCase);
        return new PaymentAdminListItemDto(
            payment.Id,
            payment.CuentaId,
            payment.Cuenta?.Numero ?? $"Cuenta #{payment.CuentaId}",
            payment.Cuenta?.MeseroId ?? 0,
            payment.Cuenta?.Mesero?.NombreCompleto ?? "Sin usuario",
            payment.Cuenta?.Mesa,
            payment.Cuenta?.Cliente,
            payment.MetodoPago,
            payment.Estado,
            payment.ReferenciaUnica,
            payment.ValorEsperado,
            payment.ValorPagado,
            payment.Moneda,
            payment.TransaccionPayU,
            payment.OrderIdPayU,
            payment.FechaCreacion,
            payment.FechaExpiracion,
            payment.FechaConfirmacion,
            payment.CheckoutUrl,
            cuentaCerrada,
            payment.CuentaPagoId.HasValue || payment.CuentaPago is not null,
            payment.Confirmaciones.Count,
            payment.Confirmaciones.Count(x => !x.FirmaValida),
            discrepancies,
            payment.Observacion,
            payment.MensajeError);
    }

    private static IReadOnlyCollection<string> BuildDiscrepancies(PagoPasarela payment)
    {
        var issues = new List<string>();

        if (string.Equals(payment.Estado, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
            (payment.ValorPagado <= 0 || Math.Round(payment.ValorPagado, 2) != Math.Round(payment.ValorEsperado, 2)))
        {
            issues.Add("Valor aprobado distinto al esperado.");
        }

        if (string.Equals(payment.Estado, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
            payment.Cuenta is not null &&
            !string.Equals(payment.Cuenta.Estado, "Cerrada", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Pago aprobado con cuenta aun abierta.");
        }

        if (payment.Confirmaciones.Any(x => !x.FirmaValida))
        {
            issues.Add("Hay confirmaciones con firma invalida.");
        }

        if (payment.Confirmaciones.Any(x => !string.IsNullOrWhiteSpace(x.Moneda) &&
                                           !string.Equals(x.Moneda, payment.Moneda, StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add("Se recibio moneda distinta en confirmacion.");
        }

        if (string.Equals(payment.Estado, "PENDING", StringComparison.OrdinalIgnoreCase) &&
            payment.FechaExpiracion.HasValue &&
            payment.FechaExpiracion.Value < DateTime.UtcNow)
        {
            issues.Add("Intento pendiente ya expirado.");
        }

        if (string.Equals(payment.Estado, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
            !payment.CuentaPagoId.HasValue)
        {
            issues.Add("Pago aprobado sin espejo en CuentaPagos.");
        }

        var duplicateCount = CountDuplicateCuentaPagos(payment);
        if (duplicateCount > 1)
        {
            issues.Add($"Se detectaron {duplicateCount} CuentaPagos con la misma referencia PayU.");
        }

        if (payment.Confirmaciones
            .Where(x => x.Procesado && !string.IsNullOrWhiteSpace(x.TransaccionPayU))
            .GroupBy(x => x.TransaccionPayU)
            .Any(g => g.Count() > 1))
        {
            issues.Add("Se recibieron confirmaciones duplicadas para la misma transaccion.");
        }

        return issues;
    }

    private static int? TryParseCuentaIdFromReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var match = CuentaIdRegex.Match(reference);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[1].Value, out var cuentaId) ? cuentaId : null;
    }

    private static bool MatchesFilters(
        PaymentAdminListItemDto item,
        int? usuarioId,
        string? estado,
        string? metodo,
        string? texto,
        bool soloDiscrepancias)
    {
        var matchesUsuario = !usuarioId.HasValue || item.MeseroId == usuarioId.Value;
        var matchesEstado = string.IsNullOrWhiteSpace(estado) || string.Equals(item.Estado, estado.Trim(), StringComparison.OrdinalIgnoreCase);
        var matchesMetodo = string.IsNullOrWhiteSpace(metodo) || string.Equals(item.MetodoPago, metodo.Trim(), StringComparison.OrdinalIgnoreCase);
        var matchesDiscrepancias = !soloDiscrepancias || item.Discrepancias.Count > 0;
        if (!matchesUsuario || !matchesEstado || !matchesMetodo || !matchesDiscrepancias)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(texto))
        {
            return true;
        }

        var needle = texto.Trim().ToLowerInvariant();
        var searchable = string.Join(" ", new[]
        {
            item.Referencia,
            item.CuentaNumero,
            item.Mesero,
            item.Mesa,
            item.Cliente,
            item.TransaccionPayU,
            item.OrderIdPayU
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        return searchable.Contains(needle, StringComparison.Ordinal);
    }

    private static bool MatchesOrphanFilter(PaymentAdminOrphanConfirmationDto item, string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return true;
        }

        var needle = texto.Trim().ToLowerInvariant();
        var searchable = string.Join(" ", new[]
        {
            item.Referencia,
            item.TransaccionPayU,
            item.CuentaNumero,
            item.Usuario,
            item.Observacion
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        return searchable.Contains(needle, StringComparison.Ordinal);
    }

    private static string Csv(string? value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{safe}\"";
    }

    private async Task<PaymentExecutiveReportDto> BuildExecutiveReportAsync(
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId,
        string? metodo,
        string? estado,
        CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var sucursalId = User.GetSucursalId();
        var turno = await db.CajaTurnos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.SucursalId == sucursalId && x.Estado == "Abierta")
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(cancellationToken);

        var resolvedDesde = NormalizeRangeStart(desde ?? turno?.FechaApertura ?? DateTime.UtcNow.Date.AddDays(-7));
        var resolvedHasta = NormalizeRangeEnd(hasta ?? DateTime.UtcNow);
        if (resolvedHasta < resolvedDesde)
        {
            throw new InvalidOperationException("La fecha final no puede ser menor que la fecha inicial.");
        }

        var payments = await db.PagoPasarelas.AsNoTracking()
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Mesero)
            .Include(x => x.Cuenta)
                .ThenInclude(x => x!.Pagos)
            .Include(x => x.CuentaPago)
            .Include(x => x.Confirmaciones)
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.SucursalId == sucursalId &&
                x.FechaCreacion >= resolvedDesde &&
                x.FechaCreacion <= resolvedHasta)
            .OrderByDescending(x => x.FechaCreacion)
            .ToListAsync(cancellationToken);

        var data = payments
            .Select(ToAdminListItem)
            .Where(x => MatchesFilters(x, usuarioId, estado, metodo, null, false))
            .ToList();

        var orphanConfirmations = await db.PagoConfirmacionesPayU.AsNoTracking()
            .Where(x => x.PagoPasarelaId == null && x.FechaRecepcion >= resolvedDesde && x.FechaRecepcion <= resolvedHasta)
            .CountAsync(cancellationToken);

        var approved = data.Where(x => x.Estado == "APPROVED").ToList();
        var pending = data.Where(x => x.Estado == "PENDING").ToList();
        var failed = data.Where(x => x.Estado is "REJECTED" or "DECLINED" or "ERROR").ToList();
        var expired = data.Count(x => x.Estado == "EXPIRED");
        var cancelled = data.Count(x => x.Estado == "CANCELLED");
        var discrepancyCount = data.Count(x => x.Discrepancias.Count > 0);
        var approvedValue = approved.Sum(x => x.ValorPagado > 0 ? x.ValorPagado : x.ValorEsperado);
        var expectedValue = data.Sum(x => x.ValorEsperado);
        var pendingValue = pending.Sum(x => x.ValorEsperado);
        var headline = new PaymentExecutiveHeadlineDto(
            data.Count,
            approved.Count,
            pending.Count,
            failed.Count,
            expired,
            cancelled,
            discrepancyCount,
            orphanConfirmations,
            expectedValue,
            approvedValue,
            pendingValue,
            approved.Count == 0 ? 0 : decimal.Round(approvedValue / approved.Count, 2),
            data.Count == 0 ? 0 : decimal.Round((decimal)approved.Count * 100m / data.Count, 2));

        var byMethod = data
            .GroupBy(x => x.MetodoPago)
            .Select(g =>
            {
                var items = g.ToList();
                var ok = items.Where(x => x.Estado == "APPROVED").ToList();
                return new PaymentExecutiveMethodDto(
                    g.Key,
                    items.Count,
                    ok.Count,
                    items.Count(x => x.Estado == "PENDING"),
                    items.Count(x => x.Estado is "REJECTED" or "DECLINED" or "ERROR" or "EXPIRED" or "CANCELLED"),
                    items.Sum(x => x.ValorEsperado),
                    ok.Sum(x => x.ValorPagado > 0 ? x.ValorPagado : x.ValorEsperado),
                    items.Count == 0 ? 0 : decimal.Round((decimal)ok.Count * 100m / items.Count, 2),
                    items.Count(x => x.Discrepancias.Count > 0));
            })
            .OrderByDescending(x => x.ValorAprobado)
            .ToList();

        var byUser = data
            .GroupBy(x => new { x.MeseroId, x.Mesero })
            .Select(g =>
            {
                var items = g.ToList();
                var ok = items.Where(x => x.Estado == "APPROVED").ToList();
                return new PaymentExecutiveUserDto(
                    g.Key.MeseroId,
                    g.Key.Mesero,
                    items.Count,
                    ok.Count,
                    items.Count(x => x.Estado == "PENDING"),
                    items.Count(x => x.Estado is "REJECTED" or "DECLINED" or "ERROR" or "EXPIRED" or "CANCELLED"),
                    items.Sum(x => x.ValorEsperado),
                    ok.Sum(x => x.ValorPagado > 0 ? x.ValorPagado : x.ValorEsperado),
                    items.Count == 0 ? 0 : decimal.Round((decimal)ok.Count * 100m / items.Count, 2),
                    items.Count(x => x.Discrepancias.Count > 0));
            })
            .OrderByDescending(x => x.ValorAprobado)
            .ThenBy(x => x.Mesero)
            .ToList();

        var timeline = data
            .GroupBy(x => x.FechaCreacion.Date)
            .Select(g =>
            {
                var items = g.ToList();
                var ok = items.Where(x => x.Estado == "APPROVED").ToList();
                return new PaymentExecutiveTimelineDto(
                    g.Key,
                    items.Count,
                    ok.Count,
                    items.Count(x => x.Estado == "PENDING"),
                    items.Count(x => x.Estado is "REJECTED" or "DECLINED" or "ERROR" or "EXPIRED" or "CANCELLED"),
                    items.Sum(x => x.ValorEsperado),
                    ok.Sum(x => x.ValorPagado > 0 ? x.ValorPagado : x.ValorEsperado));
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        var hallazgos = data
            .SelectMany(x => x.Discrepancias.Select(issue => new { Issue = issue, x.ValorEsperado }))
            .GroupBy(x => x.Issue)
            .Select(g => new PaymentExecutiveIssueDto(g.Key, g.Count(), g.Sum(x => x.ValorEsperado)))
            .OrderByDescending(x => x.Total)
            .ThenByDescending(x => x.ValorComprometido)
            .ToList();

        return new PaymentExecutiveReportDto(
            resolvedDesde,
            resolvedHasta,
            headline,
            byMethod,
            byUser,
            timeline,
            hallazgos,
            data.Where(x => x.Discrepancias.Count > 0).Take(25).ToList());
    }

    private static XLWorkbook BuildExecutiveWorkbook(PaymentExecutiveReportDto report)
    {
        var workbook = new XLWorkbook();
        var moneyFormat = "$#,##0";
        var percentFormat = "0.00%";
        var dateFormat = "yyyy-mm-dd";
        var dateTimeFormat = "yyyy-mm-dd hh:mm";

        var resumen = workbook.Worksheets.Add("Resumen");
        resumen.Cell("A1").Value = "Reporte ejecutivo PayU";
        resumen.Cell("A2").Value = "Desde";
        resumen.Cell("B2").Value = report.Desde;
        resumen.Cell("C2").Value = "Hasta";
        resumen.Cell("D2").Value = report.Hasta;
        var titleRange = resumen.Range("A1:D1");
        titleRange.Merge();
        titleRange.Style.Font.SetBold();
        titleRange.Style.Font.FontSize = 16;
        resumen.Range("A2:D2").Style.Font.SetBold();

        var headlineLabels = new[]
        {
            "Intentos",
            "Aprobados",
            "Pendientes",
            "Fallidos",
            "Expirados",
            "Cancelados",
            "Discrepancias",
            "Confirmaciones huerfanas",
            "Valor esperado",
            "Valor aprobado",
            "Valor pendiente",
            "Ticket promedio aprobado",
            "Tasa de aprobacion"
        };

        for (var i = 0; i < headlineLabels.Length; i++)
        {
            resumen.Cell(i + 4, 1).Value = headlineLabels[i];
        }

        resumen.Cell("B4").Value = report.Headline.Intentos;
        resumen.Cell("B5").Value = report.Headline.Aprobados;
        resumen.Cell("B6").Value = report.Headline.Pendientes;
        resumen.Cell("B7").Value = report.Headline.Fallidos;
        resumen.Cell("B8").Value = report.Headline.Expirados;
        resumen.Cell("B9").Value = report.Headline.Cancelados;
        resumen.Cell("B10").Value = report.Headline.Discrepancias;
        resumen.Cell("B11").Value = report.Headline.ConfirmacionesHuerfanas;
        resumen.Cell("B12").Value = report.Headline.ValorEsperado;
        resumen.Cell("B13").Value = report.Headline.ValorAprobado;
        resumen.Cell("B14").Value = report.Headline.ValorPendiente;
        resumen.Cell("B15").Value = report.Headline.TicketPromedioAprobado;
        resumen.Cell("B16").Value = report.Headline.TasaAprobacion / 100m;

        resumen.Range("A4:B16").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        resumen.Range("A4:B16").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        resumen.Range("A4:A16").Style.Font.SetBold();
        resumen.Cell("B12").Style.NumberFormat.Format = moneyFormat;
        resumen.Cell("B13").Style.NumberFormat.Format = moneyFormat;
        resumen.Cell("B14").Style.NumberFormat.Format = moneyFormat;
        resumen.Cell("B15").Style.NumberFormat.Format = moneyFormat;
        resumen.Cell("B16").Style.NumberFormat.Format = percentFormat;
        resumen.Columns().AdjustToContents();

        WriteMethodSheet(workbook.Worksheets.Add("Metodos"), report.Metodos, moneyFormat, percentFormat);
        WriteUserSheet(workbook.Worksheets.Add("Usuarios"), report.Usuarios, moneyFormat, percentFormat);
        WriteTimelineSheet(workbook.Worksheets.Add("Tendencia"), report.Timeline, moneyFormat, dateFormat);
        WriteIssuesSheet(workbook.Worksheets.Add("Hallazgos"), report.Hallazgos, moneyFormat);
        WriteIncidentsSheet(workbook.Worksheets.Add("Incidencias"), report.PagosConIncidencias, moneyFormat, dateTimeFormat);

        foreach (var sheet in workbook.Worksheets)
        {
            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
        }

        return workbook;
    }

    private static void WriteMethodSheet(IXLWorksheet sheet, IReadOnlyCollection<PaymentExecutiveMethodDto> items, string moneyFormat, string percentFormat)
    {
        var headers = new[] { "Metodo", "Intentos", "Aprobados", "Pendientes", "Fallidos", "Valor esperado", "Valor aprobado", "Tasa aprobacion", "Discrepancias" };
        WriteHeaderRow(sheet, headers);
        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.MetodoPago;
            sheet.Cell(row, 2).Value = item.Intentos;
            sheet.Cell(row, 3).Value = item.Aprobados;
            sheet.Cell(row, 4).Value = item.Pendientes;
            sheet.Cell(row, 5).Value = item.Fallidos;
            sheet.Cell(row, 6).Value = item.ValorEsperado;
            sheet.Cell(row, 7).Value = item.ValorAprobado;
            sheet.Cell(row, 8).Value = item.TasaAprobacion / 100m;
            sheet.Cell(row, 9).Value = item.Discrepancias;
            row++;
        }
        if (row > 2)
        {
            sheet.Range(2, 6, row - 1, 7).Style.NumberFormat.Format = moneyFormat;
            sheet.Range(2, 8, row - 1, 8).Style.NumberFormat.Format = percentFormat;
        }
    }

    private static void WriteUserSheet(IXLWorksheet sheet, IReadOnlyCollection<PaymentExecutiveUserDto> items, string moneyFormat, string percentFormat)
    {
        var headers = new[] { "Usuario", "Intentos", "Aprobados", "Pendientes", "Fallidos", "Valor esperado", "Valor aprobado", "Tasa aprobacion", "Discrepancias" };
        WriteHeaderRow(sheet, headers);
        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Mesero;
            sheet.Cell(row, 2).Value = item.Intentos;
            sheet.Cell(row, 3).Value = item.Aprobados;
            sheet.Cell(row, 4).Value = item.Pendientes;
            sheet.Cell(row, 5).Value = item.Fallidos;
            sheet.Cell(row, 6).Value = item.ValorEsperado;
            sheet.Cell(row, 7).Value = item.ValorAprobado;
            sheet.Cell(row, 8).Value = item.TasaAprobacion / 100m;
            sheet.Cell(row, 9).Value = item.Discrepancias;
            row++;
        }
        if (row > 2)
        {
            sheet.Range(2, 6, row - 1, 7).Style.NumberFormat.Format = moneyFormat;
            sheet.Range(2, 8, row - 1, 8).Style.NumberFormat.Format = percentFormat;
        }
    }

    private static void WriteTimelineSheet(IXLWorksheet sheet, IReadOnlyCollection<PaymentExecutiveTimelineDto> items, string moneyFormat, string dateFormat)
    {
        var headers = new[] { "Fecha", "Intentos", "Aprobados", "Pendientes", "Fallidos", "Valor esperado", "Valor aprobado" };
        WriteHeaderRow(sheet, headers);
        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Fecha;
            sheet.Cell(row, 2).Value = item.Intentos;
            sheet.Cell(row, 3).Value = item.Aprobados;
            sheet.Cell(row, 4).Value = item.Pendientes;
            sheet.Cell(row, 5).Value = item.Fallidos;
            sheet.Cell(row, 6).Value = item.ValorEsperado;
            sheet.Cell(row, 7).Value = item.ValorAprobado;
            row++;
        }
        if (row > 2)
        {
            sheet.Range(2, 1, row - 1, 1).Style.DateFormat.Format = dateFormat;
            sheet.Range(2, 6, row - 1, 7).Style.NumberFormat.Format = moneyFormat;
        }
    }

    private static void WriteIssuesSheet(IXLWorksheet sheet, IReadOnlyCollection<PaymentExecutiveIssueDto> items, string moneyFormat)
    {
        var headers = new[] { "Hallazgo", "Total", "Valor comprometido" };
        WriteHeaderRow(sheet, headers);
        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Tipo;
            sheet.Cell(row, 2).Value = item.Total;
            sheet.Cell(row, 3).Value = item.ValorComprometido;
            row++;
        }
        if (row > 2)
        {
            sheet.Range(2, 3, row - 1, 3).Style.NumberFormat.Format = moneyFormat;
        }
    }

    private static void WriteIncidentsSheet(IXLWorksheet sheet, IReadOnlyCollection<PaymentAdminListItemDto> items, string moneyFormat, string dateTimeFormat)
    {
        var headers = new[] { "Fecha", "Cuenta", "Usuario", "Mesa", "Metodo", "Estado", "Referencia", "Esperado", "Pagado", "Discrepancias" };
        WriteHeaderRow(sheet, headers);
        var row = 2;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.FechaCreacion;
            sheet.Cell(row, 2).Value = item.CuentaNumero;
            sheet.Cell(row, 3).Value = item.Mesero;
            sheet.Cell(row, 4).Value = item.Mesa ?? string.Empty;
            sheet.Cell(row, 5).Value = item.MetodoPago;
            sheet.Cell(row, 6).Value = item.Estado;
            sheet.Cell(row, 7).Value = item.Referencia;
            sheet.Cell(row, 8).Value = item.ValorEsperado;
            sheet.Cell(row, 9).Value = item.ValorPagado;
            sheet.Cell(row, 10).Value = string.Join(" | ", item.Discrepancias);
            row++;
        }
        if (row > 2)
        {
            sheet.Range(2, 1, row - 1, 1).Style.DateFormat.Format = dateTimeFormat;
            sheet.Range(2, 8, row - 1, 9).Style.NumberFormat.Format = moneyFormat;
        }
    }

    private static void WriteHeaderRow(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var range = sheet.Range(1, 1, 1, headers.Count);
        range.Style.Font.SetBold();
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f2128");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static DateTime NormalizeRangeStart(DateTime value) =>
        value.TimeOfDay == TimeSpan.Zero ? value.Date : value;

    private static DateTime NormalizeRangeEnd(DateTime value) =>
        value.TimeOfDay == TimeSpan.Zero ? value.Date.AddDays(1).AddTicks(-1) : value;

    private static int CountDuplicateCuentaPagos(PagoPasarela payment) =>
        payment.Cuenta?.Pagos.Count(x =>
            string.Equals(x.Origen, "PAYU", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(x.Estado, "ANULADO_DUPLICADO_PAYU", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Referencia, payment.ReferenciaUnica, StringComparison.OrdinalIgnoreCase)) ?? 0;

    private static IReadOnlyCollection<PaymentAdminCuentaPagoDuplicateDto> BuildDuplicateCuentaPagoDtos(PagoPasarela payment)
    {
        if (payment.Cuenta?.Pagos is null)
        {
            return [];
        }

        var canonicalId = payment.CuentaPagoId
            ?? payment.Cuenta.Pagos
                .Where(x =>
                    string.Equals(x.Origen, "PAYU", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Referencia, payment.ReferenciaUnica, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.PagoPasarelaId == payment.Id)
                .ThenBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();

        return payment.Cuenta.Pagos
            .Where(x =>
                string.Equals(x.Origen, "PAYU", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Referencia, payment.ReferenciaUnica, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Id)
            .Select(x => new PaymentAdminCuentaPagoDuplicateDto(
                x.Id,
                x.CuentaId,
                x.Valor,
                x.ValorPropina,
                x.Estado,
                x.MetodoPago,
                x.Origen,
                x.Referencia,
                x.PagoPasarelaId,
                x.FechaPago,
                canonicalId.HasValue && canonicalId.Value == x.Id))
            .ToList();
    }
}
