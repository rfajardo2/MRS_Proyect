(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('pendientesService', function ($http, apiConfig) {
    return {
      resumen: function () {
        return $http.get(apiConfig.baseUrl + '/pendientes').then(function (res) { return res.data; });
      }
    };
  });
})();
