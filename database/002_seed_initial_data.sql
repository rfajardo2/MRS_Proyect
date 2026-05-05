USE MRSDrunkDb;
GO

IF NOT EXISTS (SELECT 1 FROM Empresas WHERE Nit = '000000000')
BEGIN
    INSERT INTO Empresas (Nombre, Nit, LogoUrl, Estado)
    VALUES ('MRS Drunk', '000000000', NULL, 1);
END
GO

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM Empresas WHERE Nit = '000000000');

IF NOT EXISTS (SELECT 1 FROM Sucursales WHERE EmpresaId = @EmpresaId AND Nombre = 'Local Principal')
BEGIN
    INSERT INTO Sucursales (EmpresaId, Nombre, Direccion, Estado)
    VALUES (@EmpresaId, 'Local Principal', 'Direccion demo', 1);
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = 'SuperUsuario' AND EmpresaId IS NULL)
BEGIN
    INSERT INTO Roles (EmpresaId, Nombre, Descripcion, EsSuperUsuario, Estado)
    VALUES (NULL, 'SuperUsuario', 'Acceso total al sistema', 1, 1);
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = 'Administrador' AND EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO Roles (EmpresaId, Nombre, Descripcion, EsSuperUsuario, Estado)
    VALUES (@EmpresaId, 'Administrador', 'Administrador de empresa', 0, 1);
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = 'Usuario' AND EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO Roles (EmpresaId, Nombre, Descripcion, EsSuperUsuario, Estado)
    VALUES (@EmpresaId, 'Usuario', 'Usuario operativo', 0, 1);
END

DECLARE @SuperRolId INT = (SELECT TOP 1 Id FROM Roles WHERE Nombre = 'SuperUsuario' AND EsSuperUsuario = 1);
DECLARE @SucursalId INT = (SELECT TOP 1 Id FROM Sucursales WHERE EmpresaId = @EmpresaId);

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Usuario = 'admin')
BEGIN
    INSERT INTO Usuarios (EmpresaId, SucursalId, NombreCompleto, Usuario, Correo, PasswordHash, RolId, Estado)
    VALUES (@EmpresaId, @SucursalId, 'Administrador MRS Drunk', 'admin', 'admin@mrsdrunk.com', '$2a$11$Y3Ma08VeRX6nntyVztNNBeN0OWg8.d3okCkPmlrVvGGWofa/SiwdW', @SuperRolId, 1);
END

IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Nombre = 'Dashboard')
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Dashboard', 'fa-gauge-high', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Nombre = 'Seguridad')
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Seguridad', 'fa-shield-halved', 2, 1);
IF NOT EXISTS (SELECT 1 FROM Modulos WHERE Nombre = 'Configuracion')
    INSERT INTO Modulos (Nombre, Icono, Orden, Estado) VALUES ('Configuracion', 'fa-gear', 3, 1);

DECLARE @DashboardId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Dashboard');
DECLARE @SeguridadId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Seguridad');
DECLARE @ConfiguracionId INT = (SELECT Id FROM Modulos WHERE Nombre = 'Configuracion');

IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/home')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@DashboardId, 'Home', '/home', 'fa-house', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/usuarios')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@SeguridadId, 'Usuarios', '/usuarios', 'fa-users', 1, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/roles')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@SeguridadId, 'Roles', '/roles', 'fa-user-shield', 2, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/permisos')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@SeguridadId, 'Permisos', '/permisos', 'fa-key', 3, 1);
IF NOT EXISTS (SELECT 1 FROM Ventanas WHERE Ruta = '/empresas')
    INSERT INTO Ventanas (ModuloId, Nombre, Ruta, Icono, Orden, Estado) VALUES (@ConfiguracionId, 'Empresas', '/empresas', 'fa-building', 1, 1);

DECLARE @Permisos TABLE (Codigo NVARCHAR(120), Nombre NVARCHAR(120), Descripcion NVARCHAR(250));
INSERT INTO @Permisos VALUES
('Dashboard.Home.Ver', 'Ver dashboard', 'Acceso al dashboard principal'),
('Seguridad.Usuarios.Ver', 'Ver usuarios', 'Listar y consultar usuarios'),
('Seguridad.Usuarios.Consultar', 'Consultar usuario', 'Ver detalle de usuarios'),
('Seguridad.Usuarios.Crear', 'Crear usuario', 'Crear usuarios'),
('Seguridad.Usuarios.Editar', 'Editar usuario', 'Editar usuarios'),
('Seguridad.Usuarios.Eliminar', 'Inactivar usuario', 'Activar o inactivar usuarios'),
('Seguridad.Usuarios.CambiarPassword', 'Cambiar contraseña', 'Cambiar la contraseña de otros usuarios'),
('Seguridad.Roles.Ver', 'Ver roles', 'Listar y consultar roles'),
('Seguridad.Roles.Consultar', 'Consultar rol', 'Ver detalle de roles'),
('Seguridad.Roles.Crear', 'Crear rol', 'Crear roles'),
('Seguridad.Roles.Editar', 'Editar rol', 'Editar roles'),
('Seguridad.Roles.Eliminar', 'Inactivar rol', 'Activar o inactivar roles'),
('Seguridad.Permisos.Ver', 'Ver permisos', 'Consultar permisos'),
('Seguridad.Permisos.Consultar', 'Consultar permisos', 'Ver detalle de permisos'),
('Seguridad.Permisos.Editar', 'Editar permisos', 'Asignar permisos por rol'),
('Configuracion.Empresas.Ver', 'Ver empresas', 'Listar empresas'),
('Configuracion.Empresas.Consultar', 'Consultar empresa', 'Ver detalle de empresas'),
('Configuracion.Empresas.Crear', 'Crear empresa', 'Crear empresas'),
('Configuracion.Empresas.Editar', 'Editar empresa', 'Editar empresas');

INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
SELECT Codigo, Nombre, Descripcion, 1
FROM @Permisos p
WHERE NOT EXISTS (SELECT 1 FROM Permisos x WHERE x.Codigo = p.Codigo);

INSERT INTO RolPermisos (RolId, PermisoId, VentanaId, PuedeVer, PuedeCrear, PuedeConsultar, PuedeEditar, PuedeEliminar)
SELECT @SuperRolId, p.Id, v.Id,
       CASE WHEN p.Codigo LIKE '%.Ver' OR p.Codigo LIKE '%.CambiarPassword' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Crear' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Consultar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Editar' THEN 1 ELSE 0 END,
       CASE WHEN p.Codigo LIKE '%.Eliminar' THEN 1 ELSE 0 END
FROM Permisos p
JOIN Ventanas v ON p.Codigo LIKE REPLACE((SELECT Nombre FROM Modulos WHERE Id = v.ModuloId), 'ó', 'o') + '.' + v.Nombre + '.%'
WHERE NOT EXISTS (
    SELECT 1 FROM RolPermisos rp WHERE rp.RolId = @SuperRolId AND rp.PermisoId = p.Id AND rp.VentanaId = v.Id
);
GO
