SET NOCOUNT ON;

DECLARE @RolUsuarioId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE Nombre = 'Usuario');

IF @RolUsuarioId IS NOT NULL
BEGIN
    UPDATE rp
    SET PuedeEditar = 1
    FROM dbo.RolPermisos rp
    JOIN dbo.Permisos p ON p.Id = rp.PermisoId
    WHERE rp.RolId = @RolUsuarioId
      AND p.Codigo = 'Operacion.Cuentas.Editar';

    UPDATE rp
    SET PuedeEliminar = 1
    FROM dbo.RolPermisos rp
    JOIN dbo.Permisos p ON p.Id = rp.PermisoId
    WHERE rp.RolId = @RolUsuarioId
      AND p.Codigo = 'Operacion.Cuentas.Eliminar';
END;
