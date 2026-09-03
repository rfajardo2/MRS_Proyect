(function () {
  'use strict';

  var ESTADO_LABELS = {
    PENDIENTE: 'Pendiente',
    EN_PREPARACION: 'En preparacion',
    LISTO: 'Listo',
    DESPACHADO: 'En camino',
    ENTREGADO: 'Entregado',
    CANCELADO: 'No disponible',
    SIN_COMANDA: 'Registrado'
  };

  var ESTADO_CLASES = {
    PENDIENTE: 'off',
    SIN_COMANDA: 'off',
    EN_PREPARACION: 'progreso',
    LISTO: 'progreso',
    DESPACHADO: 'progreso',
    ENTREGADO: '',
    CANCELADO: 'cancelado'
  };

  angular.module('mrsDrunkApp').controller('SeguimientoPublicoController', function ($scope, $routeParams, seguimientoPublicoService, seguimientoRealtimeService) {
    var vm = this;
    vm.loading = true;
    vm.notFound = false;
    vm.cuenta = null;

    vm.estadoLabel = function (estado) {
      return ESTADO_LABELS[estado] || estado;
    };

    vm.estadoClase = function (estado) {
      return ESTADO_CLASES[estado] || '';
    };

    vm.actualizar = function () {
      vm.loading = true;
      seguimientoPublicoService.get($routeParams.token).then(function (data) {
        vm.cuenta = data;
        vm.notFound = false;
      }).catch(function () {
        vm.cuenta = null;
        vm.notFound = true;
      }).finally(function () {
        vm.loading = false;
      });
    };

    vm.actualizar();
    seguimientoRealtimeService.connect($routeParams.token, vm.actualizar);

    $scope.$on('$destroy', function () {
      seguimientoRealtimeService.disconnect();
    });
  });
})();
