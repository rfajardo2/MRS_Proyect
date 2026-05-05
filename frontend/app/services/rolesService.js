(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('rolesService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/roles';
    return {
      list: function () { return $http.get(url).then(function (res) { return res.data; }); },
      get: function (id) { return $http.get(url + '/' + id).then(function (res) { return res.data; }); },
      create: function (data) { return $http.post(url, data); },
      update: function (id, data) { return $http.put(url + '/' + id, data); },
      toggle: function (id) { return $http.delete(url + '/' + id); }
    };
  });
})();
