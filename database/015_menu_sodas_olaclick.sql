/* Menu extraido de capturas de OlaClick: Sodas MRS Drunk (Sin alcohol). */

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
    WHERE EmpresaId = @EmpresaId AND Nombre = 'Sodas MRS Drunk (Sin alcohol)'
);

IF @CategoriaId IS NULL
BEGIN
    INSERT INTO ProductoCategorias (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES (@EmpresaId, 'Sodas MRS Drunk (Sin alcohol)', 'Sodas sin alcohol del menu digital MRS Drunk', 20, 1);
    SET @CategoriaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE ProductoCategorias
    SET Descripcion = 'Sodas sin alcohol del menu digital MRS Drunk',
        Orden = 20,
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
('Fresa y Hierbabuena', 'Siropes obtenidos mediante infusion de fresas y hierbabuena, acompa' + NCHAR(241) + 'ado de una refrescante soda Breta' + NCHAR(241) + 'a.', 12000),
('Jamaica y Lavanda', 'Siropes obtenidos mediante infusion de la flor de Jamaica y ramitas de lavanda, acompa' + NCHAR(241) + 'ado de una refrescante soda Breta' + NCHAR(241) + 'a.', 12000),
('Pi' + NCHAR(241) + 'a Grill y Jengibre', 'Siropes obtenidos mediante infusion de pi' + NCHAR(241) + 'as asadas y rayadura de jengibre, acompa' + NCHAR(241) + 'ado de una refrescante soda Breta' + NCHAR(241) + 'a.', 12000);

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
