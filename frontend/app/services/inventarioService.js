(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('inventarioService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/inventario';
    return {
      stock: function () { return $http.get(url + '/stock').then(function (res) { return res.data; }); },
      movimientos: function () { return $http.get(url + '/movimientos').then(function (res) { return res.data; }); },
      registrar: function (data) { return $http.post(url + '/movimientos', data); },
      stockMinimo: function (data) { return $http.put(url + '/stock-minimo', data); },
      proveedores: function () { return $http.get(url + '/proveedores').then(function (res) { return res.data; }); },
      crearProveedor: function (data) { return $http.post(url + '/proveedores', data); },
      compras: function () { return $http.get(url + '/compras').then(function (res) { return res.data; }); },
      crearCompra: function (data) { return $http.post(url + '/compras', data); },
      lotes: function () { return $http.get(url + '/lotes').then(function (res) { return res.data; }); },
      reportes: function () { return $http.get(url + '/reportes').then(function (res) { return res.data; }); }
    };
  });
})();
