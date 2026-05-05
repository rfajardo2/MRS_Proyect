(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('empresasService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/empresas';
    return {
      list: function () { return $http.get(url).then(function (res) { return res.data; }); },
      create: function (data) { return $http.post(url, data); },
      update: function (id, data) { return $http.put(url + '/' + id, data); }
    };
  });
})();
