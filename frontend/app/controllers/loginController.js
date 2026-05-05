(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('LoginController', function ($location, authService) {
    var vm = this;
    vm.credentials = { usuarioOCorreo: '', password: '' };
    vm.loading = false;
    vm.error = null;
    vm.showPassword = false;

    vm.togglePassword = function () {
      vm.showPassword = !vm.showPassword;
    };

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
