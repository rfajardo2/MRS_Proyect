(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('loadingService', function ($rootScope, $timeout) {
    var pending = 0;
    var visible = false;

    function publish() {
      var nextVisible = pending > 0;
      if (visible === nextVisible) {
        return;
      }

      visible = nextVisible;
      $timeout(function () {
        $rootScope.globalLoading = visible;
      }, 0);
    }

    return {
      start: function () {
        pending += 1;
        publish();
      },
      stop: function () {
        pending = Math.max(0, pending - 1);
        publish();
      },
      reset: function () {
        pending = 0;
        publish();
      }
    };
  });
})();
