(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('PaymentResultController', function ($interval, $location, $window, paymentsService) {
    var vm = this;
    vm.status = null;
    vm.paymentId = null;
    vm.reference = null;
    vm.error = null;

    vm.goInicio = function () { $location.path('/inicio'); };
    vm.goLogin = function () { $location.path('/login'); };
    vm.reopen = function () {
      if (vm.status && vm.status.checkoutUrlAbsolute) {
        $window.open(vm.status.checkoutUrlAbsolute, '_blank');
      }
    };

    var pollPromise = null;

    vm.load = function () {
      vm.paymentId = parseInt(readParam('extra3') || readParam('paymentId') || '0', 10) || 0;
      vm.reference = readParam('referenceCode') || readParam('reference_sale') || readParam('reference_pol') || '';
      if (!vm.paymentId) {
        vm.error = 'No encontramos el identificador del pago para consultar su estado.';
        return;
      }

      refresh();
      pollPromise = $interval(refresh, 5000);
    };

    function refresh() {
      paymentsService.publicStatus(vm.paymentId, vm.reference).then(function (status) {
        vm.status = status;
        vm.error = null;
        if (status.estado !== 'PENDING') {
          stopPolling();
        }
      }).catch(function () {
        vm.error = 'No fue posible consultar el estado del pago por ahora.';
      });
    }

    function stopPolling() {
      if (pollPromise) {
        $interval.cancel(pollPromise);
        pollPromise = null;
      }
    }

    function readParam(name) {
      var href = $window.location.href || '';
      var match = href.match(new RegExp('[?&#]' + name + '=([^&#]+)'));
      return match ? decodeURIComponent(match[1].replace(/\+/g, ' ')) : '';
    }

    vm.load();
  });
})();
