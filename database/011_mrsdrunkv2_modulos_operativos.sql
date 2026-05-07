USE MRSDrunkDb;
GO

IF OBJECT_ID('ConfiguracionesVenta', 'U') IS NULL
BEGIN
    CREATE TABLE ConfiguracionesVenta (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        RequiereAprobacionCierre BIT NOT NULL CONSTRAINT DF_ConfiguracionesVenta_Aprobacion DEFAULT 1,
        PermiteDividirCuenta BIT NOT NULL CONSTRAINT DF_ConfiguracionesVenta_Dividir DEFAULT 1,
        PermiteEliminarItems BIT NOT NULL CONSTRAINT DF_ConfiguracionesVenta_EliminarItems DEFAULT 1,
        RequiereMotivoEliminarItem BIT NOT NULL CONSTRAINT DF_ConfiguracionesVenta_MotivoItem DEFAULT 1,
        RequiereMotivoAnularCuenta BIT NOT NULL CONSTRAINT DF_ConfiguracionesVenta_MotivoAnular DEFAULT 1,
        PorcentajeRepartoBase DECIMAL(18,4) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_Reparto DEFAULT 45,
        TarifaCuatroPorMil DECIMAL(18,4) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_4x1000 DEFAULT 0.4,
        TarifaRetefuente DECIMAL(18,4) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_Retefuente DEFAULT 1.5,
        TarifaComisionDatafono DECIMAL(18,4) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_ComisionDatafono DEFAULT 3.29,
        TarifaRetIca DECIMAL(18,4) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_RetIca DEFAULT 0.42,
        ComisionFijaDatafono DECIMAL(18,2) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_ComisionFija DEFAULT 300,
        HoraInicioDiaOperativo TIME NOT NULL CONSTRAINT DF_ConfiguracionesVenta_HoraInicio DEFAULT '06:00',
        HoraCierreDiaOperativo TIME NOT NULL CONSTRAINT DF_ConfiguracionesVenta_HoraCierre DEFAULT '05:59',
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_ConfiguracionesVenta_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_ConfiguracionesVenta_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
    );

    CREATE UNIQUE INDEX UX_ConfiguracionesVenta_Empresa ON ConfiguracionesVenta(EmpresaId);
END
GO

IF OBJECT_ID('ProductoCategorias', 'U') IS NULL
BEGIN
    CREATE TABLE ProductoCategorias (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Orden INT NOT NULL CONSTRAINT DF_ProductoCategorias_Orden DEFAULT 0,
        Estado BIT NOT NULL CONSTRAINT DF_ProductoCategorias_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_ProductoCategorias_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_ProductoCategorias_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
    );

    CREATE UNIQUE INDEX UX_ProductoCategorias_EmpresaNombre ON ProductoCategorias(EmpresaId, Nombre);
END
GO

IF OBJECT_ID('Productos', 'U') IS NULL
BEGIN
    CREATE TABLE Productos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        CategoriaId INT NOT NULL,
        Nombre NVARCHAR(160) NOT NULL,
        Descripcion NVARCHAR(300) NULL,
        PrecioVenta DECIMAL(18,2) NOT NULL,
        CostoEstimado DECIMAL(18,2) NULL,
        ControlaInventario BIT NOT NULL CONSTRAINT DF_Productos_ControlaInventario DEFAULT 0,
        Estado BIT NOT NULL CONSTRAINT DF_Productos_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Productos_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_Productos_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_Productos_Categorias FOREIGN KEY (CategoriaId) REFERENCES ProductoCategorias(Id)
    );

    CREATE UNIQUE INDEX UX_Productos_EmpresaNombre ON Productos(EmpresaId, Nombre);
END
GO

IF OBJECT_ID('DiasOperativos', 'U') IS NULL
BEGIN
    CREATE TABLE DiasOperativos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        Fecha DATE NOT NULL,
        FechaApertura DATETIME2 NOT NULL CONSTRAINT DF_DiasOperativos_FechaApertura DEFAULT SYSUTCDATETIME(),
        FechaCierre DATETIME2 NULL,
        Cerrado BIT NOT NULL CONSTRAINT DF_DiasOperativos_Cerrado DEFAULT 0,
        UsuarioAperturaId INT NOT NULL,
        UsuarioCierreId INT NULL,
        CONSTRAINT FK_DiasOperativos_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_DiasOperativos_Sucursales FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
        CONSTRAINT FK_DiasOperativos_UsuarioApertura FOREIGN KEY (UsuarioAperturaId) REFERENCES Usuarios(Id),
        CONSTRAINT FK_DiasOperativos_UsuarioCierre FOREIGN KEY (UsuarioCierreId) REFERENCES Usuarios(Id)
    );

    CREATE UNIQUE INDEX UX_DiasOperativos_EmpresaSucursalFecha ON DiasOperativos(EmpresaId, SucursalId, Fecha);
END
GO

IF OBJECT_ID('Cuentas', 'U') IS NULL
BEGIN
    CREATE TABLE Cuentas (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        MeseroId INT NOT NULL,
        DiaOperativoId INT NULL,
        Numero NVARCHAR(40) NOT NULL,
        Mesa NVARCHAR(80) NULL,
        Cliente NVARCHAR(160) NULL,
        Estado NVARCHAR(40) NOT NULL CONSTRAINT DF_Cuentas_Estado DEFAULT 'Abierta',
        Dividida BIT NOT NULL CONSTRAINT DF_Cuentas_Dividida DEFAULT 0,
        Observacion NVARCHAR(500) NULL,
        FechaApertura DATETIME2 NOT NULL CONSTRAINT DF_Cuentas_FechaApertura DEFAULT SYSUTCDATETIME(),
        FechaSolicitudCierre DATETIME2 NULL,
        FechaCierre DATETIME2 NULL,
        AdministradorCierreId INT NULL,
        MotivoRechazo NVARCHAR(500) NULL,
        MotivoAnulacion NVARCHAR(500) NULL,
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cuentas_Subtotal DEFAULT 0,
        Descuento DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cuentas_Descuento DEFAULT 0,
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cuentas_Total DEFAULT 0,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Cuentas_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_Cuentas_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_Cuentas_Sucursales FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
        CONSTRAINT FK_Cuentas_Meseros FOREIGN KEY (MeseroId) REFERENCES Usuarios(Id),
        CONSTRAINT FK_Cuentas_DiasOperativos FOREIGN KEY (DiaOperativoId) REFERENCES DiasOperativos(Id),
        CONSTRAINT FK_Cuentas_AdminCierre FOREIGN KEY (AdministradorCierreId) REFERENCES Usuarios(Id)
    );

    CREATE UNIQUE INDEX UX_Cuentas_EmpresaNumero ON Cuentas(EmpresaId, Numero);
END
GO

IF OBJECT_ID('CuentaItems', 'U') IS NULL
BEGIN
    CREATE TABLE CuentaItems (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CuentaId INT NOT NULL,
        ProductoId INT NOT NULL,
        ProductoNombre NVARCHAR(160) NOT NULL,
        Cantidad DECIMAL(18,3) NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Descuento DECIMAL(18,2) NOT NULL CONSTRAINT DF_CuentaItems_Descuento DEFAULT 0,
        Total DECIMAL(18,2) NOT NULL,
        Eliminado BIT NOT NULL CONSTRAINT DF_CuentaItems_Eliminado DEFAULT 0,
        MotivoEliminacion NVARCHAR(500) NULL,
        UsuarioCreacionId INT NOT NULL,
        UsuarioEliminacionId INT NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CuentaItems_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaEliminacion DATETIME2 NULL,
        CONSTRAINT FK_CuentaItems_Cuentas FOREIGN KEY (CuentaId) REFERENCES Cuentas(Id),
        CONSTRAINT FK_CuentaItems_Productos FOREIGN KEY (ProductoId) REFERENCES Productos(Id),
        CONSTRAINT FK_CuentaItems_UsuarioCreacion FOREIGN KEY (UsuarioCreacionId) REFERENCES Usuarios(Id),
        CONSTRAINT FK_CuentaItems_UsuarioEliminacion FOREIGN KEY (UsuarioEliminacionId) REFERENCES Usuarios(Id)
    );
END
GO

IF OBJECT_ID('CuentaPagos', 'U') IS NULL
BEGIN
    CREATE TABLE CuentaPagos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CuentaId INT NOT NULL,
        MetodoPago NVARCHAR(40) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        Referencia NVARCHAR(160) NULL,
        FechaPago DATETIME2 NOT NULL CONSTRAINT DF_CuentaPagos_FechaPago DEFAULT SYSUTCDATETIME(),
        UsuarioRegistroId INT NOT NULL,
        CONSTRAINT FK_CuentaPagos_Cuentas FOREIGN KEY (CuentaId) REFERENCES Cuentas(Id),
        CONSTRAINT FK_CuentaPagos_UsuarioRegistro FOREIGN KEY (UsuarioRegistroId) REFERENCES Usuarios(Id)
    );
END
GO

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM Empresas ORDER BY Id);
IF @EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ConfiguracionesVenta WHERE EmpresaId = @EmpresaId)
    INSERT INTO ConfiguracionesVenta (EmpresaId) VALUES (@EmpresaId);

IF @EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductoCategorias WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO ProductoCategorias (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES
    (@EmpresaId, 'Cervezas', 'Productos embotellados y latas', 1, 1),
    (@EmpresaId, 'Cocteles', 'Preparaciones de bar', 2, 1),
    (@EmpresaId, 'Servicios', 'Servicios adicionales y cobros eventuales', 3, 1);
END
GO

DECLARE @ConfigId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Configuracion');
IF @ConfigId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Configuracion', 'fa-gears', 5, 1);
    SET @ConfigId = SCOPE_IDENTITY();
END

DECLARE @ProductosId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Productos');
IF @ProductosId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Productos', 'fa-martini-glass-citrus', 6, 1);
    SET @ProductosId = SCOPE_IDENTITY();
END

DECLARE @OperacionId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Operacion');
IF @OperacionId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Operacion', 'fa-cash-register', 7, 1);
    SET @OperacionId = SCOPE_IDENTITY();
END

DECLARE @AdminCuentasId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Administracion cuentas');
IF @AdminCuentasId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Administracion cuentas', 'fa-clipboard-list', 8, 1);
    SET @AdminCuentasId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/configuracion/ventas')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@ConfigId, 'Configuracion de ventas', '/configuracion/ventas', 'fa-sliders', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/productos/categorias')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@ProductosId, 'Categorias', '/productos/categorias', 'fa-layer-group', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/productos')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@ProductosId, 'Productos', '/productos', 'fa-wine-bottle', 2, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/operacion/cuentas')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@OperacionId, 'Mis cuentas', '/operacion/cuentas', 'fa-receipt', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/operacion/balance')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@OperacionId, 'Balance del dia', '/operacion/balance', 'fa-chart-line', 2, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/admin-cuentas')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@AdminCuentasId, 'Aprobacion de cuentas', '/admin-cuentas', 'fa-users-viewfinder', 1, 1);
ELSE
    UPDATE Ventanas SET Nombre = 'Aprobacion de cuentas' WHERE Ruta = '/admin-cuentas';
IF EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/admin-cuentas/usuarios')
    UPDATE Ventanas SET Ruta = '/cuentas-por-usuario' WHERE Ruta = '/admin-cuentas/usuarios';
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/cuentas-por-usuario')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@AdminCuentasId, 'Cuentas por usuario', '/cuentas-por-usuario', 'fa-table-list', 2, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/admin-cuentas/balance')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@AdminCuentasId, 'Balance general del dia', '/admin-cuentas/balance', 'fa-scale-balanced', 3, 1);
ELSE
    UPDATE Ventanas SET Nombre = 'Balance general del dia' WHERE Ruta = '/admin-cuentas/balance';
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/configuracion/ventas', 'Configuracion.Ventas.Ver', 'Ver configuracion de ventas', 'Consultar reglas globales de ventas'),
('/configuracion/ventas', 'Configuracion.Ventas.Editar', 'Editar configuracion de ventas', 'Modificar reglas globales de ventas'),
('/productos/categorias', 'Productos.Categorias.Ver', 'Ver categorias', 'Consultar categorias de productos'),
('/productos/categorias', 'Productos.Categorias.Crear', 'Crear categorias', 'Crear categorias de productos'),
('/productos/categorias', 'Productos.Categorias.Editar', 'Editar categorias', 'Editar categorias de productos'),
('/productos', 'Productos.Productos.Ver', 'Ver productos', 'Consultar productos'),
('/productos', 'Productos.Productos.Crear', 'Crear productos', 'Crear productos'),
('/productos', 'Productos.Productos.Editar', 'Editar productos', 'Editar productos'),
('/operacion/cuentas', 'Operacion.Cuentas.Ver', 'Ver cuentas propias', 'Consultar cuentas propias'),
('/operacion/cuentas', 'Operacion.Cuentas.Crear', 'Crear cuentas', 'Crear cuentas de mesero'),
('/operacion/cuentas', 'Operacion.Cuentas.Editar', 'Gestionar cuentas', 'Agregar productos, pagos, division y cierre'),
('/operacion/cuentas', 'Operacion.Cuentas.Eliminar', 'Eliminar items', 'Eliminar items de cuentas propias'),
('/operacion/balance', 'Operacion.Balance.Ver', 'Ver balance propio', 'Consultar balance del dia propio'),
('/admin-cuentas', 'AdministracionCuentas.Cuentas.Ver', 'Ver cuentas de meseros', 'Consultar cuentas de todos los meseros'),
('/admin-cuentas', 'AdministracionCuentas.Cuentas.Editar', 'Aprobar cuentas', 'Aprobar o rechazar cierres'),
('/admin-cuentas', 'AdministracionCuentas.Cuentas.Eliminar', 'Anular cuentas', 'Anular cuentas'),
('/cuentas-por-usuario', 'AdministracionCuentas.Usuarios.Ver', 'Ver cuentas por usuario', 'Consultar mesas y cuentas creadas por usuarios'),
('/cuentas-por-usuario', 'AdministracionCuentas.Usuarios.Editar', 'Gestionar cuentas por usuario', 'Agregar productos, pagos y dividir cuentas de usuarios'),
('/cuentas-por-usuario', 'AdministracionCuentas.Usuarios.Eliminar', 'Eliminar items cuentas por usuario', 'Eliminar items de cuentas de usuarios'),
('/admin-cuentas/balance', 'AdministracionCuentas.Balance.Ver', 'Ver balance general del dia', 'Consultar balance general del turno por usuario');

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
       CASE WHEN p.Codigo LIKE '%.Eliminar' THEN 1 ELSE 0 END
FROM @Permisos p
JOIN Permisos pe ON pe.Codigo = p.Codigo
JOIN Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = pe.Id AND rp.VentanaId = v.Id
  );
GO
