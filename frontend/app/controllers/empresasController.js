(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('EmpresasController', function (empresasService, authService) {
    var vm = this;
    vm.empresas = [];
    vm.form = {};
    vm.modalOpen = false;
    vm.permissionTooltip = 'No tiene permisos para realizar esta accion';
    vm.canCreate = authService.hasPermission('Configuracion.Empresas.Crear');
    vm.canEdit = authService.hasPermission('Configuracion.Empresas.Editar');

    function refreshPermissions() {
      authService.loadPermissions().then(function () {
        vm.canCreate = authService.hasPermission('Configuracion.Empresas.Crear');
        vm.canEdit = authService.hasPermission('Configuracion.Empresas.Editar');
      });
    }

    vm.load = function () {
      refreshPermissions();
      empresasService.list().then(function (data) { vm.empresas = data; });
    };

    vm.new = function () {
      if (!vm.canCreate) {
        return;
      }

      vm.form = { estado: true };
      vm.modalOpen = true;
    };

    vm.edit = function (empresa) {
      if (!vm.canEdit) {
        return;
      }

      vm.form = angular.copy(empresa);
      vm.modalOpen = true;
    };

    vm.save = function () {
      if ((vm.form.id && !vm.canEdit) || (!vm.form.id && !vm.canCreate)) {
        return;
      }

      var action = vm.form.id ? empresasService.update(vm.form.id, vm.form) : empresasService.create(vm.form);
      action.then(function () {
        vm.modalOpen = false;
        vm.load();
      });
    };

    vm.load();
  });
})();
