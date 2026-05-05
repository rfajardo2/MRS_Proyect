USE MRSDrunkDb;
GO

IF COL_LENGTH('RolPermisos', 'PuedeConsultar') IS NULL
BEGIN
    ALTER TABLE RolPermisos ADD PuedeConsultar BIT NOT NULL CONSTRAINT DF_RolPermisos_Consultar DEFAULT 0;
END
GO

IF OBJECT_ID('NominaRegistros', 'U') IS NULL
BEGIN
    CREATE TABLE NominaEmpleados (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        NombreCompleto NVARCHAR(160) NOT NULL,
        Documento NVARCHAR(50) NULL,
        Cargo NVARCHAR(100) NULL,
        ValorDiaBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_NominaEmpleados_ValorDiaBase DEFAULT 0,
        Estado BIT NOT NULL CONSTRAINT DF_NominaEmpleados_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_NominaEmpleados_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_NominaEmpleados_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
    );

    CREATE TABLE NominaPeriodos (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmpresaId INT NOT NULL,
        Anio INT NOT NULL,
        Mes INT NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        FechaInicio DATE NOT NULL,
        FechaFin DATE NOT NULL,
        Cerrado BIT NOT NULL CONSTRAINT DF_NominaPeriodos_Cerrado DEFAULT 0,
        Estado BIT NOT NULL CONSTRAINT DF_NominaPeriodos_Estado DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_NominaPeriodos_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_NominaPeriodos_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
    );

    CREATE UNIQUE INDEX UX_NominaPeriodos_EmpresaAnioMes ON NominaPeriodos(EmpresaId, Anio, Mes);

    CREATE TABLE NominaRegistros (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PeriodoId INT NOT NULL,
        EmpleadoId INT NOT NULL,
        Fecha DATE NOT NULL,
        Concepto NVARCHAR(60) NOT NULL CONSTRAINT DF_NominaRegistros_Concepto DEFAULT 'Dia',
        EstadoDia NVARCHAR(40) NOT NULL CONSTRAINT DF_NominaRegistros_EstadoDia DEFAULT 'Trabajado',
        Valor DECIMAL(18,2) NOT NULL CONSTRAINT DF_NominaRegistros_Valor DEFAULT 0,
        Observacion NVARCHAR(250) NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_NominaRegistros_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion DATETIME2 NULL,
        CONSTRAINT FK_NominaRegistros_Periodos FOREIGN KEY (PeriodoId) REFERENCES NominaPeriodos(Id),
        CONSTRAINT FK_NominaRegistros_Empleados FOREIGN KEY (EmpleadoId) REFERENCES NominaEmpleados(Id)
    );

    CREATE UNIQUE INDEX UX_NominaRegistros_PeriodoEmpleadoFechaConcepto ON NominaRegistros(PeriodoId, EmpleadoId, Fecha, Concepto);

    CREATE TABLE NominaPeriodoDiasNoLaborados (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PeriodoId INT NOT NULL,
        Fecha DATE NOT NULL,
        Motivo NVARCHAR(160) NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_NominaPeriodoDiasNoLaborados_FechaCreacion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_NominaPeriodoDiasNoLaborados_Periodos FOREIGN KEY (PeriodoId) REFERENCES NominaPeriodos(Id)
    );

    CREATE UNIQUE INDEX UX_NominaPeriodoDiasNoLaborados_PeriodoFecha ON NominaPeriodoDiasNoLaborados(PeriodoId, Fecha);
END
GO

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Nombre = 'Nomina')
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Nomina', 'fa-money-check-dollar', 4, 1);
GO

DECLARE @NominaId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Nomina');

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Control', '/nomina', 'fa-calendar-days', 1, 1);
GO

DECLARE @ControlVentanaId INT = (SELECT Id FROM Ventanas WHERE Ruta = '/nomina');
DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE EsSuperUsuario = 1 ORDER BY Id);

DECLARE @Permisos TABLE (Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('Nomina.Control.Ver', 'Ver nomina', 'Ver el modulo de nomina en el menu'),
('Nomina.Control.Crear', 'Crear nomina', 'Crear empleados y periodos de nomina'),
('Nomina.Control.Consultar', 'Consultar nomina', 'Consultar el control mensual de nomina'),
('Nomina.Control.Editar', 'Editar nomina', 'Editar registros diarios de nomina'),
('Nomina.Control.Eliminar', 'Eliminar nomina', 'Inactivar empleados de nomina'),
('Nomina.Control.CerrarPeriodo', 'Cerrar periodo', 'Cerrar el periodo de nomina para evitar cambios'),
('Nomina.Control.AprobarPago', 'Aprobar pago', 'Marcar o aprobar pagos de nomina en futuras versiones');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId, p.Id, @ControlVentanaId,
       CASE WHEN p.Codigo LIKE '%.Ver' OR p.Codigo LIKE '%.CerrarPeriodo' OR p.Codigo LIKE '%.AprobarPago' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Crear' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Consultar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Editar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Eliminar' THEN 1 ELSE 0 END
FROM Permisos p
WHERE @SuperRolId IS NOT NULL
  AND p.Codigo LIKE 'Nomina.Control.%'
  AND NOT EXISTS (
      SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = p.Id AND rp.VentanaId = @ControlVentanaId
  );
GO

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM Empresas WHERE Nit = '000000000' ORDER BY Id);

IF @EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM NominaEmpleados WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO NominaEmpleados (EmpresaId, NombreCompleto, Cargo, ValorDiaBase, Estado)
    VALUES
    (@EmpresaId, 'Majo', 'Bar', 60000, 1),
    (@EmpresaId, 'Natalia', 'Bar', 60000, 1),
    (@EmpresaId, 'Jairo', 'Bar', 60000, 1),
    (@EmpresaId, 'Ella', 'Bar', 60000, 1),
    (@EmpresaId, 'Disp', 'Apoyo', 60000, 1);
END
GO
