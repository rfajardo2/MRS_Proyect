/* Menu extraido de capturas de OlaClick: Micheladas MRS Drunk. */

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
    WHERE EmpresaId = @EmpresaId AND Nombre = 'Micheladas MRS Drunk'
);

IF @CategoriaId IS NULL
BEGIN
    INSERT INTO ProductoCategorias (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES (@EmpresaId, 'Micheladas MRS Drunk', 'Micheladas del menu digital MRS Drunk', 30, 1);
    SET @CategoriaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE ProductoCategorias
    SET Descripcion = 'Micheladas del menu digital MRS Drunk',
        Orden = 30,
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
('Red Love', 'Cerveza Aguila Negra, manzana roja, fresa y cereza.', 15000),
('Blue Ocean', 'Cerveza Smirnoff Ice, shot de blue curacao y uvas.', 18000),
('Green Eyes', 'Cerveza Michelob Ultra, manzana verde y cereza verde.', 15000),
('Yellow Sun', 'Cerveza Coronita, mango y maracuya.', 18000);

MERGE Productos AS target
USING @Productos AS source
ON target.EmpresaId = @EmpresaId AND target.Nombre = source.Nombre
WHEN MATCHED THEN
    UPDATE SET
        target.CategoriaId = @CategoriaId,
        target.Descripcion = source.Descripcion,
        target.PrecioVenta = source.PrecioVenta,
        target.CostoEstimado = NULL,
        target.ControlaInventario = 0,
        target.Estado = 1,
        target.FechaModificacion = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (EmpresaId, CategoriaId, Nombre, Descripcion, PrecioVenta, CostoEstimado, ControlaInventario, Estado)
    VALUES (@EmpresaId, @CategoriaId, source.Nombre, source.Descripcion, source.PrecioVenta, NULL, 0, 1);
