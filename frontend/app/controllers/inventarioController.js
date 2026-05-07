(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('InventarioController', function (inventarioService, authService) {
    var vm = this;
    vm.stock = [];
    vm.movimientos = [];
    vm.form = { tipo: 'Entrada', cantidad: 1 };
    vm.minimo = {};
    vm.error = null;
    vm.message = null;
    vm.tipos = [
      { value: 'Entrada', label: 'Entrada' },
      { value: 'AjusteEntrada', label: 'Ajuste entrada' },
      { value: 'AjusteSalida', label: 'Ajuste salida' },
      { value: 'Devolucion', label: 'Devolucion' },
      { value: 'Rotura', label: 'Rotura' },
      { value: 'Vencimiento', label: 'Vencimiento' },
      { value: 'Dano', label: 'Dano' },
      { value: 'ConsumoInterno', label: 'Consumo interno' }
    ];
    vm.canMove = authService.hasPermission('Productos.Inventario.Mover');
    vm.canEdit = authService.hasPermission('Productos.Inventario.Editar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canMove = authService.hasPermission('Productos.Inventario.Mover');
        vm.canEdit = authService.hasPermission('Productos.Inventario.Editar');
      });

      inventarioService.stock().then(function (data) {
        vm.stock = data;
        if (!vm.form.productoId && vm.stock.length) {
          vm.form.productoId = vm.stock[0].productoId;
        }
      });

      inventarioService.movimientos().then(function (data) {
        vm.movimientos = data;
      });
    };

    vm.registrar = function () {
      if (!vm.canMove) { return; }
      vm.error = null;
      inventarioService.registrar(vm.form).then(function () {
        vm.message = 'Movimiento registrado correctamente.';
        vm.form = { tipo: 'Entrada', cantidad: 1 };
        vm.load();
      }).catch(handleError);
    };

    vm.editarMinimo = function (item) {
      vm.minimo = { productoId: item.productoId, producto: item.producto, cantidadMinima: item.cantidadMinima };
    };

    vm.guardarMinimo = function () {
      if (!vm.canEdit || !vm.minimo.productoId) { return; }
      inventarioService.stockMinimo(vm.minimo).then(function () {
        vm.message = 'Stock minimo actualizado.';
        vm.minimo = {};
        vm.load();
      }).catch(handleError);
    };

    function handleError(err) {
      vm.error = err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.';
    }

    vm.load();
  });
})();
