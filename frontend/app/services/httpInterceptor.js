(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('httpInterceptor', function ($q, $location, $window, loadingService) {
    function shouldTrack(config) {
      return !config || config.showLoader !== false;
    }

    return {
      request: function (config) {
        if (shouldTrack(config)) {
          loadingService.start();
        }

        var token = $window.sessionStorage.getItem('mrs_drunk_token');
        if (token) {
          config.headers.Authorization = 'Bearer ' + token;
        }
        return config;
      },
      requestError: function (rejection) {
        if (shouldTrack(rejection.config)) {
          loadingService.stop();
        }
        return $q.reject(rejection);
      },
      response: function (response) {
        if (shouldTrack(response.config)) {
          loadingService.stop();
        }
        return response;
      },
      responseError: function (rejection) {
        if (shouldTrack(rejection.config)) {
          loadingService.stop();
        }

        if (rejection.status === 401) {
          $window.sessionStorage.removeItem('mrs_drunk_token');
          $window.sessionStorage.removeItem('mrs_drunk_user');
          $location.path('/login');
        }
        return $q.reject(rejection);
      }
    };
  });
})();
