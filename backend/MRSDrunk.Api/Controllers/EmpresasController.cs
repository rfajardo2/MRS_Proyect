using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRSDrunk.Api.Data;
using MRSDrunk.Api.DTOs;
using MRSDrunk.Api.Middleware;
using MRSDrunk.Api.Models;

namespace MRSDrunk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmpresasController(MrsDrunkDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Configuracion.Empresas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<EmpresaDto>>> Get(CancellationToken cancellationToken)
    {
        var data = await db.Empresas.AsNoTracking()
            .OrderByDescending(x => x.EsPrincipal)
            .ThenBy(x => x.Nombre)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("sucursales")]
    [RequirePermission("Configuracion.Empresas.Ver")]
    public async Task<ActionResult<IReadOnlyCollection<SucursalDto>>> GetSucursales(CancellationToken cancellationToken)
    {
        var data = await db.Sucursales.AsNoTracking()
            .Include(x => x.Empresa)
            .OrderBy(x => x.Empresa!.Nombre)
            .ThenByDescending(x => x.EsPrincipal)
            .ThenBy(x => x.Nombre)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpPost]
    [RequirePermission("Configuracion.Empresas.Crear")]
    public async Task<ActionResult<EmpresaDto>> Post(UpsertEmpresaRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateEmpresa(request);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        if (request.EsPrincipal)
        {
            await ClearEmpresaPrincipal(cancellationToken);
        }

        var entity = new Empresa();
        MapEmpresa(entity, request);
        db.Empresas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Configuracion.Empresas.Editar")]
    public async Task<IActionResult> Put(int id, UpsertEmpresaRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Empresas.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var validation = ValidateEmpresa(request);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        if (request.EsPrincipal)
        {
            await ClearEmpresaPrincipal(cancellationToken, id);
        }

        MapEmpresa(entity, request);
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("sucursales")]
    [RequirePermission("Configuracion.Empresas.Crear")]
    public async Task<ActionResult<SucursalDto>> CrearSucursal(UpsertSucursalRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateSucursal(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        if (request.EsPrincipal)
        {
            await ClearSucursalPrincipal(request.EmpresaId, cancellationToken);
        }

        var entity = new Sucursal();
        MapSucursal(entity, request);
        db.Sucursales.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(entity).Reference(x => x.Empresa).LoadAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPut("sucursales/{id:int}")]
    [RequirePermission("Configuracion.Empresas.Editar")]
    public async Task<IActionResult> EditarSucursal(int id, UpsertSucursalRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Sucursales.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var validation = await ValidateSucursal(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        if (request.EsPrincipal)
        {
            await ClearSucursalPrincipal(request.EmpresaId, cancellationToken, id);
        }

        MapSucursal(entity, request);
        entity.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static EmpresaDto ToDto(Empresa x) => new(
        x.Id,
        x.Nombre,
        x.Nit,
        x.DigitoVerificacion,
        x.RazonSocial,
        x.NombreComercial,
        x.TipoDocumento,
        x.RegimenTributario,
        x.ResponsabilidadFiscal,
        x.MatriculaMercantil,
        x.ActividadEconomicaCiiu,
        x.DireccionFiscal,
        x.Departamento,
        x.Municipio,
        x.Pais,
        x.Telefono,
        x.CorreoFacturacion,
        x.RepresentanteLegal,
        x.DocumentoRepresentante,
        x.EsPrincipal,
        x.LogoUrl,
        x.Estado);

    private static SucursalDto ToDto(Sucursal x) => new(
        x.Id,
        x.EmpresaId,
        x.Empresa?.Nombre ?? "Empresa",
        x.Nombre,
        x.Codigo,
        x.Direccion,
        x.Departamento,
        x.Municipio,
        x.Pais,
        x.Telefono,
        x.Correo,
        x.EsPrincipal,
        x.Estado);

    private static void MapEmpresa(Empresa entity, UpsertEmpresaRequest request)
    {
        entity.Nombre = TextOrDefault(request.Nombre, request.RazonSocial ?? string.Empty);
        entity.Nit = request.Nit.Trim();
        entity.DigitoVerificacion = Clean(request.DigitoVerificacion);
        entity.RazonSocial = TextOrDefault(request.RazonSocial, entity.Nombre);
        entity.NombreComercial = Clean(request.NombreComercial);
        entity.TipoDocumento = TextOrDefault(request.TipoDocumento, "NIT");
        entity.RegimenTributario = TextOrDefault(request.RegimenTributario, "Responsable de IVA");
        entity.ResponsabilidadFiscal = TextOrDefault(request.ResponsabilidadFiscal, "R-99-PN");
        entity.MatriculaMercantil = Clean(request.MatriculaMercantil);
        entity.ActividadEconomicaCiiu = Clean(request.ActividadEconomicaCiiu);
        entity.DireccionFiscal = Clean(request.DireccionFiscal);
        entity.Departamento = Clean(request.Departamento);
        entity.Municipio = Clean(request.Municipio);
        entity.Pais = TextOrDefault(request.Pais, "CO");
        entity.Telefono = Clean(request.Telefono);
        entity.CorreoFacturacion = Clean(request.CorreoFacturacion);
        entity.RepresentanteLegal = Clean(request.RepresentanteLegal);
        entity.DocumentoRepresentante = Clean(request.DocumentoRepresentante);
        entity.EsPrincipal = request.EsPrincipal;
        entity.LogoUrl = Clean(request.LogoUrl);
        entity.Estado = request.Estado;
    }

    private static void MapSucursal(Sucursal entity, UpsertSucursalRequest request)
    {
        entity.EmpresaId = request.EmpresaId;
        entity.Nombre = request.Nombre.Trim();
        entity.Codigo = Clean(request.Codigo);
        entity.Direccion = Clean(request.Direccion);
        entity.Departamento = Clean(request.Departamento);
        entity.Municipio = Clean(request.Municipio);
        entity.Pais = TextOrDefault(request.Pais, "CO");
        entity.Telefono = Clean(request.Telefono);
        entity.Correo = Clean(request.Correo);
        entity.EsPrincipal = request.EsPrincipal;
        entity.Estado = request.Estado;
    }

    private static string? ValidateEmpresa(UpsertEmpresaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Nit))
        {
            return "El nombre y NIT son obligatorios.";
        }

        return null;
    }

    private async Task<string?> ValidateSucursal(UpsertSucursalRequest request, CancellationToken cancellationToken)
    {
        if (request.EmpresaId <= 0 || string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "La empresa y el nombre de la sede son obligatorios.";
        }

        var exists = await db.Empresas.AnyAsync(x => x.Id == request.EmpresaId, cancellationToken);
        return exists ? null : "La empresa no existe.";
    }

    private async Task ClearEmpresaPrincipal(CancellationToken cancellationToken, int? exceptId = null)
    {
        var principales = await db.Empresas.Where(x => x.EsPrincipal && (!exceptId.HasValue || x.Id != exceptId)).ToListAsync(cancellationToken);
        foreach (var empresa in principales)
        {
            empresa.EsPrincipal = false;
        }
    }

    private async Task ClearSucursalPrincipal(int empresaId, CancellationToken cancellationToken, int? exceptId = null)
    {
        var principales = await db.Sucursales.Where(x => x.EmpresaId == empresaId && x.EsPrincipal && (!exceptId.HasValue || x.Id != exceptId)).ToListAsync(cancellationToken);
        foreach (var sucursal in principales)
        {
            sucursal.EsPrincipal = false;
        }
    }

    private static string TextOrDefault(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
