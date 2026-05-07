(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AdminUsuariosCuentasController', function (operacionService, productosService, configuracionService, authService) {
    var vm = this;
    vm.cuentas = [];
    vm.filtered = [];
    vm.userSummaries = [];
    vm.usuarios = [];
    vm.productos = [];
    vm.selected = null;
    vm.filters = { usuarioId: '', estado: '', texto: '' };
    vm.item = {};
    vm.pago = { metodoPago: 'Efectivo', incluyePropina: false, valorPropina: 0 };
    vm.configuracion = { porcentajePropinaDefecto: 10 };
    vm.canView = authService.hasPermission('AdministracionCuentas.Usuarios.Ver');
    vm.canEdit = authService.hasPermission('AdministracionCuentas.Usuarios.Editar');
    vm.canDelete = authService.hasPermission('AdministracionCuentas.Usuarios.Eliminar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canView = authService.hasPermission('AdministracionCuentas.Usuarios.Ver');
        vm.canEdit = authService.hasPermission('AdministracionCuentas.Usuarios.Editar');
        vm.canDelete = authService.hasPermission('AdministracionCuentas.Usuarios.Eliminar');
      });

      operacionService.cuentasUsuarios().then(function (data) {
        vm.cuentas = data;
        vm.usuarios = buildUsuarios(data);
        vm.applyFilters();
        if (vm.selected) {
          vm.selected = vm.cuentas.find(function (x) { return x.id === vm.selected.id; }) || null;
        }
      }).catch(handleError);

      productosService.catalogoAdminCuentas().then(function (data) {
        vm.productos = data;
      }).catch(handleError);

      configuracionService.ventasOperacion().then(function (data) {
        vm.configuracion = data;
      }).catch(handleError);
    };

    vm.applyFilters = function () {
      var text = (vm.filters.texto || '').toLowerCase();
      vm.filtered = vm.cuentas.filter(function (cuenta) {
        var matchUsuario = !vm.filters.usuarioId || cuenta.meseroId === Number(vm.filters.usuarioId);
        var matchEstado = !vm.filters.estado || cuenta.estado === vm.filters.estado;
        var searchable = [cuenta.numero, cuenta.mesero, cuenta.mesa, cuenta.cliente].join(' ').toLowerCase();
        return matchUsuario && matchEstado && (!text || searchable.indexOf(text) >= 0);
      });
      vm.userSummaries = buildUserSummaries(vm.filtered);
    };

    vm.selectUserSummary = function (summary) {
      if(vm.filters.usuarioId === summary.usuarioId) {
        vm.filters.usuarioId = '';
        vm.selected = null;
      } else {
        vm.filters.usuarioId = summary.usuarioId;
      }
     
      vm.applyFilters();
    };

    vm.select = function (cuenta) {
      vm.selected = cuenta;
      vm.item = {};
      vm.pago = { metodoPago: 'Efectivo', incluyePropina: false, valorPropina: 0 };
    };

    vm.isEditable = function (cuenta) {
      return cuenta && (cuenta.estado === 'Abierta' || cuenta.estado === 'Rechazada');
    };

    vm.agregarItem = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      if (!vm.item.productoId) {
        return showWarning('Selecciona un producto', 'Debes elegir el producto que vas a agregar a la cuenta.');
      }
      if (!vm.item.cantidad || vm.item.cantidad <= 0) {
        return showWarning('Cantidad invalida', 'La cantidad debe ser mayor que cero.');
      }
      operacionService.agregarItemUsuario(vm.selected.id, vm.item).then(function () {
        showSuccess('Producto agregado');
        vm.item = {};
        vm.load();
      }).catch(handleError);
    };

    vm.eliminarItem = function (item) {
      if (!vm.selected || !vm.canDelete) { return; }
      Swal.fire({
        title: 'Eliminar producto',
        input: 'text',
        inputLabel: 'Motivo',
        inputPlaceholder: 'Motivo de eliminacion',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        operacionService.eliminarItemUsuario(vm.selected.id, item.id, { motivo: result.value || '' }).then(function () {
          showSuccess('Producto eliminado');
          vm.load();
        }).catch(handleError);
      });
    };

    vm.registrarPago = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      vm.normalizarPagoConPropina();
      if (!vm.pago.valor || vm.pago.valor <= 0) {
        return showWarning('Valor invalido', 'El valor recibido debe ser mayor que cero.');
      }
      vm.pago.valorPropina = vm.pago.incluyePropina ? (vm.pago.valorPropina || 0) : 0;
      if (vm.pago.valorPropina < 0) {
        return showWarning('Propina invalida', 'La propina no puede ser negativa.');
      }
      if (vm.pago.valorPropina > vm.pago.valor) {
        return showWarning('Propina invalida', 'La propina no puede ser mayor que el valor recibido.');
      }
      operacionService.registrarPagoUsuario(vm.selected.id, vm.pago).then(function () {
        showSuccess('Pago registrado');
        vm.pago = { metodoPago: 'Efectivo', incluyePropina: false, valorPropina: 0 };
        vm.load();
      }).catch(handleError);
    };

    vm.eliminarPago = function (pago) {
      if (!vm.selected || !vm.canEdit) { return; }
      Swal.fire({
        title: 'Eliminar pago',
        text: 'Deseas eliminar este pago de ' + formatMoney(pago.valor) + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        operacionService.eliminarPagoUsuario(vm.selected.id, pago.id).then(function () {
          showSuccess('Pago eliminado');
          vm.load();
        }).catch(handleError);
      });
    };

    vm.dividir = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      operacionService.dividirUsuario(vm.selected.id, !vm.selected.dividida).then(function () {
        showSuccess(vm.selected.dividida ? 'Division retirada' : 'Cuenta marcada como dividida');
        vm.load();
      }).catch(handleError);
    };

    vm.togglePropina = function () {
      if (!vm.pago.incluyePropina) {
        vm.pago.valorPropina = 0;
        return;
      }
      if (!vm.pago.valorPropina || vm.pago.valorPropina <= 0) {
        vm.pago.valorPropina = vm.propinaSugerida();
      }
      vm.normalizarPagoConPropina();
    };

    vm.propinaSugerida = function () {
      var base = vm.selected ? vm.selected.total : 0;
      var porcentaje = vm.configuracion.porcentajePropinaDefecto || 0;
      return Math.round(base * porcentaje / 100);
    };

    vm.aplicarPropinaSugerida = function () {
      if (!vm.selected) { return; }
      vm.pago.incluyePropina = true;
      vm.pago.valorPropina = vm.propinaSugerida();
      vm.normalizarPagoConPropina();
    };

    vm.normalizarPagoConPropina = function () {
      if (!vm.pago.incluyePropina) { return; }
      var propina = Number(vm.pago.valorPropina || 0);
      var saldo = getSaldoCuenta();
      if (propina < 0) { return; }
      if (!vm.pago.valor || vm.pago.valor <= saldo || vm.pago.valor < saldo + propina) {
        vm.pago.valor = saldo + propina;
      }
    };

    function buildUsuarios(cuentas) {
      var map = {};
      cuentas.forEach(function (cuenta) {
        map[cuenta.meseroId] = { id: cuenta.meseroId, nombre: cuenta.mesero };
      });
      return Object.keys(map).map(function (key) { return map[key]; }).sort(function (a, b) { return a.nombre.localeCompare(b.nombre); });
    }

    function buildUserSummaries(cuentas) {
      var map = {};
      cuentas.forEach(function (cuenta) {
        var key = cuenta.meseroId || 0;
        if (!map[key]) {
          map[key] = {
            usuarioId: cuenta.meseroId,
            nombre: cuenta.mesero || 'Sin usuario',
            cuentas: 0,
            abiertas: 0,
            pendientes: 0,
            cerradas: 0,
            anuladas: 0,
            total: 0,
            pagado: 0,
            propina: 0,
            saldo: 0
          };
        }

        map[key].cuentas += 1;
        map[key].total += Number(cuenta.total || 0);
        map[key].pagado += Number(cuenta.totalPagado || 0);
        map[key].propina += Number(cuenta.totalPropina || 0);
        map[key].saldo += Number(cuenta.saldoPendiente || 0);

        if (cuenta.estado === 'Abierta') {
          map[key].abiertas += 1;
        } else if (cuenta.estado === 'PendienteAprobacion') {
          map[key].pendientes += 1;
        } else if (cuenta.estado === 'Cerrada') {
          map[key].cerradas += 1;
        } else if (cuenta.estado === 'Anulada') {
          map[key].anuladas += 1;
        }
      });

      return Object.keys(map).map(function (key) { return map[key]; }).sort(function (a, b) {
        return b.total - a.total || a.nombre.localeCompare(b.nombre);
      });
    }

    function handleError(err) {
      var message = err.status === 403
        ? 'Tu rol no tiene permiso para esta accion. Revisa permisos de Cuentas por usuario.'
        : (err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.');
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
    }

    function showWarning(title, text) {
      Swal.fire({ title: title, text: text, icon: 'warning', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
    }

    function showSuccess(title) {
      Swal.fire({ title: title, icon: 'success', timer: 1200, showConfirmButton: false, background: '#141417', color: '#f7f7f8' });
    }

    function formatMoney(value) {
      return '$' + Math.round(value || 0).toLocaleString('es-CO');
    }

    function getSaldoCuenta() {
      return vm.selected ? (vm.selected.saldoPendiente || vm.selected.total || 0) : 0;
    }

    vm.load();
  });
})();
