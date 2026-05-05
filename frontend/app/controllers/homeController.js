(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('HomeController', function (authService, menuService) {
    var vm = this;
    vm.user = authService.getUser();
    vm.modules = [];
    vm.cards = [
      { title: 'Usuarios', value: 'Base activa', icon: 'fa-users', tone: 'red' },
      { title: 'Roles', value: 'Control granular', icon: 'fa-user-shield', tone: 'white' },
      { title: 'Permisos', value: 'Menu dinamico', icon: 'fa-key', tone: 'dark' },
      { title: 'Multiempresa', value: vm.user ? vm.user.empresa : 'MRS Drunk', icon: 'fa-building', tone: 'red' }
    ];

    menuService.get().then(function (data) {
      vm.modules = data;
    });
  });
})();
