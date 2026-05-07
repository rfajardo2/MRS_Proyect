(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('MenuPublicoController', function ($location, productosService) {
    var vm = this;
    vm.loading = true;
    vm.error = null;
    vm.productos = [];
    vm.categorias = [];
    vm.categoriaActiva = '';
    vm.search = '';

    vm.goInicio = function () {
      $location.path('/inicio');
    };

    vm.goLogin = function () {
      $location.path('/login');
    };

    vm.selectCategoria = function (categoria) {
      vm.categoriaActiva = categoria || '';
    };

    vm.filteredProductos = function () {
      var term = (vm.search || '').toLowerCase();
      return vm.productos.filter(function (producto) {
        var matchCategoria = !vm.categoriaActiva || producto.categoria === vm.categoriaActiva;
        var matchTerm = !term ||
          (producto.nombre || '').toLowerCase().indexOf(term) >= 0 ||
          (producto.descripcion || '').toLowerCase().indexOf(term) >= 0 ||
          (producto.categoria || '').toLowerCase().indexOf(term) >= 0;

        return matchCategoria && matchTerm;
      });
    };

    vm.productosPorCategoria = function (categoria) {
      return vm.filteredProductos().filter(function (producto) {
        return producto.categoria === categoria;
      });
    };

    vm.categoriasVisibles = function () {
      var categorias = [];
      vm.filteredProductos().forEach(function (producto) {
        if (categorias.indexOf(producto.categoria) < 0) {
          categorias.push(producto.categoria);
        }
      });
      return categorias;
    };

    vm.formatPrice = function (value) {
      return '$' + Number(value || 0).toLocaleString('es-CO', { maximumFractionDigits: 0 });
    };

    function load() {
      vm.loading = true;
      productosService.menuPublico()
        .then(function (data) {
          vm.productos = data || [];
          vm.categorias = [];
          vm.productos.forEach(function (producto) {
            if (vm.categorias.indexOf(producto.categoria) < 0) {
              vm.categorias.push(producto.categoria);
            }
          });
          vm.categoriaActiva = vm.categorias[0] || '';
        })
        .catch(function () {
          vm.error = 'No pudimos cargar el menu disponible.';
        })
        .finally(function () {
          vm.loading = false;
        });
    }

    load();
  });
})();
