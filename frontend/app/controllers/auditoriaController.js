(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AuditoriaController', function (auditoriaService) {
    var vm = this;
    vm.registros = [];
    vm.entidades = [];
    vm.search = '';
    vm.filtroEntidad = '';
    vm.loading = true;

    var accionIcono = {
      Crear: 'fa-solid fa-plus',
      Editar: 'fa-solid fa-pen',
      Eliminar: 'fa-solid fa-trash',
      Activar: 'fa-solid fa-toggle-on',
      Desactivar: 'fa-solid fa-toggle-off',
      CambioRol: 'fa-solid fa-user-shield',
      EditarPermisos: 'fa-solid fa-key'
    };
    vm.iconoAccion = function (accion) {
      return accionIcono[accion] || 'fa-solid fa-circle-info';
    };

    vm.load = function () {
      vm.loading = true;
      auditoriaService.list({ entidad: vm.filtroEntidad || null }).then(function (data) {
        vm.registros = data;
      }).finally(function () {
        vm.loading = false;
      });
    };

    auditoriaService.entidades().then(function (data) { vm.entidades = data; });
    vm.load();
  });
})();
