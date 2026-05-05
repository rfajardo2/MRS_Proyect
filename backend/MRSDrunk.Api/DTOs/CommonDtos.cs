namespace MRSDrunk.Api.DTOs;

public sealed record EmpresaDto(
    int Id,
    string Nombre,
    string Nit,
    string? DigitoVerificacion,
    string RazonSocial,
    string? NombreComercial,
    string TipoDocumento,
    string RegimenTributario,
    string ResponsabilidadFiscal,
    string? MatriculaMercantil,
    string? ActividadEconomicaCiiu,
    string? DireccionFiscal,
    string? Departamento,
    string? Municipio,
    string Pais,
    string? Telefono,
    string? CorreoFacturacion,
    string? RepresentanteLegal,
    string? DocumentoRepresentante,
    bool EsPrincipal,
    string? LogoUrl,
    bool Estado);

public sealed record UpsertEmpresaRequest(
    string Nombre,
    string Nit,
    string? DigitoVerificacion,
    string? RazonSocial,
    string? NombreComercial,
    string? TipoDocumento,
    string? RegimenTributario,
    string? ResponsabilidadFiscal,
    string? MatriculaMercantil,
    string? ActividadEconomicaCiiu,
    string? DireccionFiscal,
    string? Departamento,
    string? Municipio,
    string? Pais,
    string? Telefono,
    string? CorreoFacturacion,
    string? RepresentanteLegal,
    string? DocumentoRepresentante,
    bool EsPrincipal,
    string? LogoUrl,
    bool Estado);

public sealed record SucursalDto(
    int Id,
    int EmpresaId,
    string Empresa,
    string Nombre,
    string? Codigo,
    string? Direccion,
    string? Departamento,
    string? Municipio,
    string Pais,
    string? Telefono,
    string? Correo,
    bool EsPrincipal,
    bool Estado);

public sealed record UpsertSucursalRequest(
    int EmpresaId,
    string Nombre,
    string? Codigo,
    string? Direccion,
    string? Departamento,
    string? Municipio,
    string? Pais,
    string? Telefono,
    string? Correo,
    bool EsPrincipal,
    bool Estado);

public sealed record RolDto(int Id, int? EmpresaId, string Nombre, string? Descripcion, bool EsSuperUsuario, bool Estado);
public sealed record UpsertRolRequest(int? EmpresaId, string Nombre, string? Descripcion, bool Estado);

public sealed record UsuarioDto(
    int Id,
    int EmpresaId,
    int? SucursalId,
    string NombreCompleto,
    string Usuario,
    string Correo,
    int RolId,
    string Rol,
    string Empresa,
    bool Estado,
    DateTime FechaCreacion);

public sealed record UpsertUsuarioRequest(
    int EmpresaId,
    int? SucursalId,
    string NombreCompleto,
    string Usuario,
    string Correo,
    string? Password,
    int RolId,
    bool Estado);

public sealed record PermisoDto(int Id, string Codigo, string Nombre, string? Descripcion, bool Estado);

public sealed record RolPermisoDto(
    int Id,
    int RolId,
    int PermisoId,
    int VentanaId,
    string Modulo,
    string Ventana,
    string Permiso,
    bool PuedeVer,
    bool PuedeCrear,
    bool PuedeConsultar,
    bool PuedeEditar,
    bool PuedeEliminar);

public sealed record AsignarPermisoRequest(
    int PermisoId,
    int VentanaId,
    bool PuedeVer,
    bool PuedeCrear,
    bool PuedeConsultar,
    bool PuedeEditar,
    bool PuedeEliminar);

public sealed record GuardarPermisosRolRequest(IReadOnlyCollection<AsignarPermisoRequest> Permisos);

public sealed record MenuModuloDto(int Id, string Nombre, string? Icono, int Orden, IReadOnlyCollection<MenuVentanaDto> Ventanas);
public sealed record MenuVentanaDto(int Id, string Nombre, string Ruta, string? Icono, int Orden);
