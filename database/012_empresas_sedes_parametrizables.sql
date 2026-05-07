USE MRSDrunkDb;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('Empresas', 'DigitoVerificacion') IS NULL
    ALTER TABLE Empresas ADD DigitoVerificacion NVARCHAR(2) NULL;
IF COL_LENGTH('Empresas', 'RazonSocial') IS NULL
    ALTER TABLE Empresas ADD RazonSocial NVARCHAR(180) NOT NULL CONSTRAINT DF_Empresas_RazonSocial DEFAULT '';
IF COL_LENGTH('Empresas', 'NombreComercial') IS NULL
    ALTER TABLE Empresas ADD NombreComercial NVARCHAR(180) NULL;
IF COL_LENGTH('Empresas', 'TipoDocumento') IS NULL
    ALTER TABLE Empresas ADD TipoDocumento NVARCHAR(20) NOT NULL CONSTRAINT DF_Empresas_TipoDocumento DEFAULT 'NIT';
IF COL_LENGTH('Empresas', 'RegimenTributario') IS NULL
    ALTER TABLE Empresas ADD RegimenTributario NVARCHAR(80) NOT NULL CONSTRAINT DF_Empresas_RegimenTributario DEFAULT 'Responsable de IVA';
IF COL_LENGTH('Empresas', 'ResponsabilidadFiscal') IS NULL
    ALTER TABLE Empresas ADD ResponsabilidadFiscal NVARCHAR(80) NOT NULL CONSTRAINT DF_Empresas_ResponsabilidadFiscal DEFAULT 'R-99-PN';
IF COL_LENGTH('Empresas', 'MatriculaMercantil') IS NULL
    ALTER TABLE Empresas ADD MatriculaMercantil NVARCHAR(80) NULL;
IF COL_LENGTH('Empresas', 'ActividadEconomicaCiiu') IS NULL
    ALTER TABLE Empresas ADD ActividadEconomicaCiiu NVARCHAR(20) NULL;
IF COL_LENGTH('Empresas', 'DireccionFiscal') IS NULL
    ALTER TABLE Empresas ADD DireccionFiscal NVARCHAR(250) NULL;
IF COL_LENGTH('Empresas', 'Departamento') IS NULL
    ALTER TABLE Empresas ADD Departamento NVARCHAR(80) NULL;
IF COL_LENGTH('Empresas', 'Municipio') IS NULL
    ALTER TABLE Empresas ADD Municipio NVARCHAR(80) NULL;
IF COL_LENGTH('Empresas', 'Pais') IS NULL
    ALTER TABLE Empresas ADD Pais NVARCHAR(5) NOT NULL CONSTRAINT DF_Empresas_Pais DEFAULT 'CO';
IF COL_LENGTH('Empresas', 'Telefono') IS NULL
    ALTER TABLE Empresas ADD Telefono NVARCHAR(40) NULL;
IF COL_LENGTH('Empresas', 'CorreoFacturacion') IS NULL
    ALTER TABLE Empresas ADD CorreoFacturacion NVARCHAR(180) NULL;
IF COL_LENGTH('Empresas', 'RepresentanteLegal') IS NULL
    ALTER TABLE Empresas ADD RepresentanteLegal NVARCHAR(160) NULL;
IF COL_LENGTH('Empresas', 'DocumentoRepresentante') IS NULL
    ALTER TABLE Empresas ADD DocumentoRepresentante NVARCHAR(40) NULL;
IF COL_LENGTH('Empresas', 'EsPrincipal') IS NULL
    ALTER TABLE Empresas ADD EsPrincipal BIT NOT NULL CONSTRAINT DF_Empresas_EsPrincipal DEFAULT 0;
GO

UPDATE Empresas
SET RazonSocial = Nombre
WHERE (RazonSocial IS NULL OR RazonSocial = '');

IF NOT EXISTS (SELECT 1 FROM Empresas WHERE EsPrincipal = 1)
BEGIN
    UPDATE Empresas
    SET EsPrincipal = 1
    WHERE Id = (SELECT TOP 1 Id FROM Empresas ORDER BY Id);
END
GO

IF COL_LENGTH('Sucursales', 'Codigo') IS NULL
    ALTER TABLE Sucursales ADD Codigo NVARCHAR(40) NULL;
IF COL_LENGTH('Sucursales', 'Departamento') IS NULL
    ALTER TABLE Sucursales ADD Departamento NVARCHAR(80) NULL;
IF COL_LENGTH('Sucursales', 'Municipio') IS NULL
    ALTER TABLE Sucursales ADD Municipio NVARCHAR(80) NULL;
IF COL_LENGTH('Sucursales', 'Pais') IS NULL
    ALTER TABLE Sucursales ADD Pais NVARCHAR(5) NOT NULL CONSTRAINT DF_Sucursales_Pais DEFAULT 'CO';
IF COL_LENGTH('Sucursales', 'Telefono') IS NULL
    ALTER TABLE Sucursales ADD Telefono NVARCHAR(40) NULL;
IF COL_LENGTH('Sucursales', 'Correo') IS NULL
    ALTER TABLE Sucursales ADD Correo NVARCHAR(180) NULL;
IF COL_LENGTH('Sucursales', 'EsPrincipal') IS NULL
    ALTER TABLE Sucursales ADD EsPrincipal BIT NOT NULL CONSTRAINT DF_Sucursales_EsPrincipal DEFAULT 0;
IF COL_LENGTH('Sucursales', 'FechaModificacion') IS NULL
    ALTER TABLE Sucursales ADD FechaModificacion DATETIME2 NULL;
GO

;WITH primeras AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY EmpresaId ORDER BY Id) AS rn
    FROM Sucursales
)
UPDATE s
SET EsPrincipal = 1
FROM Sucursales s
JOIN primeras p ON p.Id = s.Id
WHERE p.rn = 1
  AND NOT EXISTS (SELECT 1 FROM Sucursales x WHERE x.EmpresaId = s.EmpresaId AND x.EsPrincipal = 1);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UX_Sucursales_EmpresaCodigo' AND object_id = OBJECT_ID('Sucursales')
)
BEGIN
    CREATE UNIQUE INDEX UX_Sucursales_EmpresaCodigo ON Sucursales(EmpresaId, Codigo) WHERE Codigo IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM Permisos WHERE Codigo = 'Configuracion.Empresas.Consultar')
    INSERT INTO Permisos (Codigo, Nombre, Descripcion, Estado)
    VALUES ('Configuracion.Empresas.Consultar', 'Consultar empresas y sedes', 'Consultar informacion legal de empresas y sedes', 1);
GO
