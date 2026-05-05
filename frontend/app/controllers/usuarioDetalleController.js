(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('UsuarioDetalleController', function ($routeParams, $location, usuariosService) {
    var vm = this;
    vm.data = null;
    vm.loading = true;
    vm.error = null;

    vm.back = function () {
      $location.path('/usuarios');
    };

    usuariosService.get($routeParams.id).then(function (data) {
      vm.data = normalize(data);
    }).catch(function (err) {
      if (err.status === 403) {
        vm.error = 'No tiene permisos para consultar el detalle de usuarios.';
        return;
      }

      if (err.status === 404) {
        vm.error = 'No se encontro el usuario solicitado.';
        return;
      }

      vm.error = 'No fue posible cargar el detalle del usuario.';
    }).finally(function () {
      vm.loading = false;
    });

    function normalize(data) {
      data = data || {};
      return {
        usuario: data.usuario || data.Usuario || {},
        sucursal: data.sucursal || data.Sucursal || null,
        permisos: data.permisos || data.Permisos || []
      };
    }
  });
})();
