USE MRSDrunkDb;
GO

SET NOCOUNT ON;

IF OBJECT_ID('dbo.AreasPreparacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AreasPreparacion (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Orden INT NOT NULL CONSTRAINT DF_AreasPreparacion_Orden DEFAULT (0),
        Estado BIT NOT NULL CONSTRAINT DF_AreasPreparacion_Estado DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_AreasPreparacion_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_AreasPreparacion_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id)
    );

    CREATE UNIQUE INDEX UX_AreasPreparacion_EmpresaNombre ON dbo.AreasPreparacion(EmpresaId, Nombre);
END;
GO

IF COL_LENGTH('dbo.Productos', 'AreaPreparacionId') IS NULL
BEGIN
    ALTER TABLE dbo.Productos
        ADD AreaPreparacionId INT NULL
            CONSTRAINT FK_Productos_AreasPreparacion FOREIGN KEY REFERENCES dbo.AreasPreparacion(Id);
END;
GO

IF COL_LENGTH('dbo.Productos', 'RequierePreparacion') IS NULL
BEGIN
    ALTER TABLE dbo.Productos
        ADD RequierePreparacion BIT NOT NULL CONSTRAINT DF_Productos_RequierePreparacion DEFAULT (1);
END;
GO

IF OBJECT_ID('dbo.UsuarioAreaPreparacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioAreaPreparacion (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UsuarioId INT NOT NULL,
        AreaPreparacionId INT NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_UsuarioAreaPreparacion_Estado DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_UsuarioAreaPreparacion_FechaCreacion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_UsuarioAreaPreparacion_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT FK_UsuarioAreaPreparacion_Areas FOREIGN KEY (AreaPreparacionId) REFERENCES dbo.AreasPreparacion(Id)
    );

    CREATE UNIQUE INDEX UX_UsuarioAreaPreparacion_UsuarioArea ON dbo.UsuarioAreaPreparacion(UsuarioId, AreaPreparacionId);
END;
GO

IF OBJECT_ID('dbo.Comandas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comandas (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        CuentaId INT NOT NULL,
        Numero NVARCHAR(40) NOT NULL,
        MeseroId INT NOT NULL,
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_Comandas_Estado DEFAULT ('PENDIENTE'),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Comandas_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_Comandas_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_Comandas_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(Id),
        CONSTRAINT FK_Comandas_Cuentas FOREIGN KEY (CuentaId) REFERENCES dbo.Cuentas(Id),
        CONSTRAINT FK_Comandas_Meseros FOREIGN KEY (MeseroId) REFERENCES dbo.Usuarios(Id)
    );

    CREATE INDEX IX_Comandas_CuentaId ON dbo.Comandas(CuentaId);
    CREATE UNIQUE INDEX UX_Comandas_EmpresaNumero ON dbo.Comandas(EmpresaId, Numero);
END;
GO

IF OBJECT_ID('dbo.ComandaDetalles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComandaDetalles (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ComandaId INT NOT NULL,
        CuentaItemId INT NOT NULL,
        ProductoId INT NOT NULL,
        ProductoNombre NVARCHAR(160) NOT NULL,
        AreaPreparacionId INT NULL,
        Cantidad DECIMAL(18,3) NOT NULL,
        RequierePreparacion BIT NOT NULL CONSTRAINT DF_ComandaDetalles_RequierePreparacion DEFAULT (1),
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_ComandaDetalles_Estado DEFAULT ('PENDIENTE'),
        Observacion NVARCHAR(300) NULL,
        UsuarioTomaId INT NULL,
        FechaToma DATETIME2 NULL,
        UsuarioListoId INT NULL,
        FechaListo DATETIME2 NULL,
        UsuarioDespachoId INT NULL,
        FechaDespacho DATETIME2 NULL,
        UsuarioEntregaId INT NULL,
        FechaEntrega DATETIME2 NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_ComandaDetalles_FechaCreacion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ComandaDetalles_Comandas FOREIGN KEY (ComandaId) REFERENCES dbo.Comandas(Id),
        CONSTRAINT FK_ComandaDetalles_CuentaItems FOREIGN KEY (CuentaItemId) REFERENCES dbo.CuentaItems(Id),
        CONSTRAINT FK_ComandaDetalles_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id),
        CONSTRAINT FK_ComandaDetalles_Areas FOREIGN KEY (AreaPreparacionId) REFERENCES dbo.AreasPreparacion(Id),
        CONSTRAINT FK_ComandaDetalles_UsuarioToma FOREIGN KEY (UsuarioTomaId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT FK_ComandaDetalles_UsuarioListo FOREIGN KEY (UsuarioListoId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT FK_ComandaDetalles_UsuarioDespacho FOREIGN KEY (UsuarioDespachoId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT FK_ComandaDetalles_UsuarioEntrega FOREIGN KEY (UsuarioEntregaId) REFERENCES dbo.Usuarios(Id)
    );

    CREATE UNIQUE INDEX UX_ComandaDetalles_CuentaItemId ON dbo.ComandaDetalles(CuentaItemId);
    CREATE INDEX IX_ComandaDetalles_ComandaId ON dbo.ComandaDetalles(ComandaId);
    CREATE INDEX IX_ComandaDetalles_AreaEstado ON dbo.ComandaDetalles(AreaPreparacionId, Estado);
END;
GO

IF OBJECT_ID('dbo.ComandaDetalleEventos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComandaDetalleEventos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ComandaDetalleId INT NOT NULL,
        EstadoAnterior NVARCHAR(30) NULL,
        EstadoNuevo NVARCHAR(30) NOT NULL,
        UsuarioId INT NOT NULL,
        Fecha DATETIME2 NOT NULL CONSTRAINT DF_ComandaDetalleEventos_Fecha DEFAULT SYSUTCDATETIME(),
        Observacion NVARCHAR(300) NULL,
        CONSTRAINT FK_ComandaDetalleEventos_Detalles FOREIGN KEY (ComandaDetalleId) REFERENCES dbo.ComandaDetalles(Id),
        CONSTRAINT FK_ComandaDetalleEventos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
    );

    CREATE INDEX IX_ComandaDetalleEventos_Detalle ON dbo.ComandaDetalleEventos(ComandaDetalleId, Fecha);
END;
GO

IF COL_LENGTH('dbo.Cuentas', 'TokenPublico') IS NULL
BEGIN
    ALTER TABLE dbo.Cuentas
        ADD TokenPublico NVARCHAR(64) NULL;
END;
GO

IF COL_LENGTH('dbo.Cuentas', 'CodigoPublico') IS NULL
BEGIN
    ALTER TABLE dbo.Cuentas
        ADD CodigoPublico NVARCHAR(20) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Cuentas_TokenPublico')
    CREATE UNIQUE INDEX UX_Cuentas_TokenPublico ON dbo.Cuentas(TokenPublico) WHERE TokenPublico IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Cuentas_CodigoPublico')
    CREATE UNIQUE INDEX UX_Cuentas_CodigoPublico ON dbo.Cuentas(CodigoPublico) WHERE CodigoPublico IS NOT NULL;
GO

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM dbo.Empresas ORDER BY Id);
IF @EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AreasPreparacion WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.AreasPreparacion (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES
    (@EmpresaId, 'Barra', 'Cocteles, licores y bebidas', 1, 1),
    (@EmpresaId, 'Cocina', 'Platos y alimentos', 2, 1),
    (@EmpresaId, 'Postres', 'Postres y dulces', 3, 1);
END;
GO
