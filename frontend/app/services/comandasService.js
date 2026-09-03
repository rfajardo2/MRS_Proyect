(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('comandasService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/comandas';
    var preparacionUrl = apiConfig.baseUrl + '/preparacion';
    return {
      porCuenta: function (cuentaId) { return $http.get(url + '/cuentas/' + cuentaId).then(function (res) { return res.data; }); },
      enviar: function (cuentaId, data) { return $http.post(url + '/cuentas/' + cuentaId + '/enviar', data).then(function (res) { return res.data; }); },
      areas: function () { return $http.get(preparacionUrl + '/areas').then(function (res) { return res.data; }); },
      tablero: function (areaId) {
        var params = areaId ? { areaId: areaId } : {};
        return $http.get(preparacionUrl + '/tablero', { params: params }).then(function (res) { return res.data; });
      },
      tomar: function (detalleId) { return $http.post(preparacionUrl + '/detalles/' + detalleId + '/tomar'); },
      marcarListo: function (detalleId) { return $http.post(preparacionUrl + '/detalles/' + detalleId + '/listo'); },
      tomarComanda: function (comandaId) { return $http.post(preparacionUrl + '/comandas/' + comandaId + '/tomar'); },
      marcarListoComanda: function (comandaId, data) { return $http.post(preparacionUrl + '/comandas/' + comandaId + '/listo', data); },
      despacharComanda: function (comandaId) { return $http.post(preparacionUrl + '/comandas/' + comandaId + '/despachar'); },
      despachar: function (detalleId) { return $http.post(preparacionUrl + '/detalles/' + detalleId + '/despachar'); },
      entregar: function (detalleId) { return $http.post(url + '/detalles/' + detalleId + '/entregar'); },
      historial: function (areaId) {
        var params = areaId ? { areaId: areaId } : {};
        return $http.get(preparacionUrl + '/historial', { params: params }).then(function (res) { return res.data; });
      }
    };
  });
})();
