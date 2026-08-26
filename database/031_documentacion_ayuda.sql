USE MRSDrunkDb;
GO

SET NOCOUNT ON;

DECLARE @AyudaId INT = (SELECT Id FROM dbo.Modulos WHERE Nombre = 'Ayuda');
IF @AyudaId IS NULL
BEGIN
    INSERT INTO dbo.Modulos (Nombre, Icono, Orden, Estado) VALUES ('Ayuda', 'fa-circle-question', 9, 1);
    SET @AyudaId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM dbo.Ventanas WHERE Ruta = '/ayuda')
    INSERT INTO dbo.Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@AyudaId, 'Ayuda', '/ayuda', 'fa-circle-question', 1, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/ayuda', 'Ayuda.General.Ver', 'Ver ayuda', 'Consultar la guia de uso de la plataforma');

INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT p.Codigo, p.Nombre, p.Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Codigo = p.Codigo);

-- La ayuda debe verse para todos los roles de todas las empresas, no solo SuperUsuario.
INSERT INTO dbo.RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT r.Id,
       pe.Id,
       v.Id,
       1,
       0,
       1,
       0,
       0
FROM dbo.Roles r
CROSS JOIN @Permisos p
JOIN dbo.Permisos pe ON pe.Codigo = p.Codigo
JOIN dbo.Ventanas v ON v.Ruta = p.Ruta
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.RolPermisos rp
    WHERE rp.RolId = r.Id
      AND rp.PermisoId = pe.Id
      AND rp.VentanaId = v.Id
);
GO
