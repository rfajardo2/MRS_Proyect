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
      configuracionService.guardarVentas(vm.form)
        .then(function () { vm.saved = true; })
        .catch(function (err) { vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar la configuracion.'; });
    };

    vm.load();
  });
})();
