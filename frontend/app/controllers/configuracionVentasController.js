(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('ConfiguracionVentasController', function (configuracionService, authService) {
    var vm = this;
    vm.form = null;
    vm.error = null;
    vm.saved = false;
    vm.canEdit = authService.hasPermission('Configuracion.Ventas.Editar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canEdit = authService.hasPermission('Configuracion.Ventas.Editar');
      });
      configuracionService.ventas().then(function (data) { vm.form = data; });
    };

    vm.save = function () {
      if (!vm.canEdit) { return; }
      vm.error = null;
      vm.saved = false;
      if (vm.form.porcentajePropinaDefecto < 0) {
        return Swal.fire({ title: 'Valor invalido', text: 'La propina por defecto no puede ser negativa.', icon: 'warning', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
      }
      configuracionService.guardarVentas(vm.form)
        .then(function () {
          vm.saved = true;
          Swal.fire({ title: 'Configuracion guardada', icon: 'success', timer: 1200, showConfirmButton: false, background: '#141417', color: '#f7f7f8' });
        })
        .catch(function (err) {
          vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar la configuracion.';
          Swal.fire({ title: 'Atencion', text: vm.error, icon: 'error', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
        });
    };

    vm.load();
  });
})();
