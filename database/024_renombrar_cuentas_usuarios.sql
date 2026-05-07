SET NOCOUNT ON;

UPDATE dbo.Ventanas
SET Nombre = 'Cuentas de usuarios'
WHERE Ruta = '/admin-cuentas'
  AND Nombre <> 'Cuentas de usuarios';
