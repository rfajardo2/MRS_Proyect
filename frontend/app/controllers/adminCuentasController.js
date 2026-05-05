(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AdminCuentasController', function (operacionService, authService) {
    var vm = this;
    vm.cuentas = [];
    vm.balance = [];
    vm.canEdit = authService.hasPermission('AdministracionCuentas.Cuentas.Editar');
    vm.canDelete = authService.hasPermission('AdministracionCuentas.Cuentas.Eliminar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canEdit = authService.hasPermission('AdministracionCuentas.Cuentas.Editar');
        vm.canDelete = authService.hasPermission('AdministracionCuentas.Cuentas.Eliminar');
      });
      operacionService.cuentasAdmin().then(function (data) { vm.cuentas = data; });
      operacionService.balanceMeseros().then(function (data) { vm.balance = data; });
    };

    vm.aprobar = function (cuenta) {
      if (!vm.canEdit) { return; }
      operacionService.resolverCierre(cuenta.id, { aprobar: true }).then(vm.load);
    };

    vm.rechazar = function (cuenta) {
      if (!vm.canEdit) { return; }
      var motivo = window.prompt('Motivo de rechazo') || '';
      operacionService.resolverCierre(cuenta.id, { aprobar: false, motivo: motivo }).then(vm.load);
    };

    vm.anular = function (cuenta) {
      if (!vm.canDelete) { return; }
      var motivo = window.prompt('Motivo de anulacion') || '';
      operacionService.anular(cuenta.id, { motivo: motivo }).then(vm.load);
    };

    vm.load();
  });
})();
