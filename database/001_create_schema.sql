IF DB_ID('MRSDrunkDb') IS NULL
BEGIN
    CREATE DATABASE MRSDrunkDb;
END
GO

USE MRSDrunkDb;
GO

CREATE TABLE Empresas (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(160) NOT NULL,
    Nit NVARCHAR(40) NOT NULL,
    LogoUrl NVARCHAR(500) NULL,
    Estado BIT NOT NULL CONSTRAINT DF_Empresas_Estado DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Empresas_FechaCreacion DEFAULT SYSUTCDATETIME(),
    FechaModificacion DATETIME2 NULL
);
GO

CREATE TABLE Sucursales (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EmpresaId INT NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Direccion NVARCHAR(250) NULL,
    Estado BIT NOT NULL CONSTRAINT DF_Sucursales_Estado DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Sucursales_FechaCreacion DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Sucursales_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
);
GO

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EmpresaId INT NULL,
    Nombre NVARCHAR(80) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    EsSuperUsuario BIT NOT NULL CONSTRAINT DF_Roles_EsSuperUsuario DEFAULT 0,
    Estado BIT NOT NULL CONSTRAINT DF_Roles_Estado DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Roles_FechaCreacion DEFAULT SYSUTCDATETIME(),
    FechaModificacion DATETIME2 NULL,
    CONSTRAINT FK_Roles_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
);
GO

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EmpresaId INT NOT NULL,
    SucursalId INT NULL,
    NombreCompleto NVARCHAR(160) NOT NULL,
    Usuario NVARCHAR(80) NOT NULL,
    Correo NVARCHAR(180) NOT NULL,
    PasswordHash NVARCHAR(250) NOT NULL,
    RolId INT NOT NULL,
    Estado BIT NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT SYSUTCDATETIME(),
    FechaModificacion DATETIME2 NULL,
    UsuarioCreacion INT NULL,
    UsuarioModificacion INT NULL,
    CONSTRAINT FK_Usuarios_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
    CONSTRAINT FK_Usuarios_Sucursales FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES Roles(Id)
);
GO

CREATE UNIQUE INDEX UX_Usuarios_Usuario ON Usuarios(Usuario);
CREATE UNIQUE INDEX UX_Usuarios_Correo ON Usuarios(Correo);
GO

CREATE TABLE Modulos (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(80) NOT NULL,
    Icono NVARCHAR(80) NULL,
    Orden INT NOT NULL CONSTRAINT DF_Modulos_Orden DEFAULT 0,
    Estado BIT NOT NULL CONSTRAINT DF_Modulos_Estado DEFAULT 1
);
GO

CREATE TABLE Ventanas (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ModuloId INT NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Ruta NVARCHAR(160) NOT NULL,
    Icono NVARCHAR(80) NULL,
    Orden INT NOT NULL CONSTRAINT DF_Ventanas_Orden DEFAULT 0,
    Estado BIT NOT NULL CONSTRAINT DF_Ventanas_Estado DEFAULT 1,
    CONSTRAINT FK_Ventanas_Modulos FOREIGN KEY (ModuloId) REFERENCES Modulos(Id)
);
GO

CREATE TABLE Permisos (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Codigo NVARCHAR(120) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    Estado BIT NOT NULL CONSTRAINT DF_Permisos_Estado DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_Permisos_Codigo ON Permisos(Codigo);
GO

CREATE TABLE RolPermisos (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RolId INT NOT NULL,
    PermisoId INT NOT NULL,
    VentanaId INT NOT NULL,
    PuedeVer BIT NOT NULL CONSTRAINT DF_RolPermisos_Ver DEFAULT 0,
    PuedeCrear BIT NOT NULL CONSTRAINT DF_RolPermisos_Crear DEFAULT 0,
    PuedeConsultar BIT NOT NULL CONSTRAINT DF_RolPermisos_Consultar DEFAULT 0,
    PuedeEditar BIT NOT NULL CONSTRAINT DF_RolPermisos_Editar DEFAULT 0,
    PuedeEliminar BIT NOT NULL CONSTRAINT DF_RolPermisos_Eliminar DEFAULT 0,
    CONSTRAINT FK_RolPermisos_Roles FOREIGN KEY (RolId) REFERENCES Roles(Id),
    CONSTRAINT FK_RolPermisos_Permisos FOREIGN KEY (PermisoId) REFERENCES Permisos(Id),
    CONSTRAINT FK_RolPermisos_Ventanas FOREIGN KEY (VentanaId) REFERENCES Ventanas(Id)
);
GO

CREATE UNIQUE INDEX UX_RolPermisos_RolPermisoVentana ON RolPermisos(RolId, PermisoId, VentanaId);
GO

CREATE TABLE RefreshTokens (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UsuarioId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    FechaExpiracion DATETIME2 NOT NULL,
    Estado BIT NOT NULL CONSTRAINT DF_RefreshTokens_Estado DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_RefreshTokens_FechaCreacion DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_RefreshTokens_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);
GO
