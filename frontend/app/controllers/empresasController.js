(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('EmpresasController', function (empresasService, authService) {
    var vm = this;
    vm.empresas = [];
    vm.sucursales = [];
    vm.form = {};
    vm.sucursalForm = {};
    vm.modalOpen = false;
    vm.sucursalModalOpen = false;
    vm.error = null;
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
      empresasService.sucursales().then(function (data) { vm.sucursales = data; });
    };

    vm.new = function () {
      if (!vm.canCreate) {
        return;
      }

      vm.form = {
        estado: true,
        esPrincipal: vm.empresas.length === 0,
        tipoDocumento: 'NIT',
        regimenTributario: 'Responsable de IVA',
        responsabilidadFiscal: 'R-99-PN',
        pais: 'CO'
      };
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
      }).catch(function (err) {
        vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar la empresa.';
      });
    };

    vm.newSucursal = function (empresa) {
      if (!vm.canCreate) {
        return;
      }

      vm.sucursalForm = {
        empresaId: empresa ? empresa.id : null,
        estado: true,
        esPrincipal: false,
        pais: 'CO'
      };
      vm.sucursalModalOpen = true;
    };

    vm.editSucursal = function (sucursal) {
      if (!vm.canEdit) {
        return;
      }

      vm.sucursalForm = angular.copy(sucursal);
      vm.sucursalModalOpen = true;
    };

    vm.saveSucursal = function () {
      if ((vm.sucursalForm.id && !vm.canEdit) || (!vm.sucursalForm.id && !vm.canCreate)) {
        return;
      }

      var action = vm.sucursalForm.id
        ? empresasService.updateSucursal(vm.sucursalForm.id, vm.sucursalForm)
        : empresasService.createSucursal(vm.sucursalForm);

      action.then(function () {
        vm.sucursalModalOpen = false;
        vm.load();
      }).catch(function (err) {
        vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar la sede.';
      });
    };

    vm.load();
  });
})();
