USE MRSDrunkDb;
GO

IF OBJECT_ID('NominaPeriodoDiasNoLaborados', 'U') IS NULL
BEGIN
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
