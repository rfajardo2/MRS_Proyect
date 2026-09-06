(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('LayoutController', function ($scope, $rootScope, $location, $window, $interval, authService, menuService, pendientesService) {
    var layout = this;
    layout.user = authService.getUser();
    layout.menu = [];
    layout.search = '';
    layout.sidebarOpen = false;
    layout.menuLoaded = false;
    layout.moduleOpen = {};
    layout.pendientes = { cuentasAbiertas: 0, comandasPendientes: 0, reservasHoySinConfirmar: 0 };

    // Efecto Zeigarnik: contadores de pendientes visibles en todo momento en
    // el sidebar, no solo dentro de cada pantalla, para que no se olvide una
    // cuenta abierta o una comanda sin despachar al final del turno.
    var pendientesPorRuta = {
      '/operacion/cuentas': 'cuentasAbiertas',
      '/preparacion': 'comandasPendientes',
      '/reservas': 'reservasHoySinConfirmar'
    };

    layout.pendienteCount = function (ruta) {
      var campo = pendientesPorRuta[ruta];
      return campo ? (layout.pendientes[campo] || 0) : 0;
    };

    layout.loadPendientes = function () {
      if (!authService.isAuthenticated()) {
        return;
      }

      pendientesService.resumen().then(function (data) {
        layout.pendientes = data;
      }).catch(function () {
        // Silencioso: el badge de pendientes es informativo, no debe
        // interrumpir al usuario si esta llamada puntual falla.
      });
    };

    layout.isAuthenticated = function () {
      return authService.isAuthenticated();
    };

    layout.loadMenu = function () {
      if (!authService.isAuthenticated() || layout.menuLoaded) {
        return;
      }

      layout.user = authService.getUser();
      authService.loadPermissions();
      menuService.get().then(function (menu) {
        layout.menu = menu;
        layout.menuLoaded = true;
        layout.openActiveModule();
      });
    };

    layout.logout = function () {
      authService.logout();
      layout.menu = [];
      layout.menuLoaded = false;
      layout.user = null;
      $location.path('/inicio');
    };

    layout.go = function (ruta) {
      if (!ruta) {
        return;
      }

      if (ruta === '/admin-cuentas/usuarios') {
        ruta = '/cuentas-por-usuario';
      }

      layout.sidebarOpen = false;
      $location.path(ruta);
      if ($location.path() !== ruta) {
        $window.location.hash = '#!' + ruta;
      }
    };

    layout.isActive = function (ruta) {
      return $location.path() === ruta;
    };

    layout.toggleModule = function (modulo) {
      layout.moduleOpen[modulo.nombre] = !layout.isModuleOpen(modulo);
    };

    layout.isModuleOpen = function (modulo) {
      if (layout.search) {
        return true;
      }

      if (layout.moduleOpen[modulo.nombre] === undefined) {
        layout.moduleOpen[modulo.nombre] = layout.moduleHasActiveRoute(modulo);
      }

      return layout.moduleOpen[modulo.nombre];
    };

    layout.moduleHasActiveRoute = function (modulo) {
      return (modulo.ventanas || []).some(function (ventana) {
        return layout.isActive(ventana.ruta);
      });
    };

    layout.openActiveModule = function () {
      (layout.menu || []).forEach(function (modulo) {
        if (layout.moduleHasActiveRoute(modulo)) {
          layout.moduleOpen[modulo.nombre] = true;
        }
      });
    };

    $scope.$watch(function () { return layout.search; }, function (value) {
      layout.normalizedSearch = (value || '').toLowerCase();
    });

    $rootScope.$on('$routeChangeSuccess', function () {
      layout.loadMenu();
      layout.openActiveModule();
      layout.loadPendientes();
    });

    var pendientesInterval = $interval(layout.loadPendientes, 60000);
    $scope.$on('$destroy', function () {
      $interval.cancel(pendientesInterval);
    });

    layout.loadMenu();
    layout.loadPendientes();
  });
})();
