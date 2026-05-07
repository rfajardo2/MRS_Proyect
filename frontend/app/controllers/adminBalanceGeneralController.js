(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AdminBalanceGeneralController', function (operacionService) {
    var vm = this;
    vm.balance = null;
    vm.resumen = null;
    vm.usuarios = [];
    vm.filters = { usuarioId: '' };
    vm.expanded = {};

    vm.load = function () {
      operacionService.balanceGeneralDia().then(function (data) {
        vm.balance = data;
        vm.usuarios = buildUsuarios(data.cuentas || []);
        vm.applyFilters();
      });
    };

    vm.applyFilters = function () {
      var cuentas = (vm.balance && vm.balance.cuentas ? vm.balance.cuentas : []).filter(function (cuenta) {
        return !vm.filters.usuarioId || cuenta.meseroId === Number(vm.filters.usuarioId);
      });
      vm.resumen = buildResumen(cuentas);
    };

    vm.toggleCuenta = function (cuenta) {
      vm.expanded[cuenta.id] = !vm.expanded[cuenta.id];
    };

    function buildUsuarios(cuentas) {
      var map = {};
      cuentas.forEach(function (cuenta) {
        map[cuenta.meseroId] = { id: cuenta.meseroId, nombre: cuenta.mesero };
      });
      return Object.keys(map).map(function (key) { return map[key]; }).sort(function (a, b) {
        return a.nombre.localeCompare(b.nombre);
      });
    }

    function buildResumen(cuentas) {
      var pagos = [];
      var productos = {};
      cuentas.forEach(function (cuenta) {
        (cuenta.pagos || []).forEach(function (pago) { pagos.push(pago); });
        (cuenta.items || []).filter(function (item) { return !item.eliminado; }).forEach(function (item) {
          if (!productos[item.productoNombre]) {
            productos[item.productoNombre] = { producto: item.productoNombre, cantidad: 0, total: 0 };
          }
          productos[item.productoNombre].cantidad += Number(item.cantidad || 0);
          productos[item.productoNombre].total += Number(item.total || 0);
        });
      });

      return {
        cuentas: cuentas,
        cuentasAbiertas: cuentas.filter(function (x) { return x.estado === 'Abierta' || x.estado === 'PendienteAprobacion'; }).length,
        cuentasCerradas: cuentas.filter(function (x) { return x.estado === 'Cerrada'; }).length,
        cuentasPendientes: cuentas.filter(function (x) { return x.estado === 'PendienteAprobacion'; }).length,
        cuentasRechazadas: cuentas.filter(function (x) { return x.estado === 'Rechazada'; }).length,
        totalVendido: cuentas.filter(function (x) { return x.estado === 'Cerrada'; }).reduce(sumField('total'), 0),
        totalPagado: pagos.reduce(sumField('valor'), 0),
        totalPropinas: pagos.reduce(sumField('valorPropina'), 0),
        saldoPendiente: cuentas.reduce(sumField('saldoPendiente'), 0),
        pagosPorMetodo: buildPagosPorMetodo(pagos),
        productos: Object.keys(productos).map(function (key) { return productos[key]; }).sort(function (a, b) { return b.total - a.total; })
      };
    }

    function buildPagosPorMetodo(pagos) {
      var map = {};
      pagos.forEach(function (pago) {
        var metodo = pago.metodoPago || 'Sin metodo';
        if (!map[metodo]) {
          map[metodo] = { metodoPago: metodo, cantidad: 0, total: 0 };
        }
        map[metodo].cantidad += 1;
        map[metodo].total += Number(pago.valor || 0);
      });
      return Object.keys(map).map(function (key) { return map[key]; }).sort(function (a, b) { return a.metodoPago.localeCompare(b.metodoPago); });
    }

    function sumField(field) {
      return function (total, item) {
        return total + Number(item[field] || 0);
      };
    }

    vm.load();
  });
})();
