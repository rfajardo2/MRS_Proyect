(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('ProductosController', function (productosService, authService) {
    var vm = this;
    vm.categorias = [];
    vm.productos = [];
    vm.categoriaForm = {};
    vm.productoForm = {};
    vm.categoriaModal = false;
    vm.productoModal = false;
    vm.error = null;
    vm.canCreateCategoria = authService.hasPermission('Productos.Categorias.Crear');
    vm.canEditCategoria = authService.hasPermission('Productos.Categorias.Editar');
    vm.canCreateProducto = authService.hasPermission('Productos.Productos.Crear');
    vm.canEditProducto = authService.hasPermission('Productos.Productos.Editar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canCreateCategoria = authService.hasPermission('Productos.Categorias.Crear');
        vm.canEditCategoria = authService.hasPermission('Productos.Categorias.Editar');
        vm.canCreateProducto = authService.hasPermission('Productos.Productos.Crear');
        vm.canEditProducto = authService.hasPermission('Productos.Productos.Editar');
      });
      productosService.categorias().then(function (data) { vm.categorias = data; });
      productosService.productos().then(function (data) { vm.productos = data; });
    };

    vm.newCategoria = function () { vm.categoriaForm = { estado: true, orden: 0 }; vm.categoriaModal = true; };
    vm.editCategoria = function (item) { vm.categoriaForm = angular.copy(item); vm.categoriaModal = true; };
    vm.saveCategoria = function () {
      var action = vm.categoriaForm.id ? productosService.editarCategoria(vm.categoriaForm.id, vm.categoriaForm) : productosService.crearCategoria(vm.categoriaForm);
      action.then(function () { vm.categoriaModal = false; vm.load(); })
        .catch(function (err) { vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar la categoria.'; });
    };

    vm.newProducto = function () { vm.productoForm = { estado: true, controlaInventario: false, precioVenta: 0 }; vm.productoModal = true; };
    vm.editProducto = function (item) { vm.productoForm = angular.copy(item); vm.productoModal = true; };
    vm.saveProducto = function () {
      var action = vm.productoForm.id ? productosService.editarProducto(vm.productoForm.id, vm.productoForm) : productosService.crearProducto(vm.productoForm);
      action.then(function () { vm.productoModal = false; vm.load(); })
        .catch(function (err) { vm.error = err.data && err.data.message ? err.data.message : 'No fue posible guardar el producto.'; });
    };

    vm.load();
  });
})();
