(function () {
  'use strict';

  // Fragmentacion (chunking): las referencias de pago ("BAR-MESA-5-CUENTA-1-
  // 20260904013522891") son ilegibles de un vistazo para quien concilia pagos.
  // Se muestran agrupadas en segmentos con sentido; si el valor no calza con
  // el patron esperado, se devuelve tal cual (dato de otro origen o legado).
  angular.module('mrsDrunkApp').filter('payuReferencia', function () {
    var pattern = /^BAR-MESA-(.+)-CUENTA-(\d+)-(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})\d{3}$/;

    return function (value) {
      if (!value) {
        return value;
      }

      var match = pattern.exec(value);
      if (!match) {
        return value;
      }

      var mesa = match[1] === 'SINMESA' ? 'Sin mesa' : ('Mesa ' + match[1]);
      var cuenta = 'Cuenta ' + match[2];
      var fecha = match[5] + '/' + match[4] + ' ' + match[6] + ':' + match[7];
      return mesa + ' · ' + cuenta + ' · ' + fecha;
    };
  });
})();
