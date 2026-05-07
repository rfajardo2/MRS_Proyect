SET NOCOUNT ON;

UPDATE dbo.Ventanas
SET Nombre = 'Aprobacion de cuentas'
WHERE Ruta = '/admin-cuentas'
  AND Nombre <> 'Aprobacion de cuentas';
