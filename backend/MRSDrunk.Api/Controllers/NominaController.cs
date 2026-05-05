using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Helpers;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NominaController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet("empleados")]
    [RequirePermission("Nomina.Control.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<NominaEmpleadoDto>>> GetEmpleados(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var data = await db.NominaEmpleados.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.NombreCompleto)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost("empleados")]
    [RequirePermission("Nomina.Control.Crear")]
    public async Task<ActionResult<NominaEmpleadoDto>> CrearEmpleado(UpsertNominaEmpleadoRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateEmpleado(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var entity = new NominaEmpleado
        {
            EmpresaId = User.GetEmpresaId()
        };
        MapEmpleado(entity, request);

        db.NominaEmpleados.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPut("empleados/{id:int}")]
    [RequirePermission("Nomina.Control.Editar")]
    public async Task<IActionResult> EditarEmpleado(int id, UpsertNominaEmpleadoRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateEmpleado(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var entity = await db.NominaEmpleados.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        MapEmpleado(entity, request);
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("empleados/{id:int}")]
    [RequirePermission("Nomina.Control.Eliminar")]
    public async Task<IActionResult> ToggleEmpleado(int id, CancellationToken cancellationToken)
    {
        var entity = await db.NominaEmpleados.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Estado = !entity.Estado;
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("periodos")]
    [RequirePermission("Nomina.Control.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<NominaPeriodoDto>>> GetPeriodos(CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var data = await db.NominaPeriodos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderByDescending(x => x.Anio)
            .ThenByDescending(x => x.Mes)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost("periodos")]
    [RequirePermission("Nomina.Control.Crear")]
    public async Task<ActionResult<NominaPeriodoDto>> CrearPeriodo(UpsertNominaPeriodoRequest request, CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var exists = await db.NominaPeriodos.AnyAsync(x => x.EmpresaId == empresaId && x.Anio == request.Anio && x.Mes == request.Mes, cancellationToken);
        if (exists)
        {
            return BadRequest(new { message = "Ya existe un periodo para ese mes." });
        }

        var defaultInicio = new DateTime(request.Anio, request.Mes, 1);
        var fechaInicio = (request.FechaInicio ?? defaultInicio).Date;
        var fechaFin = (request.FechaFin ?? defaultInicio.AddMonths(1).AddDays(-1)).Date;
        if (fechaFin < fechaInicio)
        {
            return BadRequest(new { message = "La fecha final no puede ser menor que la fecha inicial." });
        }

        var entity = new NominaPeriodo
        {
            EmpresaId = empresaId,
            Anio = request.Anio,
            Mes = request.Mes,
            Nombre = string.IsNullOrWhiteSpace(request.Nombre)
                ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(defaultInicio.ToString("MMMM yyyy", new CultureInfo("es-CO")))
                : request.Nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Estado = true
        };

        db.NominaPeriodos.Add(entity);
        SyncDiasNoLaborados(entity, request.DiasNoLaborados);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPut("periodos/{id:int}")]
    [RequirePermission("Nomina.Control.Editar")]
    public async Task<IActionResult> EditarPeriodo(int id, UpsertNominaPeriodoRequest request, CancellationToken cancellationToken)
    {
        var periodo = await db.NominaPeriodos
            .Include(x => x.DiasNoLaborados)
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == User.GetEmpresaId(), cancellationToken);

        if (periodo is null)
        {
            return NotFound();
        }

        if (periodo.Cerrado)
        {
            return BadRequest(new { message = "El periodo esta cerrado." });
        }

        var defaultInicio = new DateTime(request.Anio, request.Mes, 1);
        var fechaInicio = (request.FechaInicio ?? defaultInicio).Date;
        var fechaFin = (request.FechaFin ?? defaultInicio.AddMonths(1).AddDays(-1)).Date;
        if (fechaFin < fechaInicio)
        {
            return BadRequest(new { message = "La fecha final no puede ser menor que la fecha inicial." });
        }

        periodo.Anio = request.Anio;
        periodo.Mes = request.Mes;
        periodo.Nombre = string.IsNullOrWhiteSpace(request.Nombre)
            ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(defaultInicio.ToString("MMMM yyyy", new CultureInfo("es-CO")))
            : request.Nombre;
        periodo.FechaInicio = fechaInicio;
        periodo.FechaFin = fechaFin;
        periodo.FechaModificacion = DateTime.UtcNow;

        db.NominaPeriodoDiasNoLaborados.RemoveRange(periodo.DiasNoLaborados);
        SyncDiasNoLaborados(periodo, request.DiasNoLaborados);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("control/{periodoId:int}")]
    [RequirePermission("Nomina.Control.Consultar")]
    public async Task<ActionResult<NominaControlDto>> GetControl(int periodoId, CancellationToken cancellationToken)
    {
        var empresaId = User.GetEmpresaId();
        var periodo = await db.NominaPeriodos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == periodoId && x.EmpresaId == empresaId, cancellationToken);

        if (periodo is null)
        {
            return NotFound();
        }

        var todosEmpleados = await db.NominaEmpleados.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.NombreCompleto)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        var empleados = todosEmpleados.Where(x => x.Estado).ToList();

        var registros = await db.NominaRegistros.AsNoTracking()
            .Where(x => x.PeriodoId == periodoId)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        var diasNoLaborados = await db.NominaPeriodoDiasNoLaborados.AsNoTracking()
            .Where(x => x.PeriodoId == periodoId)
            .Select(x => new NominaDiaNoLaboradoDto(x.Id, x.Fecha, x.Motivo))
            .ToListAsync(cancellationToken);

        var noLaboradosMap = diasNoLaborados.ToDictionary(x => x.Fecha.Date, x => x.Motivo);

        var dias = Enumerable.Range(0, (periodo.FechaFin - periodo.FechaInicio).Days + 1)
            .Select(i => periodo.FechaInicio.AddDays(i))
            .Select(fecha => new NominaDiaDto(
                fecha,
                fecha.ToString("dddd", new CultureInfo("es-CO")),
                noLaboradosMap.ContainsKey(fecha.Date),
                noLaboradosMap.TryGetValue(fecha.Date, out var motivo) ? motivo : null))
            .ToList();

        var totals = registros
            .GroupBy(x => x.EmpleadoId)
            .Select(g =>
            {
                var empleado = empleados.FirstOrDefault(e => e.Id == g.Key);
                return new NominaEmpleadoTotalDto(g.Key, empleado?.NombreCompleto ?? "Empleado", g.Sum(SignedValor));
            })
            .OrderBy(x => x.NombreCompleto)
            .ToList();

        return Ok(new NominaControlDto(ToDto(periodo), empleados, todosEmpleados, dias, diasNoLaborados, registros, totals, registros.Sum(SignedValor)));
    }

    [HttpPut("control/{periodoId:int}/registro")]
    [RequirePermission("Nomina.Control.Editar")]
    public async Task<IActionResult> GuardarRegistro(int periodoId, UpsertNominaRegistroRequest request, CancellationToken cancellationToken)
    {
        var periodo = await db.NominaPeriodos.FirstOrDefaultAsync(x => x.Id == periodoId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (periodo is null)
        {
            return NotFound();
        }

        if (periodo.Cerrado)
        {
            return BadRequest(new { message = "El periodo esta cerrado." });
        }

        var validationError = ValidateRegistro(periodo, request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var fecha = request.Fecha.Date;
        var codigo = Clean(request.CodigoNovedad);
        var concepto = TextOrDefault(request.Concepto, "Dia");
        var entity = await db.NominaRegistros.FirstOrDefaultAsync(x =>
            x.PeriodoId == periodoId &&
            x.EmpleadoId == request.EmpleadoId &&
            x.Fecha == fecha &&
            x.Concepto == concepto &&
            x.CodigoNovedad == codigo,
            cancellationToken);

        if (entity is null)
        {
            entity = new NominaRegistro
            {
                PeriodoId = periodoId,
                EmpleadoId = request.EmpleadoId,
                Fecha = fecha,
                Concepto = concepto,
                CodigoNovedad = codigo
            };
            db.NominaRegistros.Add(entity);
        }

        entity.FechaFin = request.FechaFin?.Date;
        entity.Concepto = concepto;
        entity.TipoNovedad = Clean(request.TipoNovedad);
        entity.CodigoNovedad = codigo;
        entity.EstadoDia = TextOrDefault(request.EstadoDia, concepto == "Dia" ? "Trabajado" : "Novedad");
        entity.Horas = request.Horas;
        entity.Porcentaje = request.Porcentaje;
        entity.BaseCalculo = request.BaseCalculo;
        entity.Valor = request.Valor;
        entity.Observacion = Clean(request.Observacion);
        entity.FechaModificacion = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("control/{periodoId:int}/registro/{registroId:int}")]
    [RequirePermission("Nomina.Control.Eliminar")]
    public async Task<IActionResult> EliminarRegistro(int periodoId, int registroId, CancellationToken cancellationToken)
    {
        var periodo = await db.NominaPeriodos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == periodoId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);

        if (periodo is null)
        {
            return NotFound();
        }

        if (periodo.Cerrado)
        {
            return BadRequest(new { message = "El periodo esta cerrado." });
        }

        var registro = await db.NominaRegistros.FirstOrDefaultAsync(x => x.Id == registroId && x.PeriodoId == periodoId, cancellationToken);
        if (registro is null)
        {
            return NotFound();
        }

        db.NominaRegistros.Remove(registro);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("control/{periodoId:int}/cerrar")]
    [RequirePermission("Nomina.Control.CerrarPeriodo")]
    public async Task<IActionResult> CerrarPeriodo(int periodoId, CancellationToken cancellationToken)
    {
        var periodo = await db.NominaPeriodos.FirstOrDefaultAsync(x => x.Id == periodoId && x.EmpresaId == User.GetEmpresaId(), cancellationToken);
        if (periodo is null)
        {
            return NotFound();
        }

        periodo.Cerrado = true;
        periodo.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static NominaEmpleadoDto ToDto(NominaEmpleado x) => new(
        x.Id,
        x.EmpresaId,
        x.TipoDocumento,
        x.NumeroDocumento,
        x.FechaExpedicionDocumento,
        x.PrimerNombre,
        x.SegundoNombre,
        x.PrimerApellido,
        x.SegundoApellido,
        x.NombreCompleto,
        x.Documento,
        x.FechaNacimiento,
        x.Genero,
        x.EstadoCivil,
        x.Telefono,
        x.Correo,
        x.Direccion,
        x.Departamento,
        x.Municipio,
        x.Pais,
        x.Cargo,
        x.FechaIngreso,
        x.FechaRetiro,
        x.TipoContrato,
        x.TipoTrabajador,
        x.SubtipoCotizante,
        x.TipoSalario,
        x.SalarioIntegral,
        x.SalarioBase,
        x.ValorDiaBase,
        x.PeriodicidadPago,
        x.JornadaLaboral,
        x.Eps,
        x.FondoPension,
        x.FondoCesantias,
        x.Arl,
        x.NivelRiesgoArl,
        x.CajaCompensacion,
        x.Banco,
        x.TipoCuenta,
        x.NumeroCuenta,
        x.ContactoEmergenciaNombre,
        x.ContactoEmergenciaTelefono,
        x.Observaciones,
        x.Estado);

    private static NominaPeriodoDto ToDto(NominaPeriodo x) => new(x.Id, x.EmpresaId, x.Anio, x.Mes, x.Nombre, x.FechaInicio, x.FechaFin, (x.FechaFin.Date - x.FechaInicio.Date).Days + 1, x.Cerrado, x.Estado);

    private static NominaRegistroDto ToDto(NominaRegistro x) => new(
        x.Id,
        x.PeriodoId,
        x.EmpleadoId,
        x.Fecha,
        x.FechaFin,
        x.Concepto,
        x.TipoNovedad,
        x.CodigoNovedad,
        x.EstadoDia,
        x.Horas,
        x.Porcentaje,
        x.BaseCalculo,
        x.Valor,
        x.Observacion);

    private static void SyncDiasNoLaborados(NominaPeriodo periodo, IReadOnlyCollection<NominaDiaNoLaboradoDto>? dias)
    {
        foreach (var dia in dias ?? [])
        {
            var fecha = dia.Fecha.Date;
            if (fecha < periodo.FechaInicio.Date || fecha > periodo.FechaFin.Date)
            {
                continue;
            }

            periodo.DiasNoLaborados.Add(new NominaPeriodoDiaNoLaborado
            {
                Fecha = fecha,
                Motivo = dia.Motivo
            });
        }
    }

    private static void MapEmpleado(NominaEmpleado entity, UpsertNominaEmpleadoRequest request)
    {
        entity.TipoDocumento = TextOrDefault(request.TipoDocumento, "CC");
        entity.NumeroDocumento = TextOrDefault(request.NumeroDocumento, request.Documento ?? string.Empty);
        entity.FechaExpedicionDocumento = request.FechaExpedicionDocumento?.Date;
        entity.PrimerNombre = TextOrDefault(request.PrimerNombre, FirstToken(request.NombreCompleto));
        entity.SegundoNombre = Clean(request.SegundoNombre);
        entity.PrimerApellido = TextOrDefault(request.PrimerApellido, LastToken(request.NombreCompleto));
        entity.SegundoApellido = Clean(request.SegundoApellido);
        entity.NombreCompleto = TextOrDefault(request.NombreCompleto, BuildNombreCompleto(request));
        entity.Documento = Clean(request.Documento) ?? entity.NumeroDocumento;
        entity.FechaNacimiento = request.FechaNacimiento?.Date;
        entity.Genero = Clean(request.Genero);
        entity.EstadoCivil = Clean(request.EstadoCivil);
        entity.Telefono = Clean(request.Telefono);
        entity.Correo = Clean(request.Correo);
        entity.Direccion = Clean(request.Direccion);
        entity.Departamento = Clean(request.Departamento);
        entity.Municipio = Clean(request.Municipio);
        entity.Pais = TextOrDefault(request.Pais, "CO");
        entity.Cargo = Clean(request.Cargo);
        entity.FechaIngreso = (request.FechaIngreso ?? DateTime.UtcNow).Date;
        entity.FechaRetiro = request.FechaRetiro?.Date;
        entity.TipoContrato = TextOrDefault(request.TipoContrato, "Termino indefinido");
        entity.TipoTrabajador = TextOrDefault(request.TipoTrabajador, "Dependiente");
        entity.SubtipoCotizante = TextOrDefault(request.SubtipoCotizante, "No aplica");
        entity.TipoSalario = TextOrDefault(request.TipoSalario, "Ordinario");
        entity.SalarioIntegral = request.SalarioIntegral;
        entity.SalarioBase = request.SalarioBase;
        entity.ValorDiaBase = request.ValorDiaBase;
        entity.PeriodicidadPago = TextOrDefault(request.PeriodicidadPago, "Mensual");
        entity.JornadaLaboral = TextOrDefault(request.JornadaLaboral, "Tiempo completo");
        entity.Eps = Clean(request.Eps);
        entity.FondoPension = Clean(request.FondoPension);
        entity.FondoCesantias = Clean(request.FondoCesantias);
        entity.Arl = Clean(request.Arl);
        entity.NivelRiesgoArl = TextOrDefault(request.NivelRiesgoArl, "I");
        entity.CajaCompensacion = Clean(request.CajaCompensacion);
        entity.Banco = Clean(request.Banco);
        entity.TipoCuenta = Clean(request.TipoCuenta);
        entity.NumeroCuenta = Clean(request.NumeroCuenta);
        entity.ContactoEmergenciaNombre = Clean(request.ContactoEmergenciaNombre);
        entity.ContactoEmergenciaTelefono = Clean(request.ContactoEmergenciaTelefono);
        entity.Observaciones = Clean(request.Observaciones);
        entity.Estado = request.Estado;
    }

    private static string? ValidateEmpleado(UpsertNominaEmpleadoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TipoDocumento) || string.IsNullOrWhiteSpace(request.NumeroDocumento))
        {
            return "El tipo y numero de documento son obligatorios.";
        }

        if (string.IsNullOrWhiteSpace(request.PrimerNombre) || string.IsNullOrWhiteSpace(request.PrimerApellido))
        {
            return "El primer nombre y primer apellido son obligatorios.";
        }

        if (request.FechaIngreso.HasValue && request.FechaRetiro.HasValue && request.FechaRetiro.Value.Date < request.FechaIngreso.Value.Date)
        {
            return "La fecha de retiro no puede ser menor que la fecha de ingreso.";
        }

        if (request.SalarioBase < 0 || request.ValorDiaBase < 0)
        {
            return "El salario base y el valor dia no pueden ser negativos.";
        }

        return null;
    }

    private static string? ValidateRegistro(NominaPeriodo periodo, UpsertNominaRegistroRequest request)
    {
        var fecha = request.Fecha.Date;
        if (fecha < periodo.FechaInicio.Date || fecha > periodo.FechaFin.Date)
        {
            return "La fecha del registro debe estar dentro del periodo.";
        }

        if (request.FechaFin.HasValue && request.FechaFin.Value.Date < fecha)
        {
            return "La fecha final no puede ser menor que la fecha inicial.";
        }

        if (!string.Equals(request.Concepto, "Dia", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.TipoNovedad) || string.IsNullOrWhiteSpace(request.CodigoNovedad))
            {
                return "La novedad debe tener tipo y concepto legal.";
            }

            if (request.Valor < 0)
            {
                return "El valor de la novedad no puede ser negativo. Usa el tipo Deduccion para descuentos.";
            }

            if (request.Horas.HasValue && request.Horas < 0)
            {
                return "Las horas no pueden ser negativas.";
            }

            if (request.Porcentaje.HasValue && request.Porcentaje < 0)
            {
                return "El porcentaje no puede ser negativo.";
            }

            if (request.BaseCalculo.HasValue && request.BaseCalculo < 0)
            {
                return "La base de calculo no puede ser negativa.";
            }
        }

        return null;
    }

    private static string BuildNombreCompleto(UpsertNominaEmpleadoRequest request) =>
        string.Join(" ", new[] { request.PrimerNombre, request.SegundoNombre, request.PrimerApellido, request.SegundoApellido }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

    private static string TextOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FirstToken(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    private static string LastToken(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;

    private static decimal SignedValor(NominaRegistroDto registro) =>
        string.Equals(registro.TipoNovedad, "Deduccion", StringComparison.OrdinalIgnoreCase) ? -registro.Valor : registro.Valor;
}
