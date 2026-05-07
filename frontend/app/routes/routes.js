(function () {
  'use strict';

  angular.module('mrsDrunkApp').config(function ($routeProvider, $httpProvider) {
    $routeProvider
      .when('/login', {
        templateUrl: 'app/views/login.html',
        controller: 'LoginController',
        controllerAs: 'vm',
        public: true
      })
      .when('/home', {
        templateUrl: 'app/views/home.html'
      })
      .when('/usuarios', {
        templateUrl: 'app/views/usuarios.html'
      })
      .when('/usuarios/:id', {
        templateUrl: 'app/views/usuario-detalle.html'
      })
      .when('/roles', {
        templateUrl: 'app/views/roles.html'
      })
      .when('/permisos', {
        templateUrl: 'app/views/permisos.html'
      })
      .when('/empresas', {
        templateUrl: 'app/views/empresas.html'
      })
      .when('/sesiones', {
        templateUrl: 'app/views/sesiones.html'
      })
      .when('/nomina', {
        redirectTo: '/nomina/resumen'
      })
      .when('/nomina/resumen', {
        templateUrl: 'app/views/nomina.html'
      })
      .when('/nomina/registro-diario', {
        templateUrl: 'app/views/nomina.html'
      })
      .when('/nomina/control-diario', {
        templateUrl: 'app/views/nomina.html'
      })
      .when('/nomina/novedades', {
        templateUrl: 'app/views/nomina.html'
      })
      .when('/nomina/empleados', {
        templateUrl: 'app/views/nomina.html'
      })
      .when('/nomina/periodos', {
        templateUrl: 'app/views/nomina.html'
      })
      .when('/configuracion/ventas', {
        templateUrl: 'app/views/configuracion-ventas.html'
      })
      .when('/productos/categorias', {
        templateUrl: 'app/views/productos.html'
      })
      .when('/productos', {
        templateUrl: 'app/views/productos.html'
      })
      .when('/productos/inventario', {
        templateUrl: 'app/views/inventario.html'
      })
      .when('/operacion/cuentas', {
        templateUrl: 'app/views/operacion-cuentas.html'
      })
      .when('/operacion/balance', {
        templateUrl: 'app/views/balance-dia.html'
      })
      .when('/operacion/caja', {
        templateUrl: 'app/views/caja.html'
      })
      .when('/admin-cuentas', {
        templateUrl: 'app/views/admin-cuentas.html'
      })
      .when('/admin-cuentas/usuarios', {
        redirectTo: '/cuentas-por-usuario'
      })
      .when('/cuentas-por-usuario', {
        templateUrl: 'app/views/admin-usuarios-cuentas.html'
      })
      .when('/admin-cuentas/balance', {
        templateUrl: 'app/views/admin-balance-general.html'
      })
      .otherwise({ redirectTo: '/home' });

    $httpProvider.interceptors.push('httpInterceptor');
  });

  angular.module('mrsDrunkApp').run(function ($rootScope, $location, authService, loadingService) {
    $rootScope.$on('$routeChangeStart', function (event, next) {
      if (!next.public && !authService.isAuthenticated()) {
        event.preventDefault();
        $location.path('/login');
      }

      if (next.public && authService.isAuthenticated()) {
        event.preventDefault();
        $location.path('/home');
      }
    });

    $rootScope.$on('$routeChangeError', function () {
      loadingService.reset();
    });
  });
})();
