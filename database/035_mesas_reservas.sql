USE MRSDrunkDb;
GO

SET NOCOUNT ON;

IF OBJECT_ID('dbo.Mesas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mesas (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        Nombre NVARCHAR(40) NOT NULL,
        Capacidad INT NOT NULL CONSTRAINT DF_Mesas_Capacidad DEFAULT (4),
        PosicionX INT NOT NULL CONSTRAINT DF_Mesas_PosicionX DEFAULT (0),
        PosicionY INT NOT NULL CONSTRAINT DF_Mesas_PosicionY DEFAULT (0),
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Mesas_Estado DEFAULT ('Libre'),
        Activa BIT NOT NULL CONSTRAINT DF_Mesas_Activa DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Mesas_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_Mesas_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id)
    );

    CREATE UNIQUE INDEX UX_Mesas_EmpresaNombre ON dbo.Mesas(EmpresaId, Nombre);
END;
GO

IF OBJECT_ID('dbo.Reservas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservas (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        MesaId INT NOT NULL,
        Cliente NVARCHAR(120) NOT NULL,
        Telefono NVARCHAR(30) NULL,
        NumeroPersonas INT NOT NULL CONSTRAINT DF_Reservas_NumeroPersonas DEFAULT (2),
        FechaHora DATETIME2 NOT NULL,
        DuracionMinutos INT NOT NULL CONSTRAINT DF_Reservas_DuracionMinutos DEFAULT (90),
        Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Reservas_Estado DEFAULT ('Pendiente'),
        Observacion NVARCHAR(250) NULL,
        UsuarioCreacionId INT NOT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Reservas_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_Reservas_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_Reservas_Mesas FOREIGN KEY (MesaId) REFERENCES dbo.Mesas(Id),
        CONSTRAINT FK_Reservas_Usuarios FOREIGN KEY (UsuarioCreacionId) REFERENCES dbo.Usuarios(Id)
    );

    CREATE INDEX IX_Reservas_Mesa_Fecha ON dbo.Reservas(MesaId, FechaHora);
    CREATE INDEX IX_Reservas_Empresa_Fecha ON dbo.Reservas(EmpresaId, FechaHora);
END;
GO

DECLARE @ReservasId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Reservas');
IF @ReservasId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Reservas', 'fa-calendar-days', 11, 1);
    SET @ReservasId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/reservas')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@ReservasId, 'Plano y reservas', '/reservas', 'fa-chair', 1, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/reservas', 'Reservas.Mesas.Ver', 'Ver plano de mesas', 'Consultar el plano visual y el estado de las mesas'),
('/reservas', 'Reservas.Mesas.Crear', 'Crear mesas', 'Agregar mesas al plano'),
('/reservas', 'Reservas.Mesas.Editar', 'Editar mesas', 'Editar mesas, su posicion y cambiar su estado manualmente'),
('/reservas', 'Reservas.Reservas.Ver', 'Ver reservas', 'Consultar la agenda de reservas'),
('/reservas', 'Reservas.Reservas.Crear', 'Crear reservas', 'Registrar nuevas reservas'),
('/reservas', 'Reservas.Reservas.Editar', 'Editar reservas', 'Editar reservas y cambiar su estado (confirmar, cancelar, completar)');

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
       CASE WHEN p.Codigo LIKE '%.Crear' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Ver' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Editar' THEN 1 ELSE 0 END,
       0
FROM @Permisos p
JOIN Permisos pe ON pe.Codigo = p.Codigo
JOIN Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = pe.Id AND rp.VentanaId = v.Id
  );
GO
