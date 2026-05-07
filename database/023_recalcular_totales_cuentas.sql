SET NOCOUNT ON;

;WITH Totales AS (
    SELECT
        c.Id,
        COALESCE(SUM(CASE WHEN i.Eliminado = 0 THEN i.Cantidad * i.PrecioUnitario ELSE 0 END), 0) AS Subtotal,
        COALESCE(SUM(CASE WHEN i.Eliminado = 0 THEN i.Descuento ELSE 0 END), 0) AS Descuento,
        COALESCE(SUM(CASE WHEN i.Eliminado = 0 THEN i.Total ELSE 0 END), 0) AS Total
    FROM dbo.Cuentas c
    LEFT JOIN dbo.CuentaItems i ON i.CuentaId = c.Id
    GROUP BY c.Id
)
UPDATE c
SET c.Subtotal = t.Subtotal,
    c.Descuento = t.Descuento,
    c.Total = t.Total,
    c.FechaModificacion = SYSUTCDATETIME()
FROM dbo.Cuentas c
JOIN Totales t ON t.Id = c.Id
WHERE c.Subtotal <> t.Subtotal
   OR c.Descuento <> t.Descuento
   OR c.Total <> t.Total;
