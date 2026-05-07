(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('inventarioService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/inventario';
    return {
      stock: function () { return $http.get(url + '/stock').then(function (res) { return res.data; }); },
      movimientos: function () { return $http.get(url + '/movimientos').then(function (res) { return res.data; }); },
      registrar: function (data) { return $http.post(url + '/movimientos', data); },
      stockMinimo: function (data) { return $http.put(url + '/stock-minimo', data); }
    };
  });
})();
