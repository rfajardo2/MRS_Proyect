/* Menu extraido de capturas de OlaClick: Cervezas. */

DECLARE @EmpresaId INT = (SELECT TOP 1 Id FROM Empresas WHERE EsPrincipal = 1 ORDER BY Id);
IF @EmpresaId IS NULL
    SET @EmpresaId = (SELECT TOP 1 Id FROM Empresas ORDER BY Id);

IF @EmpresaId IS NULL
BEGIN
    RAISERROR('No existe empresa para asociar los productos.', 16, 1);
    RETURN;
END

DECLARE @CategoriaId INT = (
    SELECT TOP 1 Id
    FROM ProductoCategorias
    WHERE EmpresaId = @EmpresaId AND Nombre = 'Cervezas'
);

IF @CategoriaId IS NULL
BEGIN
    INSERT INTO ProductoCategorias (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES (@EmpresaId, 'Cervezas', 'Cervezas del menu digital MRS Drunk', 50, 1);
    SET @CategoriaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE ProductoCategorias
    SET Descripcion = 'Cervezas del menu digital MRS Drunk',
        Orden = 50,
        Estado = 1,
        FechaModificacion = SYSUTCDATETIME()
    WHERE Id = @CategoriaId;
END

DECLARE @Productos TABLE (
    Nombre NVARCHAR(160) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    PrecioVenta DECIMAL(18,2) NOT NULL
);

INSERT INTO @Productos (Nombre, Descripcion, PrecioVenta) VALUES
('Aguila Negra', 'Cubetas x 10 und $54.000', 6000),
('Aguila Light', 'Cubetas x 10 und $54.000', 6000),
('Coronita', 'Cubetas x 10 und $54.000', 6000),
('Miller Lite', 'Cubetas x 10 und $54.000', 6000),
('Budweiser', 'Cubetas x 10 und $63.000', 7000),
('Club Colombia', 'Cubetas x 10 und $72.000', 8000),
('Michelob Ultra', 'Cubetas x 10 und $72.000', 8000),
('Corona', 'Cubetas x 10 und $108.000', 12000),
('Stella Artois', 'Cubetas x 10 und $108.000', 12000),
('Smirnoff Ice', 'Cubetas x 10 und $135.000', 15000);

MERGE Productos AS target
USING @Productos AS source
ON target.EmpresaId = @EmpresaId AND target.Nombre = source.Nombre
WHEN MATCHED THEN
    UPDATE SET
        target.CategoriaId = @CategoriaId,
        target.Descripcion = source.Descripcion,
        target.PrecioVenta = source.PrecioVenta,
        target.CostoEstimado = NULL,
        target.ControlaInventario = 1,
        target.Estado = 1,
        target.FechaModificacion = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (EmpresaId, CategoriaId, Nombre, Descripcion, PrecioVenta, CostoEstimado, ControlaInventario, Estado)
    VALUES (@EmpresaId, @CategoriaId, source.Nombre, source.Descripcion, source.PrecioVenta, NULL, 1, 1);
