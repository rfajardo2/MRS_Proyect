USE MRSDrunkDb;
GO

SET NOCOUNT ON;

IF OBJECT_ID('dbo.RegistrosAuditoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RegistrosAuditoria (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        UsuarioId INT NOT NULL,
        Entidad NVARCHAR(80) NOT NULL,
        EntidadId NVARCHAR(40) NOT NULL,
        Accion NVARCHAR(20) NOT NULL,
        Detalle NVARCHAR(1000) NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_RegistrosAuditoria_FechaCreacion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_RegistrosAuditoria_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_RegistrosAuditoria_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
    );

    CREATE INDEX IX_RegistrosAuditoria_Empresa_Entidad ON dbo.RegistrosAuditoria(EmpresaId, Entidad, EntidadId);
    CREATE INDEX IX_RegistrosAuditoria_FechaCreacion ON dbo.RegistrosAuditoria(FechaCreacion DESC);
END;
GO

DECLARE @AuditoriaId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Auditoria');
IF @AuditoriaId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Auditoria', 'fa-clock-rotate-left', 10, 1);
    SET @AuditoriaId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/auditoria')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@AuditoriaId, 'Registro de cambios', '/auditoria', 'fa-list-check', 1, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/auditoria', 'Auditoria.Registros.Ver', 'Ver registro de auditoria', 'Consultar quien creo, edito o elimino registros del sistema');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId,
       pe.Id,
       v.Id,
       1,
       0,
       1,
       0,
       0
FROM @Permisos p
JOIN Permisos pe ON pe.Codigo = p.Codigo
JOIN Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = pe.Id AND rp.VentanaId = v.Id
  );
GO
