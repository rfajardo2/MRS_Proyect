SET NOCOUNT ON;

IF COL_LENGTH('dbo.CuentaPagos', 'IncluyePropina') IS NULL
BEGIN
    ALTER TABLE dbo.CuentaPagos
        ADD IncluyePropina BIT NOT NULL CONSTRAINT DF_CuentaPagos_IncluyePropina DEFAULT (0);
END;

IF COL_LENGTH('dbo.CuentaPagos', 'ValorPropina') IS NULL
BEGIN
    ALTER TABLE dbo.CuentaPagos
        ADD ValorPropina DECIMAL(18,2) NOT NULL CONSTRAINT DF_CuentaPagos_ValorPropina DEFAULT (0);
END;

IF COL_LENGTH('dbo.ConfiguracionesVenta', 'PorcentajePropinaDefecto') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionesVenta
        ADD PorcentajePropinaDefecto DECIMAL(18,4) NOT NULL CONSTRAINT DF_ConfiguracionesVenta_PorcentajePropinaDefecto DEFAULT (10);
END;

EXEC(N'
UPDATE dbo.CuentaPagos
SET ValorPropina = 0,
    IncluyePropina = 0
WHERE ValorPropina IS NULL;
');

EXEC(N'
UPDATE dbo.ConfiguracionesVenta
SET PorcentajePropinaDefecto = 10
WHERE PorcentajePropinaDefecto IS NULL;
');
