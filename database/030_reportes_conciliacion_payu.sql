SET NOCOUNT ON;

DECLARE @OperacionId INT = (SELECT Id FROM dbo.Modulos WHERE Nombre = 'Operacion');

IF @OperacionId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ventanas WHERE Ruta = '/operacion/pagos-payu-reportes')
BEGIN
    INSERT INTO dbo.Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado)
    VALUES (@OperacionId, 'Reportes PayU', '/operacion/pagos-payu-reportes', 'fa-chart-line', 5, 1);
END;

UPDATE dbo.Ventanas
SET Nombre = 'Reportes PayU',
    Icono = 'fa-chart-line',
    Orden = 5
WHERE Ruta = '/operacion/pagos-payu-reportes';

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250), PuedeEditar BIT);
INSERT INTO @Permisos VALUES
('/operacion/pagos-payu', 'Operacion.PagosPayU.Conciliar', 'Conciliar pagos PayU', 'Ejecutar acciones de remediacion, reproceso y conciliacion historica de pagos PayU', 1),
('/operacion/pagos-payu-reportes', 'Operacion.PagosPayU.Reportes', 'Ver reportes PayU', 'Consultar reportes ejecutivos de pagos PayU por fecha, usuario y metodo', 0);

INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT p.Codigo, p.Nombre, p.Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO dbo.RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId,
       pe.Id,
       v.Id,
       1,
       0,
       1,
       p.PuedeEditar,
       0
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
