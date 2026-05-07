SET NOCOUNT ON;

DECLARE @AdminCuentasId INT = (SELECT Id FROM dbo.Modulos WHERE Nombre = 'Administracion cuentas');

IF @AdminCuentasId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ventanas WHERE Ruta = '/admin-cuentas/usuarios')
BEGIN
    INSERT INTO dbo.Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado)
    VALUES (@AdminCuentasId, 'Cuentas por usuario', '/admin-cuentas/usuarios', 'fa-table-list', 2, 1);
END;

UPDATE dbo.Ventanas
SET Orden = 3
WHERE Ruta = '/admin-cuentas/balance'
  AND Orden < 3;

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/admin-cuentas/usuarios', 'AdministracionCuentas.Usuarios.Ver', 'Ver cuentas por usuario', 'Consultar mesas y cuentas creadas por usuarios'),
('/admin-cuentas/usuarios', 'AdministracionCuentas.Usuarios.Editar', 'Gestionar cuentas por usuario', 'Agregar productos, pagos y dividir cuentas de usuarios'),
('/admin-cuentas/usuarios', 'AdministracionCuentas.Usuarios.Eliminar', 'Eliminar items cuentas por usuario', 'Eliminar items de cuentas de usuarios');

INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT p.Codigo, p.Nombre, p.Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO dbo.RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId,
       pe.Id,
       v.Id,
       CASE WHEN p.Codigo LIKE '%.Ver' THEN 1 ELSE 0 END,
       0,
       CASE WHEN p.Codigo LIKE '%.Ver' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Editar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Eliminar' THEN 1 ELSE 0 END
FROM @Permisos p
JOIN dbo.Permisos pe ON pe.Codigo = p.Codigo
JOIN dbo.Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.RolPermisos rp
      WHERE rp.RolId = @SuperRolId
        AND rp.PermisoId = pe.Id
        AND rp.VentanaId = v.Id
  );
