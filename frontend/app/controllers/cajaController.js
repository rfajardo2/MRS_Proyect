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
      var cuentasAbiertas = getCuentasAbiertas();
      if (cuentasAbiertas.length) {
        return Swal.fire({
          title: 'No se puede cerrar la caja',
          html: 'Hay ' + cuentasAbiertas.length + ' cuenta(s) sin cerrar:<br><br><strong>' + cuentasAbiertas.map(function (cuenta) {
            return cuenta.numero + ' - ' + cuenta.mesero + ' (' + cuenta.estado + ')';
          }).join('<br>') + '</strong><br><br>Debes cerrar o anular esas cuentas antes de cerrar caja.',
          icon: 'warning',
          background: '#141417',
          color: '#f7f7f8',
          confirmButtonColor: '#ef233c'
        });
      }

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
      Swal.fire({
        title: 'Atencion',
        text: vm.error,
        icon: 'error',
        background: '#141417',
        color: '#f7f7f8',
        confirmButtonColor: '#ef233c'
      });
    }

    function getCuentasAbiertas() {
      if (!vm.turno || !vm.turno.cuentas) {
        return [];
      }

      return vm.turno.cuentas.filter(function (cuenta) {
        return cuenta.estado !== 'Cerrada';
      });
    }

    vm.load();
  });
})();
