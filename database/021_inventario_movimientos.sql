/* Inventario: stock por sede, kardex de movimientos y permisos. */

IF OBJECT_ID('InventarioStocks', 'U') IS NULL
BEGIN
    CREATE TABLE InventarioStocks (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        ProductoId INT NOT NULL,
        CantidadActual DECIMAL(18,3) NOT NULL CONSTRAINT DF_InventarioStocks_CantidadActual DEFAULT 0,
        CantidadMinima DECIMAL(18,3) NOT NULL CONSTRAINT DF_InventarioStocks_CantidadMinima DEFAULT 0,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_InventarioStocks_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_InventarioStocks_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_InventarioStocks_Sucursales FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
        CONSTRAINT FK_InventarioStocks_Productos FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_InventarioStocks_EmpresaSucursalProducto' AND object_id = OBJECT_ID('InventarioStocks'))
    CREATE UNIQUE INDEX UX_InventarioStocks_EmpresaSucursalProducto ON InventarioStocks(EmpresaId, SucursalId, ProductoId);
GO

IF OBJECT_ID('InventarioMovimientos', 'U') IS NULL
BEGIN
    CREATE TABLE InventarioMovimientos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        ProductoId INT NOT NULL,
        UsuarioId INT NOT NULL,
        CuentaId INT NULL,
        CuentaItemId INT NULL,
        Tipo NVARCHAR(40) NOT NULL,
        Cantidad DECIMAL(18,3) NOT NULL,
        SaldoAnterior DECIMAL(18,3) NOT NULL,
        SaldoNuevo DECIMAL(18,3) NOT NULL,
        CostoUnitario DECIMAL(18,2) NULL,
        Referencia NVARCHAR(160) NULL,
        Motivo NVARCHAR(300) NULL,
        FechaMovimiento DATETIME2 NOT NULL CONSTRAINT DF_InventarioMovimientos_FechaMovimiento DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_InventarioMovimientos_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_InventarioMovimientos_Sucursales FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
        CONSTRAINT FK_InventarioMovimientos_Productos FOREIGN KEY (ProductoId) REFERENCES Productos(Id),
        CONSTRAINT FK_InventarioMovimientos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
        CONSTRAINT FK_InventarioMovimientos_Cuentas FOREIGN KEY (CuentaId) REFERENCES Cuentas(Id),
        CONSTRAINT FK_InventarioMovimientos_CuentaItems FOREIGN KEY (CuentaItemId) REFERENCES CuentaItems(Id)
    );
END
GO

IF COL_LENGTH('CuentaItems', 'InventarioAplicado') IS NULL
    ALTER TABLE CuentaItems ADD InventarioAplicado BIT NOT NULL CONSTRAINT DF_CuentaItems_InventarioAplicado DEFAULT 0;
GO

DECLARE @ProductosId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Productos');
IF @ProductosId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Productos', 'fa-martini-glass-citrus', 6, 1);
    SET @ProductosId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/productos/inventario')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@ProductosId, 'Inventario', '/productos/inventario', 'fa-boxes-stacked', 3, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/productos/inventario', 'Productos.Inventario.Ver', 'Ver inventario', 'Consultar stock y movimientos de inventario'),
('/productos/inventario', 'Productos.Inventario.Mover', 'Registrar movimientos', 'Registrar entradas, salidas, ajustes y devoluciones'),
('/productos/inventario', 'Productos.Inventario.Editar', 'Editar inventario', 'Configurar stock minimo');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId,
       pe.Id,
       v.Id,
       CASE WHEN p.Codigo LIKE '%.Ver' OR p.Codigo LIKE '%.Mover' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Mover' THEN 1 ELSE 0 END,
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
