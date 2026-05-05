(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('BalanceDiaController', function (operacionService) {
    var vm = this;
    vm.balance = null;
    operacionService.balanceDia().then(function (data) { vm.balance = data; });
  });
})();
