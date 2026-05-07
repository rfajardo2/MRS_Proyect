(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('operacionService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/operacion';
    var adminUrl = apiConfig.baseUrl + '/administracion-cuentas';
    return {
      misCuentas: function () { return $http.get(url + '/cuentas/mias').then(function (res) { return res.data; }); },
      crearCuenta: function (data) { return $http.post(url + '/cuentas', data); },
      agregarItem: function (cuentaId, data) { return $http.post(url + '/cuentas/' + cuentaId + '/items', data); },
      eliminarItem: function (cuentaId, itemId, data) { return $http.delete(url + '/cuentas/' + cuentaId + '/items/' + itemId, { data: data || {}, headers: { 'Content-Type': 'application/json' } }); },
      dividir: function (cuentaId, dividida) { return $http.post(url + '/cuentas/' + cuentaId + '/dividir', { dividida: dividida }); },
      registrarPago: function (cuentaId, data) { return $http.post(url + '/cuentas/' + cuentaId + '/pagos', data); },
      eliminarPago: function (cuentaId, pagoId) { return $http.delete(url + '/cuentas/' + cuentaId + '/pagos/' + pagoId); },
      solicitarCierre: function (cuentaId) { return $http.post(url + '/cuentas/' + cuentaId + '/solicitar-cierre'); },
      balanceDia: function () { return $http.get(url + '/balance-dia').then(function (res) { return res.data; }); },
      cajaActual: function () { return $http.get(apiConfig.baseUrl + '/caja/turno/actual').then(function (res) { return res.data; }); },
      cajaTurnos: function () { return $http.get(apiConfig.baseUrl + '/caja/turnos').then(function (res) { return res.data; }); },
      abrirCaja: function (data) { return $http.post(apiConfig.baseUrl + '/caja/turnos', data); },
      cerrarCaja: function (turnoId, data) { return $http.post(apiConfig.baseUrl + '/caja/turnos/' + turnoId + '/cerrar', data); },
      cuentasAdmin: function () { return $http.get(adminUrl).then(function (res) { return res.data; }); },
      cuentasUsuarios: function () { return $http.get(adminUrl + '/usuarios-cuentas').then(function (res) { return res.data; }); },
      agregarItemUsuario: function (cuentaId, data) { return $http.post(adminUrl + '/usuarios-cuentas/' + cuentaId + '/items', data); },
      eliminarItemUsuario: function (cuentaId, itemId, data) { return $http.delete(adminUrl + '/usuarios-cuentas/' + cuentaId + '/items/' + itemId, { data: data || {}, headers: { 'Content-Type': 'application/json' } }); },
      dividirUsuario: function (cuentaId, dividida) { return $http.post(adminUrl + '/usuarios-cuentas/' + cuentaId + '/dividir', { dividida: dividida }); },
      registrarPagoUsuario: function (cuentaId, data) { return $http.post(adminUrl + '/usuarios-cuentas/' + cuentaId + '/pagos', data); },
      eliminarPagoUsuario: function (cuentaId, pagoId) { return $http.delete(adminUrl + '/usuarios-cuentas/' + cuentaId + '/pagos/' + pagoId); },
      resolverCierre: function (cuentaId, data) { return $http.post(adminUrl + '/' + cuentaId + '/resolver-cierre', data); },
      anular: function (cuentaId, data) { return $http.post(adminUrl + '/' + cuentaId + '/anular', data); },
      balanceMeseros: function () { return $http.get(adminUrl + '/balance-meseros').then(function (res) { return res.data; }); }
    };
  });
})();
