(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('authService', function ($http, $window, apiConfig) {
    var tokenKey = 'mrs_drunk_token';
    var userKey = 'mrs_drunk_user';
    var permissionsKey = 'mrs_drunk_permissions';

    function setSession(response) {
      $window.sessionStorage.setItem(tokenKey, response.token);
      $window.sessionStorage.setItem(userKey, angular.toJson(response.usuario));
    }

    return {
      login: function (credentials) {
        return $http.post(apiConfig.baseUrl + '/auth/login', credentials).then(function (res) {
          setSession(res.data);
          return res.data;
        });
      },
      logout: function () {
        var token = $window.sessionStorage.getItem(tokenKey);
        if (token) {
          $http.post(apiConfig.baseUrl + '/auth/logout', null, { showLoader: false }).catch(angular.noop);
        }
        $window.sessionStorage.removeItem(tokenKey);
        $window.sessionStorage.removeItem(userKey);
        $window.sessionStorage.removeItem(permissionsKey);
      },
      getToken: function () {
        return $window.sessionStorage.getItem(tokenKey);
      },
      getUser: function () {
        return angular.fromJson($window.sessionStorage.getItem(userKey) || 'null');
      },
      isAuthenticated: function () {
        return !!$window.sessionStorage.getItem(tokenKey);
      },
      me: function () {
        return $http.get(apiConfig.baseUrl + '/auth/me');
      },
      loadPermissions: function () {
        return $http.get(apiConfig.baseUrl + '/auth/permissions').then(function (res) {
          $window.sessionStorage.setItem(permissionsKey, angular.toJson(res.data || []));
          return res.data || [];
        });
      },
      hasPermission: function (code) {
        var user = this.getUser();
        if (user && user.esSuperUsuario) {
          return true;
        }

        var permissions = angular.fromJson($window.sessionStorage.getItem(permissionsKey) || '[]');
        return permissions.indexOf(code) >= 0;
      }
    };
  });
})();
