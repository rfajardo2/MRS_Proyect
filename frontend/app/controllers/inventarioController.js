(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('InventarioController', function (inventarioService, authService) {
    var vm = this;
    vm.stock = [];
    vm.productosActivos = [];
    vm.movimientos = [];
    vm.proveedores = [];
    vm.compras = [];
    vm.lotes = [];
    vm.reporte = { productosVendidos: [], perdidas: [], stockValorizado: 0, comprasPeriodo: 0 };
    vm.form = { tipo: 'Entrada', cantidad: 1 };
    vm.compra = { detalles: [] };
    vm.compraDetalle = { cantidad: 1, costoUnitario: 0 };
    vm.proveedorForm = { estado: true };
    vm.minimo = {};
    vm.filters = { search: '', estadoStock: '', tipoMovimiento: '' };
    vm.tab = 'stock';
    vm.stats = { total: 0, bajoMinimo: 0, inactivos: 0 };
    vm.savingMove = false;
    vm.savingMinimo = false;
    vm.tipos = [
      { value: 'Entrada', label: 'Entrada', requiresReason: false },
      { value: 'AjusteEntrada', label: 'Ajuste entrada', requiresReason: true },
      { value: 'AjusteSalida', label: 'Ajuste salida', requiresReason: true },
      { value: 'Devolucion', label: 'Devolucion', requiresReason: true },
      { value: 'Rotura', label: 'Rotura', requiresReason: true },
      { value: 'Vencimiento', label: 'Vencimiento', requiresReason: true },
      { value: 'Dano', label: 'Dano', requiresReason: true },
      { value: 'ConsumoInterno', label: 'Consumo interno', requiresReason: true }
    ];
    vm.canMove = authService.hasPermission('Productos.Inventario.Mover');
    vm.canEdit = authService.hasPermission('Productos.Inventario.Editar');
    vm.canEntrada = authService.hasPermission('Productos.Inventario.Entrada') || vm.canMove;
    vm.canSalida = authService.hasPermission('Productos.Inventario.Salida') || vm.canMove;
    vm.canAjuste = authService.hasPermission('Productos.Inventario.Ajuste') || vm.canMove;
    vm.canCompras = authService.hasPermission('Productos.Inventario.Compras') || vm.canMove;
    vm.canReportes = authService.hasPermission('Productos.Inventario.Reportes') || authService.hasPermission('Productos.Inventario.Ver');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canMove = authService.hasPermission('Productos.Inventario.Mover');
        vm.canEdit = authService.hasPermission('Productos.Inventario.Editar');
        vm.canEntrada = authService.hasPermission('Productos.Inventario.Entrada') || vm.canMove;
        vm.canSalida = authService.hasPermission('Productos.Inventario.Salida') || vm.canMove;
        vm.canAjuste = authService.hasPermission('Productos.Inventario.Ajuste') || vm.canMove;
        vm.canCompras = authService.hasPermission('Productos.Inventario.Compras') || vm.canMove;
        vm.canReportes = authService.hasPermission('Productos.Inventario.Reportes') || authService.hasPermission('Productos.Inventario.Ver');
      });

      inventarioService.stock().then(function (data) {
        vm.stock = data || [];
        vm.productosActivos = vm.stock.filter(function (item) { return item.estado; });
        if (!vm.form.productoId && vm.productosActivos.length) {
          vm.form.productoId = vm.productosActivos[0].productoId;
        }
        refreshStats();
      }).catch(handleError);

      inventarioService.movimientos().then(function (data) {
        vm.movimientos = data || [];
      }).catch(handleError);

      inventarioService.proveedores().then(function (data) {
        vm.proveedores = data || [];
      }).catch(handleError);

      inventarioService.compras().then(function (data) {
        vm.compras = data || [];
      }).catch(handleError);

      inventarioService.lotes().then(function (data) {
        vm.lotes = data || [];
      }).catch(handleError);

      if (vm.canReportes) {
        inventarioService.reportes().then(function (data) {
          vm.reporte = data || vm.reporte;
        }).catch(handleError);
      }
    };

    vm.clearFilters = function () {
      vm.filters = { search: '', estadoStock: '', tipoMovimiento: '' };
    };

    vm.filteredStock = function () {
      var search = normalize(vm.filters.search);
      return vm.stock.filter(function (item) {
        var matchesSearch = !search ||
          normalize(item.producto).indexOf(search) >= 0 ||
          normalize(item.categoria).indexOf(search) >= 0;
        var matchesStatus = true;
        if (vm.filters.estadoStock === 'bajo') {
          matchesStatus = item.bajoMinimo && item.estado;
        } else if (vm.filters.estadoStock === 'ok') {
          matchesStatus = !item.bajoMinimo && item.estado;
        } else if (vm.filters.estadoStock === 'inactivo') {
          matchesStatus = !item.estado;
        }
        return matchesSearch && matchesStatus;
      });
    };

    vm.filteredMovimientos = function () {
      var search = normalize(vm.filters.search);
      return vm.movimientos.filter(function (mov) {
        return (!search || normalize(mov.producto).indexOf(search) >= 0 || normalize(mov.referencia).indexOf(search) >= 0 || normalize(mov.motivo).indexOf(search) >= 0) &&
          (!vm.filters.tipoMovimiento || mov.tipo === vm.filters.tipoMovimiento);
      });
    };

    vm.registrar = function (form) {
      if (!vm.canMove) { return warn('No tienes permiso para registrar movimientos de inventario.'); }
      if (!validateMovimiento(form)) { return; }
      if (!canUseTipo(vm.form.tipo)) { return warn('No tienes permiso para este tipo de movimiento.'); }

      vm.savingMove = true;
      var payload = {
        productoId: Number(vm.form.productoId),
        tipo: vm.form.tipo,
        cantidad: Number(vm.form.cantidad),
        costoUnitario: vm.form.costoUnitario === null || vm.form.costoUnitario === undefined || vm.form.costoUnitario === '' ? null : Number(vm.form.costoUnitario),
        referencia: clean(vm.form.referencia),
        motivo: clean(vm.form.motivo)
      };

      inventarioService.registrar(payload).then(function () {
        success('Movimiento registrado');
        vm.form = { tipo: 'Entrada', cantidad: 1, productoId: payload.productoId };
        vm.load();
      }).catch(handleError).finally(function () {
        vm.savingMove = false;
      });
    };

    vm.editarMinimo = function (item) {
      vm.minimo = {
        productoId: item.productoId,
        producto: item.producto,
        cantidadMinima: item.cantidadMinima
      };
    };

    vm.guardarMinimo = function (form) {
      if (!vm.canEdit) { return warn('No tienes permiso para editar el stock minimo.'); }
      if (!vm.minimo.productoId) { return warn('Selecciona un producto de la tabla.'); }
      if (form && form.$invalid) { return warn('La cantidad minima debe ser cero o mayor.'); }
      if (Number(vm.minimo.cantidadMinima) < 0) { return warn('La cantidad minima no puede ser negativa.'); }

      vm.savingMinimo = true;
      inventarioService.stockMinimo({
        productoId: vm.minimo.productoId,
        cantidadMinima: Number(vm.minimo.cantidadMinima || 0)
      }).then(function () {
        success('Stock minimo actualizado');
        vm.minimo = {};
        vm.load();
      }).catch(handleError).finally(function () {
        vm.savingMinimo = false;
      });
    };

    vm.requiereMotivo = function () {
      var tipo = vm.tipos.find(function (item) { return item.value === vm.form.tipo; });
      return tipo && tipo.requiresReason;
    };

    vm.motivoPlaceholder = function () {
      return vm.requiereMotivo() ? 'Explica el ajuste o novedad' : 'Motivo del movimiento';
    };

    vm.tipoLabel = function (value) {
      var tipo = vm.tipos.find(function (item) { return item.value === value; });
      if (tipo) { return tipo.label; }
      if (value === 'SalidaVenta') { return 'Salida por venta'; }
      return value;
    };

    vm.esSalida = function (tipo) {
      return ['AjusteSalida', 'Rotura', 'Vencimiento', 'Dano', 'ConsumoInterno', 'SalidaVenta'].indexOf(tipo) >= 0;
    };

    vm.addCompraDetalle = function () {
      if (!vm.compraDetalle.productoId) { return warn('Selecciona un producto para la compra.'); }
      if (Number(vm.compraDetalle.cantidad) <= 0) { return warn('La cantidad debe ser mayor que cero.'); }
      if (Number(vm.compraDetalle.costoUnitario) < 0) { return warn('El costo no puede ser negativo.'); }
      var producto = vm.productosActivos.find(function (p) { return p.productoId === vm.compraDetalle.productoId; });
      vm.compra.detalles.push({
        productoId: vm.compraDetalle.productoId,
        producto: producto ? producto.producto : '',
        cantidad: Number(vm.compraDetalle.cantidad),
        costoUnitario: Number(vm.compraDetalle.costoUnitario || 0),
        codigoLote: clean(vm.compraDetalle.codigoLote),
        fechaVencimiento: vm.compraDetalle.fechaVencimiento || null
      });
      vm.compraDetalle = { cantidad: 1, costoUnitario: 0 };
    };

    vm.removeCompraDetalle = function (index) {
      vm.compra.detalles.splice(index, 1);
    };

    vm.totalCompra = function () {
      return vm.compra.detalles.reduce(function (total, item) { return total + (item.cantidad * item.costoUnitario); }, 0);
    };

    vm.guardarCompra = function () {
      if (!vm.canCompras) { return warn('No tienes permiso para registrar compras.'); }
      if (!vm.compra.detalles.length) { return warn('Agrega al menos un producto a la compra.'); }
      inventarioService.crearCompra({
        proveedorId: vm.compra.proveedorId || null,
        numeroFactura: clean(vm.compra.numeroFactura),
        fechaCompra: vm.compra.fechaCompra || null,
        observacion: clean(vm.compra.observacion),
        detalles: vm.compra.detalles.map(function (item) {
          return {
            productoId: item.productoId,
            cantidad: item.cantidad,
            costoUnitario: item.costoUnitario,
            codigoLote: item.codigoLote,
            fechaVencimiento: item.fechaVencimiento
          };
        })
      }).then(function () {
        success('Compra registrada');
        vm.compra = { detalles: [] };
        vm.load();
      }).catch(handleError);
    };

    vm.crearProveedor = function () {
      if (!vm.canCompras) { return warn('No tienes permiso para crear proveedores.'); }
      if (!clean(vm.proveedorForm.nombre)) { return warn('El nombre del proveedor es obligatorio.'); }
      inventarioService.crearProveedor(vm.proveedorForm).then(function () {
        success('Proveedor creado');
        vm.proveedorForm = { estado: true };
        vm.load();
      }).catch(handleError);
    };

    function validateMovimiento(form) {
      if (form && form.$invalid) {
        return warn('Selecciona producto, tipo y una cantidad mayor que cero.');
      }

      var cantidad = Number(vm.form.cantidad);
      var costo = vm.form.costoUnitario === null || vm.form.costoUnitario === undefined || vm.form.costoUnitario === '' ? null : Number(vm.form.costoUnitario);

      if (!vm.form.productoId) { return warn('Selecciona un producto.'); }
      if (!vm.form.tipo) { return warn('Selecciona un tipo de movimiento.'); }
      if (!Number.isFinite(cantidad) || cantidad <= 0) { return warn('La cantidad debe ser mayor que cero.'); }
      if (costo !== null && (!Number.isFinite(costo) || costo < 0)) { return warn('El costo unitario no puede ser negativo.'); }
      if (vm.requiereMotivo() && !clean(vm.form.motivo)) { return warn('Este tipo de movimiento requiere motivo.'); }
      return true;
    }

    function canUseTipo(tipo) {
      if (tipo === 'Entrada') { return vm.canEntrada || vm.canCompras; }
      if (['AjusteEntrada', 'AjusteSalida', 'Rotura', 'Vencimiento', 'Dano', 'ConsumoInterno', 'Devolucion'].indexOf(tipo) >= 0) { return vm.canAjuste; }
      return vm.canSalida;
    }

    function refreshStats() {
      vm.stats = {
        total: vm.stock.length,
        bajoMinimo: vm.stock.filter(function (item) { return item.bajoMinimo && item.estado; }).length,
        inactivos: vm.stock.filter(function (item) { return !item.estado; }).length
      };
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
