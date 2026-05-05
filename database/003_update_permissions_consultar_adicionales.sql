USE MRSDrunkDb;
GO

IF COL_LENGTH('RolPermisos', 'PuedeConsultar') IS NULL
BEGIN
    ALTER TABLE RolPermisos ADD PuedeConsultar BIT NOT NULL CONSTRAINT DF_RolPermisos_Consultar DEFAULT 0;
END
GO

DECLARE @Permisos TABLE (Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('Seguridad.Usuarios.Consultar', 'Consultar usuario', 'Ver detalle de usuarios'),
('Seguridad.Usuarios.CambiarPassword', 'Cambiar contraseña', 'Cambiar la contraseña de otros usuarios'),
('Seguridad.Roles.Consultar', 'Consultar rol', 'Ver detalle de roles'),
('Seguridad.Permisos.Consultar', 'Consultar permisos', 'Ver detalle de permisos'),
('Seguridad.Permisos.Editar', 'Editar permisos', 'Asignar permisos por rol'),
('Configuracion.Empresas.Consultar', 'Consultar empresa', 'Ver detalle de empresas');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId, p.Id, v.Id,
       CASE WHEN p.Codigo LIKE '%.Ver' OR p.Codigo LIKE '%.CambiarPassword' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Crear' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Consultar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Editar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Eliminar' THEN 1 ELSE 0 END
FROM Permisos p
JOIN Ventanas v ON p.Codigo LIKE REPLACE((SELECT Nombre FROM Modulos WHERE Id = v.ModuloId), 'ó', 'o') + '.' + v.Nombre + '.%'
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = p.Id AND rp.VentanaId = v.Id
  );
GO
