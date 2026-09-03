USE MRSDrunkDb;
GO

SET NOCOUNT ON;

DECLARE @PreparacionId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Preparacion');
IF @PreparacionId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Preparacion', 'fa-kitchen-set', 9, 1);
    SET @PreparacionId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/preparacion')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@PreparacionId, 'Barra y cocina', '/preparacion', 'fa-bell-concierge', 1, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/preparacion', 'Preparacion.Comandas.Ver', 'Ver tablero de preparacion', 'Consultar comandas pendientes, en preparacion y listas'),
('/preparacion', 'Preparacion.Comandas.Gestionar', 'Gestionar preparacion', 'Tomar, marcar listo y despachar productos de comandas');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId,
       pe.Id,
       v.Id,
       CASE WHEN p.Codigo LIKE '%.Ver' THEN 1 ELSE 0 END,
       0,
       CASE WHEN p.Codigo LIKE '%.Ver' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Gestionar' THEN 1 ELSE 0 END,
       0
FROM @Permisos p
JOIN Permisos pe ON pe.Codigo = p.Codigo
JOIN Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = pe.Id AND rp.VentanaId = v.Id
  );
GO
