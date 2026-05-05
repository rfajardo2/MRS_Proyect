(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('SesionesController', function (authService, sesionesService) {
    var vm = this;
    vm.sesiones = [];
    vm.resumen = [];
    vm.search = '';
    vm.canClose = authService.hasPermission('Seguridad.Sesiones.Cerrar');
    vm.canCloseAll = authService.hasPermission('Seguridad.Sesiones.CerrarTodas');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canClose = authService.hasPermission('Seguridad.Sesiones.Cerrar');
        vm.canCloseAll = authService.hasPermission('Seguridad.Sesiones.CerrarTodas');
      });
      sesionesService.list().then(function (data) { vm.sesiones = data; });
      sesionesService.resumen().then(function (data) { vm.resumen = data; });
    };

    vm.cerrar = function (sesion) {
      if (!vm.canClose || sesion.esSesionActual) {
        return;
      }

      Swal.fire({
        title: 'Cerrar sesion',
        text: 'Desea cerrar la sesion de ' + sesion.nombreCompleto + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Si, cerrar',
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (result.isConfirmed) {
          sesionesService.cerrar(sesion.id).then(vm.load);
        }
      });
    };

    vm.cerrarUsuario = function (item) {
      if (!vm.canClose) {
        return;
      }

      Swal.fire({
        title: 'Cerrar sesiones',
        text: 'Desea cerrar todas las sesiones de ' + item.nombreCompleto + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Si, cerrar',
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (result.isConfirmed) {
          sesionesService.cerrarUsuario(item.usuarioId).then(vm.load);
        }
      });
    };

    vm.cerrarTodas = function () {
      if (!vm.canCloseAll) {
        return;
      }

      Swal.fire({
        title: 'Cerrar todos los usuarios',
        text: 'Se cerraran todas las sesiones activas excepto la tuya.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Si, cerrar todas',
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (result.isConfirmed) {
          sesionesService.cerrarTodas().then(vm.load);
        }
      });
    };

    vm.load();
  });
})();
