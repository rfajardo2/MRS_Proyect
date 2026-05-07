# MRS Drunk v2 - Diseno de modulos operativos

## Objetivo
Reemplazar el control manual del archivo `04 CONTROL CUENTAS DRUNK ABRIL 2026.....xlsx` por modulos integrados a MRS Drunk. La v2 debe mantener el modelo actual de usuarios, roles, permisos, ventanas, empresa, sucursal, sesiones y nomina.

## Lectura del Excel actual
El archivo actual tiene 3 hojas principales:

- `INGRESO-EGRESOS`: control diario de venta total, datafono, Nequi/transferencia, efectivo, nomina, compras/gastos, retenciones, comisiones, total de gastos, saldo a favor, saldo en contra y calculos de porcentaje/reparto.
- `NOMINA`: control diario de pagos por persona, turnos, descansos, comisiones y total mensual de nomina.
- `GASTO LOCAL+NOMINA`: gastos fijos y operativos como canon, IVA, administracion, luz, agua, bodega, internet, otros y nomina total.

Reglas detectadas:

- Venta total = datafono + transferencia/Nequi + efectivo.
- Algunos cargos se calculan por medio de pago: 4x1000, retefuente, comision Bold, ret ICA y comision fija.
- Gastos totales del dia combinan nomina, compras/gastos y cargos financieros.
- El balance mensual cruza ingresos, gastos locales, nomina y saldo en efectivo.
- La nomina se maneja por dia y por trabajador, no solo como periodo mensual.
- Hay gastos fijos mensuales y gastos eventuales.

## Principios de integracion
- Todo modulo nuevo debe crear registros en `Modulos`, `Ventanas`, `Permisos` y `RolPermisos`.
- Toda API protegida debe validar permisos en backend.
- Toda pantalla debe respetar permisos en frontend.
- Las configuraciones globales deben vivir en el modulo `Configuracion`.
- Las operaciones de mesero deben quedar separadas de las operaciones de administrador.
- Los balances se deben calcular desde movimientos reales, no desde campos manuales aislados.

## Modulos propuestos

### 1. Configuracion
Ventanas:

- `Configuracion de ventas`
- `Configuracion de caja`
- `Configuracion de inventario`
- `Configuracion de gastos`

Configuraciones iniciales:

- Requiere aprobacion de administrador para cerrar cuenta.
- Permite dividir cuenta.
- Permite eliminar items de pedido.
- Requiere motivo al eliminar item.
- Requiere motivo al anular cuenta.
- Porcentaje base de reparto o utilidad.
- Tarifas de medios de pago: 4x1000, retefuente, comision datafono, ret ICA, comision fija.
- Hora de inicio del dia operativo.
- Hora de cierre del dia operativo.

### 2. Productos
Ventanas:

- `Categorias`
- `Productos`
- `Precios`
- `Recetas`

Necesidad:

- Crear carta de venta.
- Activar/inactivar productos.
- Definir precio de venta.
- Relacionar productos con insumos cuando aplique.
- Preparar descuento automatico de inventario.

### 3. Operacion
Ventanas:

- `Cuentas`
- `Mis cuentas`
- `Nueva cuenta`
- `Balance del dia`

Usuario objetivo:

- Mesero.
- Cajero si aplica.

Acciones:

- Crear cuenta.
- Agregar producto o servicio al pedido.
- Eliminar item si tiene permiso.
- Dividir cuenta.
- Solicitar cierre.
- Cerrar cuenta si la configuracion y el permiso lo permiten.
- Ver balance propio del dia operativo abierto.

Estados de cuenta:

- `Abierta`
- `PendienteAprobacion`
- `Cerrada`
- `Rechazada`
- `Anulada`

### 4. Administracion de cuentas
Ventanas:

- `Cuentas de meseros`
- `Aprobaciones de cierre`
- `Balance por usuario`
- `Cierre operativo`

Usuario objetivo:

- Administrador.
- Supervisor.

Acciones:

- Ver cuentas de todos los meseros.
- Aprobar cierre.
- Rechazar cierre.
- Reabrir cuenta.
- Anular cuenta.
- Revisar balance por mesero.
- Revisar balance general del dia.
- Cerrar dia operativo.

### 5. Caja y medios de pago
Ventanas:

- `Apertura de caja`
- `Movimientos de caja`
- `Cierre de caja`
- `Metodos de pago`
- `Arqueo`

Necesidad:

- Registrar efectivo inicial.
- Registrar ingresos y egresos.
- Separar venta por efectivo, datafono, Nequi, transferencia u otros.
- Calcular comisiones y retenciones desde configuracion.
- Comparar saldo esperado contra saldo real.

### 6. Inventario e insumos
Ventanas:

- `Insumos`
- `Movimientos de inventario`
- `Compras`
- `Devoluciones`
- `Perdidas y danos`
- `Vencimientos`
- `Ajustes`
- `Proveedores`

Tipos de movimiento:

- Compra.
- Venta.
- Devolucion a proveedor.
- Devolucion de cliente.
- Vencimiento.
- Dano.
- Producto roto.
- Consumo interno.
- Ajuste manual.
- Merma.

### 7. Gastos y servicios
Ventanas:

- `Gastos fijos`
- `Gastos eventuales`
- `Servicios y mantenimientos`
- `Pagos`

Tipos:

- Canon/arriendo.
- Administracion.
- IVA asociado.
- Luz.
- Agua.
- Internet.
- Bodega.
- Mantenimientos.
- Reparaciones.
- Servicios adicionales por eventualidades.
- Otros.

### 8. Reportes
Ventanas:

- `Resumen diario`
- `Resumen mensual`
- `Ventas por metodo de pago`
- `Gastos`
- `Utilidad`
- `Nomina vs ventas`
- `Balance de meseros`

Debe reemplazar las sumas finales del Excel:

- Venta total.
- Venta por medio de pago.
- Gastos por tipo.
- Nomina total.
- Comisiones y retenciones.
- Saldo a favor.
- Saldo en contra.
- Saldo efectivo.
- Resultado mensual.

## Permisos base

Formato sugerido: `Modulo.Ventana.Accion`.

Operacion:

- `Operacion.Cuentas.VerPropias`
- `Operacion.Cuentas.Crear`
- `Operacion.Cuentas.AgregarItem`
- `Operacion.Cuentas.EliminarItem`
- `Operacion.Cuentas.Dividir`
- `Operacion.Cuentas.SolicitarCierre`
- `Operacion.Cuentas.CerrarSinAprobacion`
- `Operacion.Balance.VerPropio`

Administracion:

- `AdministracionCuentas.Cuentas.VerTodas`
- `AdministracionCuentas.Cuentas.AprobarCierre`
- `AdministracionCuentas.Cuentas.RechazarCierre`
- `AdministracionCuentas.Cuentas.Reabrir`
- `AdministracionCuentas.Cuentas.Anular`
- `AdministracionCuentas.Balance.VerTodos`
- `AdministracionCuentas.CierreOperativo.CerrarDia`

Configuracion:

- `Configuracion.Ventas.Ver`
- `Configuracion.Ventas.Editar`
- `Configuracion.Caja.Ver`
- `Configuracion.Caja.Editar`
- `Configuracion.Inventario.Ver`
- `Configuracion.Inventario.Editar`

Caja:

- `Caja.Apertura.Crear`
- `Caja.Movimientos.Ver`
- `Caja.Movimientos.Crear`
- `Caja.Cierre.Crear`
- `Caja.Arqueo.Ver`
- `Caja.Arqueo.AprobarDiferencia`

Inventario:

- `Inventario.Insumos.Ver`
- `Inventario.Insumos.Crear`
- `Inventario.Insumos.Editar`
- `Inventario.Movimientos.Ver`
- `Inventario.Movimientos.Crear`
- `Inventario.Ajustes.Crear`
- `Inventario.Perdidas.Crear`
- `Inventario.Vencimientos.Crear`

Gastos:

- `Gastos.Fijos.Ver`
- `Gastos.Fijos.Crear`
- `Gastos.Fijos.Editar`
- `Gastos.Eventuales.Ver`
- `Gastos.Eventuales.Crear`
- `Gastos.Mantenimientos.Crear`

## Modelo de datos inicial sugerido

Configuracion:

- `ConfiguracionesGlobales`
- `ConfiguracionVentas`
- `ConfiguracionMediosPago`

Productos:

- `ProductoCategorias`
- `Productos`
- `ProductoPrecios`
- `ProductoRecetas`
- `ProductoRecetaInsumos`

Operacion:

- `DiasOperativos`
- `Cuentas`
- `CuentaItems`
- `CuentaPagos`
- `CuentaDivisiones`
- `CuentaAprobaciones`
- `CuentaEventos`

Caja:

- `Cajas`
- `CajaAperturas`
- `CajaMovimientos`
- `CajaCierres`
- `MetodosPago`

Inventario:

- `Insumos`
- `Proveedores`
- `InventarioMovimientos`
- `InventarioLotes`
- `Compras`
- `CompraItems`

Gastos:

- `GastoCategorias`
- `Gastos`
- `GastoPagos`

Reportes:

- Inicialmente no requiere tablas fisicas.
- Se pueden calcular desde vistas SQL o endpoints de resumen.

## Flujo de cuenta de mesero
1. Mesero abre `Nueva cuenta`.
2. Selecciona mesa, cliente opcional o descripcion.
3. Agrega productos/servicios.
4. Puede eliminar items solo si tiene permiso y registra motivo si la configuracion lo exige.
5. Puede dividir cuenta por items, monto o porcentaje.
6. Solicita cierre.
7. Si `Requiere aprobacion` esta activo, la cuenta pasa a `PendienteAprobacion`.
8. Administrador aprueba o rechaza.
9. Al cerrar, se registran pagos por metodo y se actualiza balance del dia.
10. Si hay recetas configuradas, se descuentan insumos.

## Flujo de balance del dia
1. Se abre un `DiaOperativo`.
2. Meseros crean cuentas.
3. Cada pago cerrado alimenta venta por metodo de pago.
4. Gastos y movimientos de caja se descuentan.
5. Nomina diaria puede cruzarse desde el modulo existente.
6. El administrador revisa balance general.
7. El administrador cierra el dia operativo.

## Fases de implementacion

### Fase 1 - Base operativa
- Configuracion de ventas.
- Productos y categorias.
- Cuentas de meseros.
- Items de cuenta.
- Solicitud/cierre con aprobacion.
- Balance propio del mesero.
- Administracion de cuentas.

### Fase 2 - Caja
- Metodos de pago.
- Pagos por cuenta.
- Apertura y cierre de caja.
- Arqueo.
- Comisiones y retenciones automaticas.

### Fase 3 - Inventario
- Insumos.
- Compras.
- Movimientos.
- Devoluciones.
- Vencimientos.
- Danos y mermas.

### Fase 4 - Gastos y reportes
- Gastos fijos.
- Gastos eventuales.
- Servicios/mantenimientos.
- Reporte diario.
- Reporte mensual equivalente al Excel.

## Primer entregable recomendado
Implementar Fase 1 completa, porque permite operar cuentas reales de meseros y reemplaza la parte mas urgente del control manual sin romper nomina ni permisos existentes.
