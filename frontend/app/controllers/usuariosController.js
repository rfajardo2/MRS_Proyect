(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('UsuariosController', function ($location, authService, usuariosService, rolesService, empresasService) {
    var vm = this;
    vm.search = '';
    vm.users = [];
    vm.roles = [];
    vm.empresas = [];
    vm.modalOpen = false;
    vm.error = null;
    vm.canChangePassword = authService.hasPermission('Seguridad.Usuarios.CambiarPassword');
    vm.canCreate = authService.hasPermission('Seguridad.Usuarios.Crear');
    vm.canConsult = authService.hasPermission('Seguridad.Usuarios.Consultar');
    vm.canEdit = authService.hasPermission('Seguridad.Usuarios.Editar');
    vm.canDelete = authService.hasPermission('Seguridad.Usuarios.Eliminar');

    vm.form = {};

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canChangePassword = authService.hasPermission('Seguridad.Usuarios.CambiarPassword');
        vm.canCreate = authService.hasPermission('Seguridad.Usuarios.Crear');
        vm.canConsult = authService.hasPermission('Seguridad.Usuarios.Consultar');
        vm.canEdit = authService.hasPermission('Seguridad.Usuarios.Editar');
        vm.canDelete = authService.hasPermission('Seguridad.Usuarios.Eliminar');
      });
      usuariosService.list().then(function (data) { vm.users = data; });
      rolesService.list().then(function (data) { vm.roles = data; });
      empresasService.list().then(function (data) { vm.empresas = data; });
    };

    vm.new = function () {
      if (!vm.canCreate) {
        return;
      }

      vm.form = { estado: true };
      vm.canChangePassword = true;
      vm.modalOpen = true;
    };

    vm.edit = function (user) {
      if (!vm.canEdit) {
        return;
      }

      vm.form = angular.copy(user);
      vm.form.password = '';
      vm.canChangePassword = authService.hasPermission('Seguridad.Usuarios.CambiarPassword');
      vm.modalOpen = true;
    };

    vm.save = function () {
      if ((vm.form.id && !vm.canEdit) || (!vm.form.id && !vm.canCreate)) {
        return;
      }

      var action = vm.form.id ? usuariosService.update(vm.form.id, vm.form) : usuariosService.create(vm.form);
      action.then(function () {
        vm.modalOpen = false;
        vm.load();
      }).catch(function (err) {
        vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar el usuario.';
      });
    };

    vm.toggle = function (user) {
      if (!vm.canDelete) {
        return;
      }

      var action = user.estado ? 'inactivar' : 'activar';
      var title = user.estado ? 'Inactivar usuario' : 'Activar usuario';

      Swal.fire({
        title: title,
        text: 'Desea ' + action + ' a ' + user.nombreCompleto + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Si, ' + action,
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) {
          return;
        }

        usuariosService.toggle(user.id).then(function () {
          Swal.fire({
            title: 'Listo',
            text: 'El usuario fue actualizado correctamente.',
            icon: 'success',
            timer: 1400,
            showConfirmButton: false,
            background: '#141417',
            color: '#f7f7f8'
          });
          vm.load();
        });
      });
    };

    vm.detail = function (user) {
      if (!vm.canConsult) {
        return;
      }

      $location.path('/usuarios/' + user.id);
    };

    vm.load();
  });
})();
