SET NOCOUNT ON;

IF COL_LENGTH('dbo.CuentaPagos', 'Origen') IS NULL
BEGIN
    ALTER TABLE dbo.CuentaPagos
        ADD Origen NVARCHAR(30) NOT NULL CONSTRAINT DF_CuentaPagos_Origen DEFAULT ('MANUAL');
END;

IF COL_LENGTH('dbo.CuentaPagos', 'Estado') IS NULL
BEGIN
    ALTER TABLE dbo.CuentaPagos
        ADD Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_CuentaPagos_Estado DEFAULT ('APLICADO');
END;

IF COL_LENGTH('dbo.CuentaPagos', 'PagoPasarelaId') IS NULL
BEGIN
    ALTER TABLE dbo.CuentaPagos
        ADD PagoPasarelaId INT NULL;
END;

IF OBJECT_ID('dbo.PagoPasarelas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoPasarelas (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PagoPasarelas PRIMARY KEY,
        EmpresaId INT NOT NULL,
        SucursalId INT NULL,
        CuentaId INT NOT NULL,
        MesaReferencia NVARCHAR(80) NULL,
        Proveedor NVARCHAR(30) NOT NULL CONSTRAINT DF_PagoPasarelas_Proveedor DEFAULT ('PAYU'),
        MetodoPago NVARCHAR(30) NOT NULL,
        ReferenciaUnica NVARCHAR(140) NOT NULL,
        ValorEsperado DECIMAL(18,2) NOT NULL,
        ValorPagado DECIMAL(18,2) NOT NULL CONSTRAINT DF_PagoPasarelas_ValorPagado DEFAULT (0),
        Moneda NVARCHAR(10) NOT NULL CONSTRAINT DF_PagoPasarelas_Moneda DEFAULT ('COP'),
        Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_PagoPasarelas_Estado DEFAULT ('PENDING'),
        TransaccionPayU NVARCHAR(80) NULL,
        OrderIdPayU NVARCHAR(80) NULL,
        CheckoutUrl NVARCHAR(400) NULL,
        RequestPayload NVARCHAR(MAX) NULL,
        ResponsePayload NVARCHAR(MAX) NULL,
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_PagoPasarelas_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaExpiracion DATETIME2 NULL,
        FechaConfirmacion DATETIME2 NULL,
        FechaUltimaConsulta DATETIME2 NULL,
        UsuarioCreacionId INT NOT NULL,
        CuentaPagoId INT NULL,
        Observacion NVARCHAR(500) NULL,
        MensajeError NVARCHAR(1000) NULL,
        CONSTRAINT FK_PagoPasarelas_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(Id),
        CONSTRAINT FK_PagoPasarelas_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(Id),
        CONSTRAINT FK_PagoPasarelas_Cuentas FOREIGN KEY (CuentaId) REFERENCES dbo.Cuentas(Id),
        CONSTRAINT FK_PagoPasarelas_UsuarioCreacion FOREIGN KEY (UsuarioCreacionId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT FK_PagoPasarelas_CuentaPago FOREIGN KEY (CuentaPagoId) REFERENCES dbo.CuentaPagos(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PagoPasarelas_ReferenciaUnica' AND object_id = OBJECT_ID('dbo.PagoPasarelas'))
BEGIN
    CREATE UNIQUE INDEX UX_PagoPasarelas_ReferenciaUnica ON dbo.PagoPasarelas(ReferenciaUnica);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PagoPasarelas_CuentaEstado' AND object_id = OBJECT_ID('dbo.PagoPasarelas'))
BEGIN
    CREATE INDEX IX_PagoPasarelas_CuentaEstado ON dbo.PagoPasarelas(CuentaId, Estado);
END;

IF OBJECT_ID('dbo.PagoConfirmacionesPayU', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoConfirmacionesPayU (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PagoConfirmacionesPayU PRIMARY KEY,
        PagoPasarelaId INT NULL,
        Referencia NVARCHAR(140) NOT NULL,
        TransaccionPayU NVARCHAR(80) NULL,
        EstadoRecibido NVARCHAR(40) NULL,
        ValorRecibido DECIMAL(18,2) NULL,
        Moneda NVARCHAR(10) NULL,
        PayloadCompleto NVARCHAR(MAX) NOT NULL,
        FirmaRecibida NVARCHAR(255) NULL,
        FirmaValida BIT NOT NULL CONSTRAINT DF_PagoConfirmacionesPayU_FirmaValida DEFAULT (0),
        Procesado BIT NOT NULL CONSTRAINT DF_PagoConfirmacionesPayU_Procesado DEFAULT (0),
        Observacion NVARCHAR(1000) NULL,
        FechaRecepcion DATETIME2 NOT NULL CONSTRAINT DF_PagoConfirmacionesPayU_FechaRecepcion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PagoConfirmacionesPayU_PagoPasarela FOREIGN KEY (PagoPasarelaId) REFERENCES dbo.PagoPasarelas(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PagoConfirmacionesPayU_ReferenciaTransaccionFecha' AND object_id = OBJECT_ID('dbo.PagoConfirmacionesPayU'))
BEGIN
    CREATE INDEX IX_PagoConfirmacionesPayU_ReferenciaTransaccionFecha
        ON dbo.PagoConfirmacionesPayU(Referencia, TransaccionPayU, FechaRecepcion);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_CuentaPagos_PagoPasarela'
)
BEGIN
    ALTER TABLE dbo.CuentaPagos
        ADD CONSTRAINT FK_CuentaPagos_PagoPasarela
            FOREIGN KEY (PagoPasarelaId) REFERENCES dbo.PagoPasarelas(Id);
END;

EXEC(N'
UPDATE dbo.CuentaPagos
SET Origen = ISNULL(NULLIF(Origen, ''''), ''MANUAL''),
    Estado = ISNULL(NULLIF(Estado, ''''), ''APLICADO'')
WHERE Origen IS NULL OR Estado IS NULL;
');
