(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('nominaService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/nomina';

    return {
      empleados: function () { return $http.get(url + '/empleados').then(function (res) { return res.data; }); },
      crearEmpleado: function (data) { return $http.post(url + '/empleados', data); },
      editarEmpleado: function (id, data) { return $http.put(url + '/empleados/' + id, data); },
      toggleEmpleado: function (id) { return $http.delete(url + '/empleados/' + id); },
      periodos: function () { return $http.get(url + '/periodos').then(function (res) { return res.data; }); },
      crearPeriodo: function (data) { return $http.post(url + '/periodos', data).then(function (res) { return res.data; }); },
      editarPeriodo: function (id, data) { return $http.put(url + '/periodos/' + id, data); },
      control: function (periodoId) { return $http.get(url + '/control/' + periodoId).then(function (res) { return res.data; }); },
      guardarRegistro: function (periodoId, data) { return $http.put(url + '/control/' + periodoId + '/registro', data); },
      eliminarRegistro: function (periodoId, registroId) { return $http.delete(url + '/control/' + periodoId + '/registro/' + registroId); },
      cerrarPeriodo: function (periodoId) { return $http.post(url + '/control/' + periodoId + '/cerrar'); }
    };
  });
})();
