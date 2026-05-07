(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('InicioController', function ($location) {
    var vm = this;

    vm.goMenu = function () {
      $location.path('/menu');
    };

    vm.goLogin = function () {
      $location.path('/login');
    };
  });
})();
