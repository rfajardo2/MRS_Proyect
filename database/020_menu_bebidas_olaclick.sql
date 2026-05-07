/* Menu extraido de capturas de OlaClick: Bebidas. */

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
    WHERE EmpresaId = @EmpresaId AND Nombre = 'Bebidas'
);

IF @CategoriaId IS NULL
BEGIN
    INSERT INTO ProductoCategorias (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES (@EmpresaId, 'Bebidas', 'Bebidas del menu digital MRS Drunk', 70, 1);
    SET @CategoriaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE ProductoCategorias
    SET Descripcion = 'Bebidas del menu digital MRS Drunk',
        Orden = 70,
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
('Gatorade', NULL, 8000),
('Electrolit', NULL, 15000),
('Schweppes Ginger', NULL, 6000),
('Soda Breta' + NCHAR(241) + 'a', NULL, 6000),
('Agua 250ml', NULL, 2000);

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
