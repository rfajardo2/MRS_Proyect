/* Caja y cierres de turno. */

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('CajaTurnos', 'U') IS NULL
BEGIN
    CREATE TABLE CajaTurnos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        UsuarioAperturaId INT NOT NULL,
        UsuarioCierreId INT NULL,
        FechaOperativa DATE NOT NULL,
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_CajaTurnos_Estado DEFAULT 'Abierta',
        SaldoInicial DECIMAL(18,2) NOT NULL CONSTRAINT DF_CajaTurnos_SaldoInicial DEFAULT 0,
        TotalVentas DECIMAL(18,2) NOT NULL CONSTRAINT DF_CajaTurnos_TotalVentas DEFAULT 0,
        TotalPagos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CajaTurnos_TotalPagos DEFAULT 0,
        TotalEfectivo DECIMAL(18,2) NOT NULL CONSTRAINT DF_CajaTurnos_TotalEfectivo DEFAULT 0,
        EfectivoEsperado DECIMAL(18,2) NOT NULL CONSTRAINT DF_CajaTurnos_EfectivoEsperado DEFAULT 0,
        EfectivoReal DECIMAL(18,2) NULL,
        Diferencia DECIMAL(18,2) NULL,
        ObservacionApertura NVARCHAR(300) NULL,
        ObservacionCierre NVARCHAR(300) NULL,
        FechaApertura DATETIME2 NOT NULL CONSTRAINT DF_CajaTurnos_FechaApertura DEFAULT SYSUTCDATETIME(),
        FechaCierre DATETIME2 NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CajaTurnos_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_CajaTurnos_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_CajaTurnos_Sucursales FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
        CONSTRAINT FK_CajaTurnos_UsuarioApertura FOREIGN KEY (UsuarioAperturaId) REFERENCES Usuarios(Id),
        CONSTRAINT FK_CajaTurnos_UsuarioCierre FOREIGN KEY (UsuarioCierreId) REFERENCES Usuarios(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CajaTurnos_Abierta_EmpresaSucursalFecha' AND object_id = OBJECT_ID('CajaTurnos'))
    CREATE UNIQUE INDEX UX_CajaTurnos_Abierta_EmpresaSucursalFecha ON CajaTurnos(EmpresaId, SucursalId, FechaOperativa) WHERE Estado = 'Abierta';
GO

DECLARE @OperacionId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Operacion');
IF @OperacionId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Operacion', 'fa-cash-register', 7, 1);
    SET @OperacionId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/operacion/caja')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@OperacionId, 'Caja', '/operacion/caja', 'fa-cash-register', 3, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/operacion/caja', 'Operacion.Caja.Ver', 'Ver caja', 'Consultar caja abierta e historial de cierres'),
('/operacion/caja', 'Operacion.Caja.Crear', 'Abrir caja', 'Abrir un turno de caja'),
('/operacion/caja', 'Operacion.Caja.Cerrar', 'Cerrar caja', 'Cerrar turno de caja y registrar arqueo'),
('/operacion/caja', 'Operacion.Caja.Editar', 'Editar caja', 'Actualizar datos operativos de caja');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId,
       pe.Id,
       v.Id,
       CASE WHEN p.Codigo LIKE '%.Ver' OR p.Codigo LIKE '%.Cerrar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Crear' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Ver' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Editar' OR p.Codigo LIKE '%.Cerrar' THEN 1 ELSE 0 END,
       0
FROM @Permisos p
JOIN Permisos pe ON pe.Codigo = p.Codigo
JOIN Ventanas v ON v.Ruta = p.Ruta
WHERE @SuperRolId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = pe.Id AND rp.VentanaId = v.Id
  );
GO
