/* Permiso especial para exportar Control diario de nomina. */

IF NOT EXISTS (SELECT 1 FROM Permisos WHERE Codigo = 'Nomina.Control.Exportar')
    INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
    VALUES ('Nomina.Control.Exportar', 'Exportar control diario', 'Exportar a Excel el control diario de nomina', 1);
GO

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1);
DECLARE @VentanaControlId INT = (SELECT TOP 1 Id FROM Ventanas WHERE Ruta = '/nomina/control-diario');
DECLARE @PermisoExportarId INT = (SELECT TOP 1 Id FROM Permisos WHERE Codigo = 'Nomina.Control.Exportar');

IF @SuperRolId IS NOT NULL AND @VentanaControlId IS NOT NULL AND @PermisoExportarId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM RolPermisos
        WHERE RolId = @SuperRolId AND VentanaId = @VentanaControlId AND PermisoId = @PermisoExportarId
    )
        INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar, PuedeExportar, PuedeConfigurar)
        VALUES (@SuperRolId, @PermisoExportarId, @VentanaControlId, 1, 0, 1, 0, 0, 1, 0);
END
GO
