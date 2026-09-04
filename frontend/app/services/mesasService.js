(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('mesasService', function ($http, apiConfig) {
    var base = apiConfig.baseUrl + '/mesas';
    return {
      list: function () {
        return $http.get(base).then(function (res) { return res.data; });
      },
      crear: function (mesa) {
        return $http.post(base, mesa).then(function (res) { return res.data; });
      },
      editar: function (id, mesa) {
        return $http.put(base + '/' + id, mesa);
      },
      cambiarEstado: function (id, estado) {
        return $http.put(base + '/' + id + '/estado', { estado: estado });
      }
    };
  });
})();
