/* Amplia novedades de nomina para devengos y deducciones legales. */

IF COL_LENGTH('NominaRegistros', 'FechaFin') IS NULL
    ALTER TABLE NominaRegistros ADD FechaFin DATE NULL;

IF COL_LENGTH('NominaRegistros', 'TipoNovedad') IS NULL
    ALTER TABLE NominaRegistros ADD TipoNovedad NVARCHAR(30) NULL;

IF COL_LENGTH('NominaRegistros', 'CodigoNovedad') IS NULL
    ALTER TABLE NominaRegistros ADD CodigoNovedad NVARCHAR(80) NULL;

IF COL_LENGTH('NominaRegistros', 'Horas') IS NULL
    ALTER TABLE NominaRegistros ADD Horas DECIMAL(18,2) NULL;

IF COL_LENGTH('NominaRegistros', 'Porcentaje') IS NULL
    ALTER TABLE NominaRegistros ADD Porcentaje DECIMAL(18,4) NULL;

IF COL_LENGTH('NominaRegistros', 'BaseCalculo') IS NULL
    ALTER TABLE NominaRegistros ADD BaseCalculo DECIMAL(18,2) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_NominaRegistros_PeriodoEmpleadoFechaConcepto' AND object_id = OBJECT_ID('NominaRegistros'))
    DROP INDEX UX_NominaRegistros_PeriodoEmpleadoFechaConcepto ON NominaRegistros;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_NominaRegistros_PeriodoEmpleadoFechaConceptoCodigo' AND object_id = OBJECT_ID('NominaRegistros'))
    CREATE UNIQUE INDEX UX_NominaRegistros_PeriodoEmpleadoFechaConceptoCodigo
    ON NominaRegistros(PeriodoId, EmpleadoId, Fecha, Concepto, CodigoNovedad);
GO

UPDATE NominaRegistros
SET
    TipoNovedad = CASE
        WHEN Concepto IN ('Descuento', 'Prestamo') THEN 'Deduccion'
        WHEN Concepto <> 'Dia' AND TipoNovedad IS NULL THEN 'Devengo'
        ELSE TipoNovedad
    END,
    CodigoNovedad = CASE
        WHEN Concepto = 'Comision' THEN 'Comision'
        WHEN Concepto = 'Bono' THEN 'Bonificacion'
        WHEN Concepto = 'Transporte' THEN 'AuxilioTransporte'
        WHEN Concepto = 'Alimentacion' THEN 'Bonificacion'
        WHEN Concepto = 'Descuento' THEN 'OtraDeduccion'
        WHEN Concepto = 'Prestamo' THEN 'PrestamoEmpresa'
        WHEN Concepto = 'Ajuste' THEN 'Reintegro'
        ELSE CodigoNovedad
    END
WHERE Concepto <> 'Dia';
GO
