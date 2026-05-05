(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('configuracionService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/configuracion';
    return {
      ventas: function () { return $http.get(url + '/ventas').then(function (res) { return res.data; }); },
      guardarVentas: function (data) { return $http.put(url + '/ventas', data); }
    };
  });
})();
