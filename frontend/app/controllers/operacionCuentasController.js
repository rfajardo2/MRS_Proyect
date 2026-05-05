(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('OperacionCuentasController', function (operacionService, productosService, authService) {
    var vm = this;
    vm.cuentas = [];
    vm.productos = [];
    vm.selected = null;
    vm.nueva = {};
    vm.item = {};
    vm.pago = { metodoPago: 'Efectivo' };
    vm.error = null;
    vm.canCreate = authService.hasPermission('Operacion.Cuentas.Crear');
    vm.canEdit = authService.hasPermission('Operacion.Cuentas.Editar');
    vm.canDelete = authService.hasPermission('Operacion.Cuentas.Eliminar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canCreate = authService.hasPermission('Operacion.Cuentas.Crear');
        vm.canEdit = authService.hasPermission('Operacion.Cuentas.Editar');
        vm.canDelete = authService.hasPermission('Operacion.Cuentas.Eliminar');
      });
      operacionService.misCuentas().then(function (data) {
        vm.cuentas = data;
        if (vm.selected) {
          vm.selected = vm.cuentas.find(function (x) { return x.id === vm.selected.id; }) || null;
        }
      });
      productosService.productos().then(function (data) { vm.productos = data.filter(function (x) { return x.estado; }); });
    };

    vm.crearCuenta = function () {
      if (!vm.canCreate) { return; }
      operacionService.crearCuenta(vm.nueva).then(function () {
        vm.nueva = {};
        vm.load();
      });
    };

    vm.select = function (cuenta) { vm.selected = cuenta; };
    vm.isEditable = function (cuenta) { return cuenta && (cuenta.estado === 'Abierta' || cuenta.estado === 'Rechazada'); };

    vm.agregarItem = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      operacionService.agregarItem(vm.selected.id, vm.item).then(function () {
        vm.item = {};
        vm.load();
      });
    };

    vm.eliminarItem = function (item) {
      if (!vm.selected || !vm.canDelete) { return; }
      var motivo = window.prompt('Motivo de eliminacion') || '';
      operacionService.eliminarItem(vm.selected.id, item.id, { motivo: motivo }).then(vm.load);
    };

    vm.dividir = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      operacionService.dividir(vm.selected.id, !vm.selected.dividida).then(vm.load);
    };

    vm.registrarPago = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      operacionService.registrarPago(vm.selected.id, vm.pago).then(function () {
        vm.pago = { metodoPago: 'Efectivo' };
        vm.load();
      });
    };

    vm.solicitarCierre = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      operacionService.solicitarCierre(vm.selected.id).then(vm.load);
    };

    vm.load();
  });
})();
