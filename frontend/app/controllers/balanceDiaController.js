(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('BalanceDiaController', function (operacionService) {
    var vm = this;
    vm.balance = null;
    vm.expanded = {};

    vm.toggleCuenta = function (cuenta) {
      vm.expanded[cuenta.id] = !vm.expanded[cuenta.id];
    };

    operacionService.balanceDia().then(function (data) { vm.balance = data; });
  });
})();
