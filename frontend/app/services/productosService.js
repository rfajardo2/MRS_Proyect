(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('productosService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/productos';
    return {
      categorias: function () { return $http.get(url + '/categorias').then(function (res) { return res.data; }); },
      crearCategoria: function (data) { return $http.post(url + '/categorias', data); },
      editarCategoria: function (id, data) { return $http.put(url + '/categorias/' + id, data); },
      productos: function () { return $http.get(url).then(function (res) { return res.data; }); },
      catalogoOperacion: function () { return $http.get(url + '/catalogo-operacion').then(function (res) { return res.data; }); },
      catalogoAdminCuentas: function () { return $http.get(url + '/catalogo-admin-cuentas').then(function (res) { return res.data; }); },
      crearProducto: function (data) { return $http.post(url, data); },
      editarProducto: function (id, data) { return $http.put(url + '/' + id, data); }
    };
  });
})();
