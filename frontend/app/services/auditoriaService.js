(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('auditoriaService', function ($http, apiConfig) {
    return {
      list: function (filtros) {
        return $http.get(apiConfig.baseUrl + '/auditoria', { params: filtros || {} }).then(function (res) { return res.data; });
      },
      entidades: function () {
        return $http.get(apiConfig.baseUrl + '/auditoria/entidades').then(function (res) { return res.data; });
      }
    };
  });
})();
