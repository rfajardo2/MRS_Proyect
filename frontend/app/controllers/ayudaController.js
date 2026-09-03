(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AyudaController', function () {
    var vm = this;

    vm.secciones = [
      {
        icono: 'fa-users',
        titulo: 'Usuarios, roles y permisos',
        tips: [
          'Cada usuario tiene un rol, y cada rol tiene permisos por ventana (ver, crear, consultar, editar, eliminar). Si alguien no ve un modulo, revisa su rol en "Roles" antes que nada.',
          'Crea el rol primero, asignale los permisos que necesita en "Permisos", y despues crea o edita el usuario con ese rol.',
          'Un usuario inactivo no puede iniciar sesion aunque su contrasena sea correcta. Actívalo desde "Usuarios" si necesita volver a entrar.'
        ]
      },
      {
        icono: 'fa-building',
        titulo: 'Empresas y sedes',
        tips: [
          'Una empresa puede tener varias sedes (sucursales). Cada usuario queda asociado a una sede al crearse.',
          'Marca una sola sede como principal por empresa; es la que se usa como referencia en reportes generales.'
        ]
      },
      {
        icono: 'fa-cash-register',
        titulo: 'Caja',
        tips: [
          'Abre el turno de caja al iniciar el dia operativo antes de registrar ventas — sin turno abierto no hay como cuadrar el cierre.',
          'Al cerrar el turno, compara el efectivo esperado contra el contado y registra la diferencia si la hay. No cierres el turno sin ese conteo.'
        ]
      },
      {
        icono: 'fa-receipt',
        titulo: 'Operacion de cuentas',
        tips: [
          'Cada mesa abre una cuenta. Agrega los productos a medida que el cliente pide — el total se recalcula solo.',
          'Al cerrar una cuenta con pago, elige el metodo de pago correcto: eso es lo que despues cuadra la caja y los reportes.',
          'Una cuenta se puede dividir entre varios pagos si el grupo paga por separado.'
        ]
      },
      {
        icono: 'fa-boxes-stacked',
        titulo: 'Inventario',
        tips: [
          'Los productos con receta descuentan insumos del inventario automaticamente al venderse — configura la receta antes de vender el producto.',
          'Registra las compras de insumos apenas lleguen, para que el stock disponible sea real.'
        ]
      },
      {
        icono: 'fa-money-check-dollar',
        titulo: 'Nomina',
        tips: [
          'Define primero el periodo de nomina en "Periodos" antes de registrar novedades o control diario.',
          'Las novedades legales (horas extra, recargos, incapacidades) se cargan por empleado y se reflejan en el resumen del periodo.'
        ]
      },
      {
        icono: 'fa-credit-card',
        titulo: 'Pagos',
        tips: [
          'Hoy los pagos con pasarela (PayU) estan en preparacion — la estructura ya soporta conectar cualquier pasarela a futuro sin rehacer el modulo.',
          'Mientras tanto, registra los pagos de las cuentas de forma manual desde Operacion de cuentas.'
        ]
      }
    ];
  });
})();
