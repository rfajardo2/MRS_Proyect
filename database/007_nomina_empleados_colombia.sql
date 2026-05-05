/* Amplia el maestro de empleados para datos laborales usados en Colombia. */

IF COL_LENGTH('NominaEmpleados', 'TipoDocumento') IS NULL
    ALTER TABLE NominaEmpleados ADD TipoDocumento NVARCHAR(10) NOT NULL CONSTRAINT DF_NominaEmpleados_TipoDocumento DEFAULT 'CC';

IF COL_LENGTH('NominaEmpleados', 'NumeroDocumento') IS NULL
    ALTER TABLE NominaEmpleados ADD NumeroDocumento NVARCHAR(50) NOT NULL CONSTRAINT DF_NominaEmpleados_NumeroDocumento DEFAULT '';

IF COL_LENGTH('NominaEmpleados', 'FechaExpedicionDocumento') IS NULL
    ALTER TABLE NominaEmpleados ADD FechaExpedicionDocumento DATE NULL;

IF COL_LENGTH('NominaEmpleados', 'PrimerNombre') IS NULL
    ALTER TABLE NominaEmpleados ADD PrimerNombre NVARCHAR(60) NOT NULL CONSTRAINT DF_NominaEmpleados_PrimerNombre DEFAULT '';

IF COL_LENGTH('NominaEmpleados', 'SegundoNombre') IS NULL
    ALTER TABLE NominaEmpleados ADD SegundoNombre NVARCHAR(60) NULL;

IF COL_LENGTH('NominaEmpleados', 'PrimerApellido') IS NULL
    ALTER TABLE NominaEmpleados ADD PrimerApellido NVARCHAR(60) NOT NULL CONSTRAINT DF_NominaEmpleados_PrimerApellido DEFAULT '';

IF COL_LENGTH('NominaEmpleados', 'SegundoApellido') IS NULL
    ALTER TABLE NominaEmpleados ADD SegundoApellido NVARCHAR(60) NULL;

IF COL_LENGTH('NominaEmpleados', 'FechaNacimiento') IS NULL
    ALTER TABLE NominaEmpleados ADD FechaNacimiento DATE NULL;

IF COL_LENGTH('NominaEmpleados', 'Genero') IS NULL
    ALTER TABLE NominaEmpleados ADD Genero NVARCHAR(30) NULL;

IF COL_LENGTH('NominaEmpleados', 'EstadoCivil') IS NULL
    ALTER TABLE NominaEmpleados ADD EstadoCivil NVARCHAR(30) NULL;

IF COL_LENGTH('NominaEmpleados', 'Telefono') IS NULL
    ALTER TABLE NominaEmpleados ADD Telefono NVARCHAR(40) NULL;

IF COL_LENGTH('NominaEmpleados', 'Correo') IS NULL
    ALTER TABLE NominaEmpleados ADD Correo NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'Direccion') IS NULL
    ALTER TABLE NominaEmpleados ADD Direccion NVARCHAR(180) NULL;

IF COL_LENGTH('NominaEmpleados', 'Departamento') IS NULL
    ALTER TABLE NominaEmpleados ADD Departamento NVARCHAR(80) NULL;

IF COL_LENGTH('NominaEmpleados', 'Municipio') IS NULL
    ALTER TABLE NominaEmpleados ADD Municipio NVARCHAR(80) NULL;

IF COL_LENGTH('NominaEmpleados', 'Pais') IS NULL
    ALTER TABLE NominaEmpleados ADD Pais NVARCHAR(5) NOT NULL CONSTRAINT DF_NominaEmpleados_Pais DEFAULT 'CO';

IF COL_LENGTH('NominaEmpleados', 'FechaIngreso') IS NULL
    ALTER TABLE NominaEmpleados ADD FechaIngreso DATE NOT NULL CONSTRAINT DF_NominaEmpleados_FechaIngreso DEFAULT CONVERT(date, SYSUTCDATETIME());

IF COL_LENGTH('NominaEmpleados', 'FechaRetiro') IS NULL
    ALTER TABLE NominaEmpleados ADD FechaRetiro DATE NULL;

IF COL_LENGTH('NominaEmpleados', 'TipoContrato') IS NULL
    ALTER TABLE NominaEmpleados ADD TipoContrato NVARCHAR(60) NOT NULL CONSTRAINT DF_NominaEmpleados_TipoContrato DEFAULT 'Termino indefinido';

IF COL_LENGTH('NominaEmpleados', 'TipoTrabajador') IS NULL
    ALTER TABLE NominaEmpleados ADD TipoTrabajador NVARCHAR(60) NOT NULL CONSTRAINT DF_NominaEmpleados_TipoTrabajador DEFAULT 'Dependiente';

IF COL_LENGTH('NominaEmpleados', 'SubtipoCotizante') IS NULL
    ALTER TABLE NominaEmpleados ADD SubtipoCotizante NVARCHAR(80) NOT NULL CONSTRAINT DF_NominaEmpleados_SubtipoCotizante DEFAULT 'No aplica';

IF COL_LENGTH('NominaEmpleados', 'TipoSalario') IS NULL
    ALTER TABLE NominaEmpleados ADD TipoSalario NVARCHAR(40) NOT NULL CONSTRAINT DF_NominaEmpleados_TipoSalario DEFAULT 'Ordinario';

IF COL_LENGTH('NominaEmpleados', 'SalarioIntegral') IS NULL
    ALTER TABLE NominaEmpleados ADD SalarioIntegral BIT NOT NULL CONSTRAINT DF_NominaEmpleados_SalarioIntegral DEFAULT 0;

IF COL_LENGTH('NominaEmpleados', 'SalarioBase') IS NULL
    ALTER TABLE NominaEmpleados ADD SalarioBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_NominaEmpleados_SalarioBase DEFAULT 0;

IF COL_LENGTH('NominaEmpleados', 'PeriodicidadPago') IS NULL
    ALTER TABLE NominaEmpleados ADD PeriodicidadPago NVARCHAR(40) NOT NULL CONSTRAINT DF_NominaEmpleados_PeriodicidadPago DEFAULT 'Mensual';

IF COL_LENGTH('NominaEmpleados', 'JornadaLaboral') IS NULL
    ALTER TABLE NominaEmpleados ADD JornadaLaboral NVARCHAR(60) NOT NULL CONSTRAINT DF_NominaEmpleados_JornadaLaboral DEFAULT 'Tiempo completo';

IF COL_LENGTH('NominaEmpleados', 'Eps') IS NULL
    ALTER TABLE NominaEmpleados ADD Eps NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'FondoPension') IS NULL
    ALTER TABLE NominaEmpleados ADD FondoPension NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'FondoCesantias') IS NULL
    ALTER TABLE NominaEmpleados ADD FondoCesantias NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'Arl') IS NULL
    ALTER TABLE NominaEmpleados ADD Arl NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'NivelRiesgoArl') IS NULL
    ALTER TABLE NominaEmpleados ADD NivelRiesgoArl NVARCHAR(5) NOT NULL CONSTRAINT DF_NominaEmpleados_NivelRiesgoArl DEFAULT 'I';

IF COL_LENGTH('NominaEmpleados', 'CajaCompensacion') IS NULL
    ALTER TABLE NominaEmpleados ADD CajaCompensacion NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'Banco') IS NULL
    ALTER TABLE NominaEmpleados ADD Banco NVARCHAR(120) NULL;

IF COL_LENGTH('NominaEmpleados', 'TipoCuenta') IS NULL
    ALTER TABLE NominaEmpleados ADD TipoCuenta NVARCHAR(40) NULL;

IF COL_LENGTH('NominaEmpleados', 'NumeroCuenta') IS NULL
    ALTER TABLE NominaEmpleados ADD NumeroCuenta NVARCHAR(80) NULL;

IF COL_LENGTH('NominaEmpleados', 'ContactoEmergenciaNombre') IS NULL
    ALTER TABLE NominaEmpleados ADD ContactoEmergenciaNombre NVARCHAR(160) NULL;

IF COL_LENGTH('NominaEmpleados', 'ContactoEmergenciaTelefono') IS NULL
    ALTER TABLE NominaEmpleados ADD ContactoEmergenciaTelefono NVARCHAR(40) NULL;

IF COL_LENGTH('NominaEmpleados', 'Observaciones') IS NULL
    ALTER TABLE NominaEmpleados ADD Observaciones NVARCHAR(500) NULL;
GO

UPDATE NominaEmpleados
SET
    NumeroDocumento = CASE WHEN ISNULL(NumeroDocumento, '') = '' THEN ISNULL(Documento, '') ELSE NumeroDocumento END,
    PrimerNombre = CASE WHEN ISNULL(PrimerNombre, '') = '' THEN LEFT(NombreCompleto, CHARINDEX(' ', NombreCompleto + ' ') - 1) ELSE PrimerNombre END,
    PrimerApellido = CASE WHEN ISNULL(PrimerApellido, '') = '' THEN RIGHT(NombreCompleto, CHARINDEX(' ', REVERSE(NombreCompleto) + ' ') - 1) ELSE PrimerApellido END;
GO
