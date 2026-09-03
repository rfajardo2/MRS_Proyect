(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('seguimientoPublicoService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/public/seguimiento';
    return {
      get: function (token) { return $http.get(url + '/' + token).then(function (res) { return res.data; }); }
    };
  });
})();
