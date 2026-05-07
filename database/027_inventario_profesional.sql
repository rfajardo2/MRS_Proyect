SET NOCOUNT ON;

IF OBJECT_ID('dbo.UnidadesMedida', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UnidadesMedida (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        Codigo NVARCHAR(20) NOT NULL,
        Nombre NVARCHAR(80) NOT NULL,
        Decimales INT NOT NULL CONSTRAINT DF_UnidadesMedida_Decimales DEFAULT 3,
        Estado BIT NOT NULL CONSTRAINT DF_UnidadesMedida_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_UnidadesMedida_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_UnidadesMedida_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id)
    );
    CREATE UNIQUE INDEX UX_UnidadesMedida_EmpresaCodigo ON dbo.UnidadesMedida(EmpresaId, Codigo);
END;

IF OBJECT_ID('dbo.Proveedores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedores (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        Nombre NVARCHAR(160) NOT NULL,
        Nit NVARCHAR(40) NULL,
        Telefono NVARCHAR(40) NULL,
        Correo NVARCHAR(160) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_Proveedores_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Proveedores_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_Proveedores_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id)
    );
    CREATE UNIQUE INDEX UX_Proveedores_EmpresaNombre ON dbo.Proveedores(EmpresaId, Nombre);
END;

IF COL_LENGTH('dbo.Productos', 'UnidadVentaId') IS NULL
    ALTER TABLE dbo.Productos ADD UnidadVentaId INT NULL;
IF COL_LENGTH('dbo.Productos', 'UnidadInventarioId') IS NULL
    ALTER TABLE dbo.Productos ADD UnidadInventarioId INT NULL;
IF COL_LENGTH('dbo.Productos', 'FactorConversionInventario') IS NULL
    ALTER TABLE dbo.Productos ADD FactorConversionInventario DECIMAL(18,6) NOT NULL CONSTRAINT DF_Productos_FactorInventario DEFAULT 1;

IF OBJECT_ID('dbo.ProductoRecetas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductoRecetas (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        ProductoVentaId INT NOT NULL,
        InsumoProductoId INT NOT NULL,
        UnidadMedidaId INT NULL,
        Cantidad DECIMAL(18,6) NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_ProductoRecetas_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_ProductoRecetas_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_ProductoRecetas_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_ProductoRecetas_ProductoVenta FOREIGN KEY (ProductoVentaId) REFERENCES dbo.Productos(Id),
        CONSTRAINT FK_ProductoRecetas_Insumo FOREIGN KEY (InsumoProductoId) REFERENCES dbo.Productos(Id),
        CONSTRAINT FK_ProductoRecetas_Unidad FOREIGN KEY (UnidadMedidaId) REFERENCES dbo.UnidadesMedida(Id)
    );
    CREATE UNIQUE INDEX UX_ProductoRecetas_EmpresaProductoInsumo ON dbo.ProductoRecetas(EmpresaId, ProductoVentaId, InsumoProductoId);
END;

IF OBJECT_ID('dbo.InventarioLotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventarioLotes (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        ProductoId INT NOT NULL,
        ProveedorId INT NULL,
        CodigoLote NVARCHAR(80) NOT NULL,
        FechaVencimiento DATETIME2 NULL,
        CantidadActual DECIMAL(18,3) NOT NULL CONSTRAINT DF_InventarioLotes_CantidadActual DEFAULT 0,
        CostoUnitario DECIMAL(18,2) NOT NULL CONSTRAINT DF_InventarioLotes_Costo DEFAULT 0,
        Estado BIT NOT NULL CONSTRAINT DF_InventarioLotes_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_InventarioLotes_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_InventarioLotes_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_InventarioLotes_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(Id),
        CONSTRAINT FK_InventarioLotes_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id),
        CONSTRAINT FK_InventarioLotes_Proveedores FOREIGN KEY (ProveedorId) REFERENCES dbo.Proveedores(Id)
    );
    CREATE UNIQUE INDEX UX_InventarioLotes_EmpresaSucursalProductoCodigo ON dbo.InventarioLotes(EmpresaId, SucursalId, ProductoId, CodigoLote);
END;

IF OBJECT_ID('dbo.InventarioCompras', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventarioCompras (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        ProveedorId INT NULL,
        UsuarioId INT NOT NULL,
        NumeroFactura NVARCHAR(80) NULL,
        FechaCompra DATETIME2 NOT NULL CONSTRAINT DF_InventarioCompras_FechaCompra DEFAULT SYSUTCDATETIME(),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_InventarioCompras_Total DEFAULT 0,
        Observacion NVARCHAR(250) NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_InventarioCompras_FechaCreacion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_InventarioCompras_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_InventarioCompras_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(Id),
        CONSTRAINT FK_InventarioCompras_Proveedores FOREIGN KEY (ProveedorId) REFERENCES dbo.Proveedores(Id),
        CONSTRAINT FK_InventarioCompras_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
    );
END;

IF OBJECT_ID('dbo.InventarioCompraDetalles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventarioCompraDetalles (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompraId INT NOT NULL,
        ProductoId INT NOT NULL,
        LoteId INT NULL,
        Cantidad DECIMAL(18,3) NOT NULL,
        CostoUnitario DECIMAL(18,2) NOT NULL,
        CodigoLote NVARCHAR(80) NULL,
        FechaVencimiento DATETIME2 NULL,
        Total DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_InventarioCompraDetalles_Compras FOREIGN KEY (CompraId) REFERENCES dbo.InventarioCompras(Id),
        CONSTRAINT FK_InventarioCompraDetalles_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id),
        CONSTRAINT FK_InventarioCompraDetalles_Lotes FOREIGN KEY (LoteId) REFERENCES dbo.InventarioLotes(Id)
    );
END;

IF COL_LENGTH('dbo.InventarioMovimientos', 'LoteId') IS NULL
    ALTER TABLE dbo.InventarioMovimientos ADD LoteId INT NULL;
IF COL_LENGTH('dbo.InventarioMovimientos', 'CompraId') IS NULL
    ALTER TABLE dbo.InventarioMovimientos ADD CompraId INT NULL;

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM dbo.Empresas ORDER BY Id);
IF @EmpresaId IS NOT NULL
BEGIN
    INSERT INTO dbo.UnidadesMedida (EmpresaId, Codigo, Nombre, Decimales)
    SELECT @EmpresaId, v.Codigo, v.Nombre, v.Decimales
    FROM (VALUES
        ('UND', 'Unidad', 0),
        ('BOT', 'Botella', 0),
        ('COPA', 'Copa', 2),
        ('ML', 'Mililitro', 3),
        ('GR', 'Gramo', 3),
        ('PAQ', 'Paquete', 0)
    ) v(Codigo, Nombre, Decimales)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.UnidadesMedida u WHERE u.EmpresaId = @EmpresaId AND u.Codigo = v.Codigo);
END;

DECLARE @ProductosId INT = (SELECT Id FROM dbo.Modulos WHERE Nombre = 'Productos');
IF @ProductosId IS NOT NULL
BEGIN
    UPDATE dbo.Ventanas SET Nombre = 'Inventario avanzado' WHERE Ruta = '/productos/inventario';
END;

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/productos/inventario', 'Productos.Inventario.Entrada', 'Registrar entradas', 'Registrar entradas manuales de inventario'),
('/productos/inventario', 'Productos.Inventario.Salida', 'Registrar salidas', 'Registrar salidas manuales de inventario'),
('/productos/inventario', 'Productos.Inventario.Ajuste', 'Registrar ajustes', 'Registrar ajustes, danos, vencimientos y devoluciones'),
('/productos/inventario', 'Productos.Inventario.Compras', 'Gestionar compras', 'Registrar proveedores, compras y lotes'),
('/productos/inventario', 'Productos.Inventario.Costos', 'Ver costos', 'Consultar costos de inventario'),
('/productos/inventario', 'Productos.Inventario.Reportes', 'Ver reportes', 'Consultar reportes de inventario');

INSERT INTO dbo.Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT p.Codigo, p.Nombre, p.Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO dbo.RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId, pe.Id, v.Id, 1,
       CASE WHEN p.Codigo LIKE '%.Entrada' OR p.Codigo LIKE '%.Compras' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Costos' OR p.Codigo LIKE '%.Reportes' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Ajuste' OR p.Codigo LIKE '%.Salida' THEN 1 ELSE 0 END,
       0
FROM @Permisos p
JOIN dbo.Permisos pe ON pe.Codigo = p.Codigo
JOIN dbo.Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = pe.Id AND rp.VentanaId = v.Id
  );
