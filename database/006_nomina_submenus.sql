USE MRSDrunkDb;
GO

DECLARE @NominaId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Nomina');
IF @NominaId IS NULL
BEGIN
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Nomina', 'fa-money-check-dollar', 4, 1);
    SET @NominaId = SCOPE_IDENTITY();
END

IF EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina')
BEGIN
    UPDATE Ventanas
    SET Nombre = 'Resumen', Ruta = '/nomina/resumen', Icono = 'fa-chart-pie', Orden = 1
    WHERE Ruta = '/nomina';
END

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina/resumen')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Resumen', '/nomina/resumen', 'fa-chart-pie', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina/registro-diario')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Registro diario', '/nomina/registro-diario', 'fa-clipboard-check', 2, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina/control-diario')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Control diario', '/nomina/control-diario', 'fa-table', 3, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina/novedades')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Novedades', '/nomina/novedades', 'fa-receipt', 4, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina/empleados')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Empleados', '/nomina/empleados', 'fa-users', 5, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/nomina/periodos')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@NominaId, 'Periodos', '/nomina/periodos', 'fa-calendar-days', 6, 1);
GO

DECLARE @Permisos TABLE (Ruta NVARCHAR(160), Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('/nomina/resumen', 'Nomina.Resumen.Ver', 'Ver resumen de nomina', 'Ver resumen y totales de nomina'),
('/nomina/registro-diario', 'Nomina.RegistroDiario.Ver', 'Ver registro diario', 'Ver la ventana de registro diario'),
('/nomina/registro-diario', 'Nomina.RegistroDiario.Editar', 'Editar registro diario', 'Guardar control diario de empleados'),
('/nomina/control-diario', 'Nomina.ControlDiario.Ver', 'Ver control diario', 'Consultar grilla mensual de nomina'),
('/nomina/novedades', 'Nomina.Novedades.Ver', 'Ver novedades', 'Consultar novedades de nomina'),
('/nomina/novedades', 'Nomina.Novedades.Crear', 'Crear novedades', 'Crear comisiones, bonos y descuentos'),
('/nomina/novedades', 'Nomina.Novedades.Eliminar', 'Eliminar novedades', 'Eliminar novedades del periodo'),
('/nomina/empleados', 'Nomina.Empleados.Ver', 'Ver empleados de nomina', 'Consultar empleados de nomina'),
('/nomina/empleados', 'Nomina.Empleados.Crear', 'Crear empleados de nomina', 'Crear empleados de nomina'),
('/nomina/empleados', 'Nomina.Empleados.Editar', 'Editar empleados de nomina', 'Editar empleados de nomina'),
('/nomina/empleados', 'Nomina.Empleados.Eliminar', 'Eliminar empleados de nomina', 'Inactivar empleados de nomina'),
('/nomina/periodos', 'Nomina.Periodos.Ver', 'Ver periodos de nomina', 'Consultar periodos de nomina'),
('/nomina/periodos', 'Nomina.Periodos.Crear', 'Crear periodos de nomina', 'Crear periodos de nomina'),
('/nomina/periodos', 'Nomina.Periodos.Editar', 'Editar periodos de nomina', 'Configurar fechas y dias no laborados');

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
