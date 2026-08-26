(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('seguimientoRealtimeService', function ($rootScope, apiConfig) {
    var connection = null;

    function hubUrl() {
      return apiConfig.baseUrl.replace(/\/api\/?$/, '') + '/hubs/seguimiento';
    }

    return {
      connect: function (token, onUpdate) {
        connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl())
          .withAutomaticReconnect()
          .build();

        connection.on('ConsumoActualizado', function () {
          $rootScope.$applyAsync(onUpdate);
        });

        connection.onreconnected(function () {
          connection.invoke('JoinCuenta', token).catch(angular.noop);
        });

        connection.start().then(function () {
          return connection.invoke('JoinCuenta', token);
        }).catch(angular.noop);
      },
      disconnect: function () {
        if (connection) {
          connection.stop();
          connection = null;
        }
      }
    };
  });
})();
