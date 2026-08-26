(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('realtimeService', function ($rootScope, apiConfig, authService) {
    var connection = null;
    var handlers = [];

    function hubUrl() {
      return apiConfig.baseUrl.replace(/\/api\/?$/, '') + '/hubs/comandas';
    }

    function ensureConnection() {
      if (connection) { return connection; }
      connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl(), { accessTokenFactory: function () { return authService.getToken(); } })
        .withAutomaticReconnect()
        .build();
      connection.start().catch(angular.noop);
      return connection;
    }

    return {
      on: function (eventName, callback) {
        if (!authService.isAuthenticated()) { return; }
        var conn = ensureConnection();
        var wrapped = function () {
          var args = arguments;
          $rootScope.$applyAsync(function () {
            callback.apply(null, args);
          });
        };
        handlers.push({ eventName: eventName, callback: callback, wrapped: wrapped });
        conn.on(eventName, wrapped);
      },
      off: function (eventName, callback) {
        if (!connection) { return; }
        handlers = handlers.filter(function (h) {
          if (h.eventName === eventName && h.callback === callback) {
            connection.off(eventName, h.wrapped);
            return false;
          }
          return true;
        });
      }
    };
  });
})();
