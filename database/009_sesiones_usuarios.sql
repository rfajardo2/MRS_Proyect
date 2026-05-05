/* Sesiones activas y permisos administrativos de cierre. */

IF OBJECT_ID('UsuarioSesiones', 'U') IS NULL
BEGIN
    CREATE TABLE UsuarioSesiones (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UsuarioId INT NOT NULL,
        EmpresaId INT NOT NULL,
        SessionId NVARCHAR(80) NOT NULL,
        IpAddress NVARCHAR(80) NULL,
        UserAgent NVARCHAR(500) NULL,
        FechaInicio DATETIME2 NOT NULL CONSTRAINT DF_UsuarioSesiones_FechaInicio DEFAULT SYSUTCDATETIME(),
        UltimaActividad DATETIME2 NOT NULL CONSTRAINT DF_UsuarioSesiones_UltimaActividad DEFAULT SYSUTCDATETIME(),
        FechaExpiracion DATETIME2 NOT NULL,
        FechaCierre DATETIME2 NULL,
        Estado BIT NOT NULL CONSTRAINT DF_UsuarioSesiones_Estado DEFAULT 1,
        CerradaPor NVARCHAR(80) NULL,
        CONSTRAINT FK_UsuarioSesiones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
        CONSTRAINT FK_UsuarioSesiones_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
    );

    CREATE UNIQUE INDEX UX_UsuarioSesiones_SessionId ON UsuarioSesiones(SessionId);
    CREATE INDEX IX_UsuarioSesiones_EmpresaEstado ON UsuarioSesiones(EmpresaId, Estado, FechaExpiracion);
END
GO

DECLARE @ModuloSeguridadId INT = (SELECT TOP 1 Id FROM Modulos WHERE Nombre = 'Seguridad');

IF @ModuloSeguridadId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/sesiones')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado)
    VALUES (@ModuloSeguridadId, 'Sesiones', '/sesiones', 'fa-user-clock', 6, 1);
GO

MERGE Permisos AS target
USING (VALUES
    ('Seguridad.Sesiones.Ver', 'Ver sesiones', 'Ver usuarios conectados y sesiones activas'),
    ('Seguridad.Sesiones.Cerrar', 'Cerrar sesiones', 'Cerrar sesiones de otros usuarios'),
    ('Seguridad.Sesiones.CerrarTodas', 'Cerrar todos los usuarios', 'Cerrar todas las sesiones activas excepto la propia')
) AS source (Codigo, Nombre, Descripcion)
ON target.Codigo = source.Codigo
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Descripcion, Estado)
    VALUES (source.Codigo, source.Nombre, source.Descripcion, 1);
GO

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1);
DECLARE @SesionVentanaId INT = (SELECT TOP 1 Id FROM Ventanas WHERE Ruta = '/sesiones');

IF @SuperRolId IS NOT NULL AND @SesionVentanaId IS NOT NULL
BEGIN
    INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
    SELECT @SuperRolId, p.Id, @SesionVentanaId, 1, 1, 1, 1, 1
    FROM Permisos p
    WHERE p.Codigo IN ('Seguridad.Sesiones.Ver', 'Seguridad.Sesiones.Cerrar', 'Seguridad.Sesiones.CerrarTodas')
      AND NOT EXISTS (
          SELECT 1 FROM RolPermisos rp
          WHERE rp.RolId = @SuperRolId AND rp.PermisoId = p.Id AND rp.VentanaId = @SesionVentanaId
      );
END
GO
