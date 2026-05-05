(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('RolesController', function (rolesService, empresasService, authService) {
    var vm = this;
    vm.roles = [];
    vm.empresas = [];
    vm.form = {};
    vm.modalOpen = false;
    vm.error = null;
    vm.permissionTooltip = 'No tiene permisos para realizar esta accion';
    vm.canCreate = authService.hasPermission('Seguridad.Roles.Crear');
    vm.canEdit = authService.hasPermission('Seguridad.Roles.Editar');
    vm.canDelete = authService.hasPermission('Seguridad.Roles.Eliminar');

    function refreshPermissions() {
      authService.loadPermissions().then(function () {
        vm.canCreate = authService.hasPermission('Seguridad.Roles.Crear');
        vm.canEdit = authService.hasPermission('Seguridad.Roles.Editar');
        vm.canDelete = authService.hasPermission('Seguridad.Roles.Eliminar');
      });
    }

    vm.load = function () {
      refreshPermissions();
      rolesService.list().then(function (data) { vm.roles = data; });
      empresasService.list().then(function (data) { vm.empresas = data; });
    };

    vm.new = function () {
      if (!vm.canCreate) {
        return;
      }

      vm.form = { estado: true, esSuperUsuario: false };
      vm.modalOpen = true;
    };

    vm.edit = function (role) {
      if (!vm.canEdit || role.esSuperUsuario) {
        return;
      }

      vm.form = angular.copy(role);
      vm.modalOpen = true;
    };

    vm.save = function () {
      if ((vm.form.id && !vm.canEdit) || (!vm.form.id && !vm.canCreate) || vm.form.esSuperUsuario) {
        return;
      }

      var action = vm.form.id ? rolesService.update(vm.form.id, vm.form) : rolesService.create(vm.form);
      action.then(function () {
        vm.modalOpen = false;
        vm.load();
      }).catch(function (err) {
        vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar el rol.';
      });
    };

    vm.toggle = function (role) {
      if (!vm.canDelete || role.esSuperUsuario) {
        return;
      }

      rolesService.toggle(role.id).then(vm.load);
    };

    vm.roleEditTooltip = function (role) {
      return role.esSuperUsuario ? 'El rol SuperUsuario no se puede editar' : vm.permissionTooltip;
    };

    vm.roleToggleTooltip = function (role) {
      return role.esSuperUsuario ? 'El rol SuperUsuario no se puede inactivar' : vm.permissionTooltip;
    };

    vm.load();
  });
})();
