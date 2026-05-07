/* Menu extraido de capturas de OlaClick: categoria Cocteles. */

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
    WHERE EmpresaId = @EmpresaId AND Nombre = 'Cocteles'
);

IF @CategoriaId IS NULL
BEGIN
    INSERT INTO ProductoCategorias (EmpresaId, Nombre, Descripcion, Orden, Estado)
    VALUES (@EmpresaId, 'Cocteles', 'Cocteles del menu digital MRS Drunk', 10, 1);
    SET @CategoriaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE ProductoCategorias
    SET Descripcion = 'Cocteles del menu digital MRS Drunk',
        Orden = 10,
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
('Margarita Clasica', 'La margarita es uno de los cocteles mas populares del mundo; es facil ver por que. La combinacion de la clasica margarita combina tequila, limon y licor de naranja.', 18000),
('Margarita Blue', 'Coctel con tequila blanco, jugo de limon, shot de blue curacao y licor de naranja.', 20000),
('Margarita Fresa', 'Coctel con tequila, zumo de limon, fresas maceradas y licor de naranja.', 20000),
('Margarita Maracuya', 'Coctel con tequila blanco, jugo de limon, sirope de maracuya y licor de naranja.', 20000),
('Mojito', 'Ron, lima, hierbabuena, almibar simple y hielo picado.', 18000),
('Mojito de Coco', 'Ron, lima, hierbabuena, almibar simple, crema de coco y hielo picado.', 20000),
('Mojito de Fresa', 'Ron, lima, hierbabuena, almibar simple, fresas y hielo picado.', 20000),
('Mojito Blue Curacao', 'Ron, lima, hierbabuena, almibar simple, shot de blue curacao y hielo picado.', 20000),
('Pi' + NCHAR(241) + 'a Colada', 'Mezcla de crema de coco, jugo de pi' + NCHAR(241) + 'a, ron blanco y hielo.', 23000),
('Blue Hawaiian', 'Ron blanco, blue curacao, zumo de pi' + NCHAR(241) + 'a y crema de coco.', 25000),
('Tequila Sunrise', 'Tequila, zumo de naranja, granadina, rodaja de limon y cereza.', 20000),
('Vodka Sunrise', 'Vodka, zumo de naranja, granadina, rodaja de limon y cereza.', 20000),
('Cuba Libre', 'Ron a' + NCHAR(241) + 'ejo, refresco de cola y zumo de limon.', 18000),
('Blue Lagoon', 'Vodka, blue curacao, zumo de limon y rodaja de limon.', 23000),
('Caipiroska', 'Vodka, almibar simple y zumo de limon.', 18000),
('Long Island Ice Tea', 'Coctel de alta graduacion con vodka, tequila, ron blanco, ginebra, triple sec y zumo de limon.', 28000);

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
