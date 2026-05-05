(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('LoginController', function ($location, authService) {
    var vm = this;
    vm.credentials = { usuarioOCorreo: 'admin', password: 'Admin123*' };
    vm.loading = false;
    vm.error = null;

    vm.login = function () {
      vm.loading = true;
      vm.error = null;

      authService.login(vm.credentials)
        .then(function () { $location.path('/home'); })
        .catch(function () { vm.error = 'No pudimos iniciar sesion. Revisa tus datos e intenta nuevamente.'; })
        .finally(function () { vm.loading = false; });
    };
  });
})();
