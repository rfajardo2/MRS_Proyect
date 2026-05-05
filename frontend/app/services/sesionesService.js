(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('sesionesService', function ($http, apiConfig) {
    return {
      list: function () {
        return $http.get(apiConfig.baseUrl + '/sesiones').then(function (res) { return res.data; });
      },
      resumen: function () {
        return $http.get(apiConfig.baseUrl + '/sesiones/resumen').then(function (res) { return res.data; });
      },
      cerrar: function (id) {
        return $http.post(apiConfig.baseUrl + '/sesiones/' + id + '/cerrar');
      },
      cerrarUsuario: function (usuarioId) {
        return $http.post(apiConfig.baseUrl + '/sesiones/usuario/' + usuarioId + '/cerrar');
      },
      cerrarTodas: function () {
        return $http.post(apiConfig.baseUrl + '/sesiones/cerrar-todas');
      }
    };
  });
})();
