(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('ProductosController', function ($location, $q, productosService, authService) {
    var vm = this;
    vm.categorias = [];
    vm.categoriasActivas = [];
    vm.unidades = [];
    vm.productos = [];
    vm.categoriaForm = {};
    vm.productoForm = {};
    vm.recetaForm = {};
    vm.recetaItems = [];
    vm.recetaProducto = null;
    vm.categoriaModal = false;
    vm.productoModal = false;
    vm.recetaModal = false;
    vm.importModal = false;
    vm.importText = '';
    vm.saving = false;
    vm.filters = { search: '', estado: '', categoriaId: '', soloInventario: false };
    vm.stats = { categoriasActivas: 0, productosActivos: 0, conInventario: 0, inactivos: 0 };
    vm.mode = $location.path() === '/productos/categorias' ? 'categorias' : 'productos';

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

      productosService.categorias().then(function (data) {
        vm.categorias = data || [];
        vm.categoriasActivas = vm.categorias.filter(function (cat) { return cat.estado; });
        refreshStats();
      }).catch(handleError);

      productosService.productos().then(function (data) {
        vm.productos = data || [];
        refreshStats();
      }).catch(handleError);

      productosService.unidades().then(function (data) {
        vm.unidades = data || [];
      }).catch(handleError);
    };

    vm.goCategorias = function () {
      $location.path('/productos/categorias');
    };

    vm.goProductos = function () {
      $location.path('/productos');
    };

    vm.clearFilters = function () {
      vm.filters = { search: '', estado: '', categoriaId: '', soloInventario: false };
    };

    vm.filteredCategorias = function () {
      var search = normalize(vm.filters.search);
      return vm.categorias.filter(function (cat) {
        return matchesEstado(cat.estado) &&
          (!search || normalize(cat.nombre).indexOf(search) >= 0 || normalize(cat.descripcion).indexOf(search) >= 0);
      });
    };

    vm.filteredProductos = function () {
      var search = normalize(vm.filters.search);
      return vm.productos.filter(function (prod) {
        var matchesSearch = !search ||
          normalize(prod.nombre).indexOf(search) >= 0 ||
          normalize(prod.categoria).indexOf(search) >= 0 ||
          normalize(prod.descripcion).indexOf(search) >= 0;

        return matchesEstado(prod.estado) &&
          matchesSearch &&
          (!vm.filters.categoriaId || Number(vm.filters.categoriaId) === prod.categoriaId) &&
          (!vm.filters.soloInventario || prod.controlaInventario);
      });
    };

    vm.countProductosCategoria = function (categoriaId) {
      return vm.productos.filter(function (prod) { return prod.categoriaId === categoriaId; }).length;
    };

    vm.margenProducto = function (producto) {
      return Number(producto.precioVenta || 0) - Number(producto.costoEstimado || 0);
    };

    vm.newCategoria = function () {
      if (!vm.canCreateCategoria) { return warn('No tienes permiso para crear categorias.'); }
      vm.categoriaForm = { estado: true, orden: nextOrden() };
      vm.categoriaModal = true;
    };

    vm.editCategoria = function (item) {
      if (!vm.canEditCategoria) { return warn('No tienes permiso para editar categorias.'); }
      vm.categoriaForm = angular.copy(item);
      vm.categoriaModal = true;
    };

    vm.saveCategoria = function (form) {
      if (!validateCategoria(form)) { return; }
      vm.saving = true;
      var payload = normalizeCategoria(vm.categoriaForm);
      var action = payload.id ? productosService.editarCategoria(payload.id, payload) : productosService.crearCategoria(payload);
      action.then(function () {
        vm.categoriaModal = false;
        vm.load();
        success('Categoria guardada');
      }).catch(handleError).finally(function () {
        vm.saving = false;
      });
    };

    vm.newProducto = function () {
      if (!vm.canCreateProducto) { return warn('No tienes permiso para crear productos.'); }
      if (!vm.categoriasActivas.length) { return warn('Crea primero una categoria activa.'); }
      vm.productoForm = {
        categoriaId: vm.categoriasActivas[0].id,
        estado: true,
        controlaInventario: true,
        precioVenta: null,
        costoEstimado: null,
        unidadVentaId: defaultUnidad('UND'),
        unidadInventarioId: defaultUnidad('UND'),
        factorConversionInventario: 1
      };
      vm.productoModal = true;
    };

    vm.editProducto = function (item) {
      if (!vm.canEditProducto) { return warn('No tienes permiso para editar productos.'); }
      vm.productoForm = angular.copy(item);
      if (!vm.categoriasActivas.some(function (cat) { return cat.id === vm.productoForm.categoriaId; })) {
        var categoriaActual = vm.categorias.find(function (cat) { return cat.id === vm.productoForm.categoriaId; });
        if (categoriaActual) {
          vm.categoriasActivas = vm.categoriasActivas.concat([categoriaActual]);
        }
      }
      vm.productoModal = true;
    };

    vm.saveProducto = function (form) {
      if (!validateProducto(form)) { return; }
      vm.saving = true;
      var payload = normalizeProducto(vm.productoForm);
      var action = payload.id ? productosService.editarProducto(payload.id, payload) : productosService.crearProducto(payload);
      action.then(function () {
        vm.productoModal = false;
        vm.load();
        success('Producto guardado');
      }).catch(handleError).finally(function () {
        vm.saving = false;
      });
    };

    function validateCategoria(form) {
      if (form && form.$invalid) {
        return warn('Completa el nombre y el orden de la categoria.');
      }

      var nombre = (vm.categoriaForm.nombre || '').trim();
      if (!nombre) { return warn('El nombre de la categoria es obligatorio.'); }
      if (nombre.length > 80) { return warn('El nombre de la categoria no puede superar 80 caracteres.'); }
      if (Number(vm.categoriaForm.orden) < 0) { return warn('El orden no puede ser negativo.'); }
      return true;
    }

    function validateProducto(form) {
      if (form && form.$invalid) {
        return warn('Completa categoria, nombre y precio de venta.');
      }

      var nombre = (vm.productoForm.nombre || '').trim();
      var precio = Number(vm.productoForm.precioVenta);
      var costo = vm.productoForm.costoEstimado === null || vm.productoForm.costoEstimado === undefined || vm.productoForm.costoEstimado === ''
        ? null
        : Number(vm.productoForm.costoEstimado);
      var factor = Number(vm.productoForm.factorConversionInventario || 1);

      if (!vm.productoForm.categoriaId) { return warn('Selecciona una categoria.'); }
      if (!nombre) { return warn('El nombre del producto es obligatorio.'); }
      if (nombre.length > 120) { return warn('El nombre del producto no puede superar 120 caracteres.'); }
      if (!Number.isFinite(precio) || precio <= 0) { return warn('El precio de venta debe ser mayor que cero.'); }
      if (costo !== null && (!Number.isFinite(costo) || costo < 0)) { return warn('El costo estimado no puede ser negativo.'); }
      if (costo !== null && costo > precio) { return warn('El costo estimado no debe ser mayor que el precio de venta.'); }
      if (!Number.isFinite(factor) || factor <= 0) { return warn('El factor de conversion debe ser mayor que cero.'); }
      return true;
    }

    function normalizeCategoria(form) {
      return {
        id: form.id,
        nombre: (form.nombre || '').trim(),
        descripcion: clean(form.descripcion),
        orden: Number(form.orden || 0),
        estado: !!form.estado
      };
    }

    function normalizeProducto(form) {
      return {
        id: form.id,
        categoriaId: Number(form.categoriaId),
        nombre: (form.nombre || '').trim(),
        descripcion: clean(form.descripcion),
        precioVenta: Number(form.precioVenta),
        costoEstimado: form.costoEstimado === null || form.costoEstimado === undefined || form.costoEstimado === '' ? null : Number(form.costoEstimado),
        unidadVentaId: form.unidadVentaId || null,
        unidadInventarioId: form.unidadInventarioId || null,
        factorConversionInventario: Number(form.factorConversionInventario || 1),
        controlaInventario: !!form.controlaInventario,
        estado: !!form.estado
      };
    }

    function matchesEstado(estado) {
      return vm.filters.estado === '' || String(estado) === vm.filters.estado;
    }

    function refreshStats() {
      vm.stats = {
        categoriasActivas: vm.categorias.filter(function (cat) { return cat.estado; }).length,
        productosActivos: vm.productos.filter(function (prod) { return prod.estado; }).length,
        conInventario: vm.productos.filter(function (prod) { return prod.controlaInventario; }).length,
        inactivos: vm.categorias.filter(function (cat) { return !cat.estado; }).length + vm.productos.filter(function (prod) { return !prod.estado; }).length
      };
    }

    function nextOrden() {
      if (!vm.categorias.length) { return 1; }
      return Math.max.apply(null, vm.categorias.map(function (cat) { return Number(cat.orden || 0); })) + 1;
    }

    vm.openReceta = function (producto) {
      if (!vm.canEditProducto) { return warn('No tienes permiso para editar recetas.'); }
      vm.recetaProducto = producto;
      vm.recetaForm = { cantidad: 1, estado: true, unidadMedidaId: producto.unidadInventarioId || defaultUnidad('UND') };
      vm.recetaItems = [];
      vm.recetaModal = true;
      productosService.receta(producto.id).then(function (data) {
        vm.recetaItems = data || [];
      }).catch(handleError);
    };

    vm.openImport = function () {
      if (!vm.canCreateProducto) { return warn('No tienes permiso para crear productos.'); }
      vm.importText = 'categoria,nombre,precio,costo,controlaInventario\nCocteles,Margarita clasica,18000,0,true';
      vm.importModal = true;
    };

    vm.importProductos = function () {
      var rows = parseImport(vm.importText);
      if (!rows.length) { return warn('Pega al menos una fila valida.'); }
      vm.saving = true;
      rows.reduce(function (chain, row) {
        return chain.then(function () { return ensureCategoria(row.categoria); })
          .then(function (categoriaId) {
            return productosService.crearProducto({
              categoriaId: categoriaId,
              nombre: row.nombre,
              descripcion: row.descripcion,
              precioVenta: row.precio,
              costoEstimado: row.costo,
              unidadVentaId: defaultUnidad('UND'),
              unidadInventarioId: defaultUnidad('UND'),
              factorConversionInventario: 1,
              controlaInventario: row.controlaInventario,
              estado: true
            });
          });
      }, $q.when()).then(function () {
        vm.importModal = false;
        success('Importacion completada');
        vm.load();
      }).catch(handleError).finally(function () {
        vm.saving = false;
      });
    };

    function parseImport(text) {
      var lines = (text || '').split(/\r?\n/).map(function (line) { return line.trim(); }).filter(Boolean);
      if (lines.length < 2) { return []; }
      return lines.slice(1).map(function (line) {
        var cols = line.split(line.indexOf(';') >= 0 ? ';' : ',').map(function (value) { return value.trim(); });
        return {
          categoria: cols[0],
          nombre: cols[1],
          precio: Number(cols[2] || 0),
          costo: cols[3] === '' || cols[3] === undefined ? null : Number(cols[3]),
          controlaInventario: String(cols[4] || 'true').toLowerCase() !== 'false',
          descripcion: cols[5] || null
        };
      }).filter(function (row) {
        return row.categoria && row.nombre && Number.isFinite(row.precio) && row.precio > 0;
      });
    }

    function ensureCategoria(nombre) {
      var existing = vm.categorias.find(function (cat) { return normalize(cat.nombre) === normalize(nombre); });
      if (existing) { return $q.when(existing.id); }
      if (!vm.canCreateCategoria) {
        return $q.reject({ data: { message: 'No existe la categoria ' + nombre + ' y no tienes permiso para crearla.' } });
      }

      return productosService.crearCategoria({ nombre: nombre, descripcion: null, orden: nextOrden(), estado: true })
        .then(function (res) {
          var categoria = res.data;
          vm.categorias.push(categoria);
          vm.categoriasActivas.push(categoria);
          return categoria.id;
        });
    }

    vm.saveRecetaItem = function () {
      if (!vm.recetaProducto) { return; }
      if (!vm.recetaForm.insumoProductoId) { return warn('Selecciona el insumo de la receta.'); }
      if (vm.recetaForm.insumoProductoId === vm.recetaProducto.id) { return warn('El producto no puede consumirse a si mismo.'); }
      if (Number(vm.recetaForm.cantidad) <= 0) { return warn('La cantidad debe ser mayor que cero.'); }

      productosService.guardarRecetaItem(vm.recetaProducto.id, {
        insumoProductoId: Number(vm.recetaForm.insumoProductoId),
        cantidad: Number(vm.recetaForm.cantidad),
        unidadMedidaId: vm.recetaForm.unidadMedidaId || null,
        estado: !!vm.recetaForm.estado
      }).then(function () {
        success('Receta actualizada');
        vm.openReceta(vm.recetaProducto);
      }).catch(handleError);
    };

    vm.deleteRecetaItem = function (item) {
      if (!vm.recetaProducto) { return; }
      productosService.eliminarRecetaItem(vm.recetaProducto.id, item.id).then(function () {
        success('Insumo retirado');
        vm.openReceta(vm.recetaProducto);
      }).catch(handleError);
    };

    vm.insumosInventario = function () {
      return vm.productos.filter(function (prod) { return prod.estado && prod.controlaInventario && (!vm.recetaProducto || prod.id !== vm.recetaProducto.id); });
    };

    function defaultUnidad(codigo) {
      var unidad = vm.unidades.find(function (item) { return item.codigo === codigo; });
      return unidad ? unidad.id : null;
    }

    function clean(value) {
      var text = (value || '').trim();
      return text || null;
    }

    function normalize(value) {
      return (value || '').toString().toLowerCase();
    }

    function handleError(err) {
      var message = err && err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.';
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
    }

    function warn(message) {
      Swal.fire({ title: 'Validacion', text: message, icon: 'warning', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
      return false;
    }

    function success(title) {
      Swal.fire({ title: title, icon: 'success', timer: 1200, showConfirmButton: false, background: '#141417', color: '#f7f7f8' });
    }

    vm.load();
  });
})();
