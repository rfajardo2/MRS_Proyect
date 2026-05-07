(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('CajaController', function (operacionService, authService) {
    var vm = this;
    vm.turno = null;
    vm.turnos = [];
    vm.apertura = { saldoInicial: 0 };
    vm.cierre = {};
    vm.error = null;
    vm.message = null;
    vm.canCreate = authService.hasPermission('Operacion.Caja.Crear');
    vm.canClose = authService.hasPermission('Operacion.Caja.Cerrar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canCreate = authService.hasPermission('Operacion.Caja.Crear');
        vm.canClose = authService.hasPermission('Operacion.Caja.Cerrar');
      });

      operacionService.cajaActual().then(function (data) {
        vm.turno = data;
      });

      operacionService.cajaTurnos().then(function (data) {
        vm.turnos = data;
      });
    };

    vm.abrir = function () {
      if (!vm.canCreate) { return; }
      vm.error = null;
      operacionService.abrirCaja(vm.apertura).then(function () {
        vm.message = 'Caja abierta correctamente.';
        vm.apertura = { saldoInicial: 0 };
        vm.load();
      }).catch(handleError);
    };

    vm.cerrar = function () {
      if (!vm.turno || !vm.canClose) { return; }
      vm.error = null;
      operacionService.cerrarCaja(vm.turno.id, vm.cierre).then(function () {
        vm.message = 'Caja cerrada correctamente.';
        vm.cierre = {};
        vm.load();
      }).catch(handleError);
    };

    vm.diferenciaPreview = function () {
      if (!vm.turno || vm.cierre.efectivoReal === undefined || vm.cierre.efectivoReal === null) {
        return null;
      }

      return Number(vm.cierre.efectivoReal || 0) - Number(vm.turno.efectivoEsperado || 0);
    };

    function handleError(err) {
      vm.error = err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.';
    }

    vm.load();
  });
})();
