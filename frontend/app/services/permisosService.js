(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('permisosService', function ($http, apiConfig) {
    return {
      list: function () { return $http.get(apiConfig.baseUrl + '/permisos').then(function (res) { return res.data; }); },
      byRole: function (rolId) { return $http.get(apiConfig.baseUrl + '/permisos/rol/' + rolId).then(function (res) { return res.data; }); },
      saveRole: function (rolId, permisos) { return $http.put(apiConfig.baseUrl + '/permisos/rol/' + rolId, { permisos: permisos }); },
      windows: function () { return $http.get(apiConfig.baseUrl + '/ventanas').then(function (res) { return res.data; }); }
    };
  });
})();
