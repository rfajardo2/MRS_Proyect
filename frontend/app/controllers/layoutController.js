(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('LayoutController', function ($scope, $rootScope, $location, $window, authService, menuService) {
    var layout = this;
    layout.user = authService.getUser();
    layout.menu = [];
    layout.search = '';
    layout.sidebarOpen = false;
    layout.menuLoaded = false;
    layout.moduleOpen = {};

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
    });

    layout.loadMenu();
  });
})();
