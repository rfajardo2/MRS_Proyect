(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AdminCuentasController', function (operacionService, authService) {
    var vm = this;
    vm.cuentas = [];
    vm.solicitudesPendientes = [];
    vm.cuentasHistoricas = [];
    vm.balance = [];
    vm.canEdit = authService.hasPermission('AdministracionCuentas.Cuentas.Editar');
    vm.canDelete = authService.hasPermission('AdministracionCuentas.Cuentas.Eliminar');
    vm.expanded = {};

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canEdit = authService.hasPermission('AdministracionCuentas.Cuentas.Editar');
        vm.canDelete = authService.hasPermission('AdministracionCuentas.Cuentas.Eliminar');
      });
      operacionService.cuentasAdmin().then(function (data) {
        vm.cuentas = data;
        vm.solicitudesPendientes = data.filter(function (cuenta) { return cuenta.estado === 'PendienteAprobacion'; });
        vm.cuentasHistoricas = data.filter(function (cuenta) { return cuenta.estado !== 'PendienteAprobacion'; });
      });
      operacionService.balanceMeseros().then(function (data) { vm.balance = data; });
    };

    vm.togglePagos = function (cuenta) {
      vm.expanded[cuenta.id] = !vm.expanded[cuenta.id];
    };

    vm.aprobar = function (cuenta) {
      if (!vm.canEdit) { return; }
      Swal.fire({
        title: 'Aprobar cierre',
        text: 'Confirmas el cierre de la cuenta ' + cuenta.numero + '?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Aprobar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        operacionService.resolverCierre(cuenta.id, { aprobar: true }).then(function () {
          showSuccess('Cuenta aprobada');
          vm.load();
        }).catch(handleError);
      });
    };

    vm.rechazar = function (cuenta) {
      if (!vm.canEdit) { return; }
      Swal.fire({
        title: 'Rechazar cierre',
        input: 'text',
        inputLabel: 'Motivo',
        inputPlaceholder: 'Motivo del rechazo',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Rechazar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        operacionService.resolverCierre(cuenta.id, { aprobar: false, motivo: result.value || '' }).then(function () {
          showSuccess('Cuenta rechazada');
          vm.load();
        }).catch(handleError);
      });
    };

    vm.anular = function (cuenta) {
      if (!vm.canDelete) { return; }
      Swal.fire({
        title: 'Anular cuenta',
        input: 'text',
        inputLabel: 'Motivo',
        inputPlaceholder: 'Motivo de anulacion',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Anular',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        operacionService.anular(cuenta.id, { motivo: result.value || '' }).then(function () {
          showSuccess('Cuenta anulada');
          vm.load();
        }).catch(handleError);
      });
    };

    function handleError(err) {
      var message = err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.';
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
    }

    function showSuccess(title) {
      Swal.fire({ title: title, icon: 'success', timer: 1200, showConfirmButton: false, background: '#141417', color: '#f7f7f8' });
    }

    vm.load();
  });
})();
