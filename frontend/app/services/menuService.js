(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('menuService', function ($http, apiConfig) {
    return {
      get: function () {
        return $http.get(apiConfig.baseUrl + '/menu').then(function (res) { return res.data; });
      }
    };
  });
})();
