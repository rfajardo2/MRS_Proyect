(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('InicioController', function ($location, $window) {
    var vm = this;

    vm.goMenu = function () {
      $location.path('/menu');
    };

    vm.goLogin = function () {
      $location.path('/login');
    };

    vm.share = function () {
      var shareData = {
        title: 'MRS Drunk - Menu digital',
        text: 'Mira la carta disponible de MRS Drunk.',
        url: $window.location.origin + $window.location.pathname + '#!/menu'
      };

      if ($window.navigator.share) {
        $window.navigator.share(shareData);
        return;
      }

      if ($window.navigator.clipboard) {
        $window.navigator.clipboard.writeText(shareData.url).then(function () {
          if ($window.Swal) {
            $window.Swal.fire('Enlace copiado', 'Ya puedes compartir el menu.', 'success');
          }
        });
        return;
      }

      $window.prompt('Copia este enlace para compartir el menu:', shareData.url);
    };
  });
})();
