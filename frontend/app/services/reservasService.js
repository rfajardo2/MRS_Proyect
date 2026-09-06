(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('reservasService', function ($http, apiConfig) {
    var base = apiConfig.baseUrl + '/reservas';
    return {
      list: function (filtros) {
        return $http.get(base, { params: filtros || {} }).then(function (res) { return res.data; });
      },
      crear: function (reserva) {
        return $http.post(base, reserva).then(function (res) { return res.data; });
      },
      editar: function (id, reserva) {
        return $http.put(base + '/' + id, reserva);
      },
      cambiarEstado: function (id, estado) {
        return $http.put(base + '/' + id + '/estado', { estado: estado });
      }
    };
  });
})();
