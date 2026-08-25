(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('OperacionCuentasController', function ($interval, $scope, $window, operacionService, paymentsService, productosService, configuracionService, authService) {
    var vm = this;
    vm.cuentas = [];
    vm.productos = [];
    vm.selected = null;
    vm.nueva = {};
    vm.item = {};
    vm.pago = { metodoPago: 'Efectivo', incluyePropina: false, valorPropina: 0 };
    vm.payu = { session: null, status: null, busy: false };
    vm.configuracion = { porcentajePropinaDefecto: 10 };
    vm.error = null;
    vm.canCreate = authService.hasPermission('Operacion.Cuentas.Crear');
    vm.canEdit = authService.hasPermission('Operacion.Cuentas.Editar');
    vm.canDelete = authService.hasPermission('Operacion.Cuentas.Eliminar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canCreate = authService.hasPermission('Operacion.Cuentas.Crear');
        vm.canEdit = authService.hasPermission('Operacion.Cuentas.Editar');
        vm.canDelete = authService.hasPermission('Operacion.Cuentas.Eliminar');
      });
      operacionService.misCuentas().then(function (data) {
        vm.cuentas = data;
        if (vm.selected) {
          vm.selected = vm.cuentas.find(function (x) { return x.id === vm.selected.id; }) || null;
        }
      });
      productosService.catalogoOperacion().then(function (data) { vm.productos = data; }).catch(handleError);
      configuracionService.ventasOperacion().then(function (data) { vm.configuracion = data; }).catch(handleError);
    };

    vm.crearCuenta = function () {
      if (!vm.canCreate) { return; }
      if (!vm.nueva.mesa && !vm.nueva.cliente) {
        return showWarning('Datos incompletos', 'Indica al menos la mesa o el cliente para crear la cuenta.');
      }
      operacionService.crearCuenta(vm.nueva).then(function () {
        vm.nueva = {};
        showSuccess('Cuenta creada');
        vm.load();
      }).catch(handleError);
    };

    vm.select = function (cuenta) { vm.selected = cuenta; };
    vm.isEditable = function (cuenta) { return cuenta && (cuenta.estado === 'Abierta' || cuenta.estado === 'Rechazada'); };

    vm.agregarItem = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      if (!vm.item.productoId) {
        return showWarning('Selecciona un producto', 'Debes elegir el producto que vas a agregar a la cuenta.');
      }
      if (!vm.item.cantidad || vm.item.cantidad <= 0) {
        return showWarning('Cantidad invalida', 'La cantidad debe ser mayor que cero.');
      }
      operacionService.agregarItem(vm.selected.id, vm.item).then(function () {
        vm.item = {};
        showSuccess('Producto agregado');
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
        operacionService.eliminarItem(vm.selected.id, item.id, { motivo: result.value || '' }).then(function () {
          showSuccess('Producto eliminado');
          vm.load();
        }).catch(handleError);
      });
    };

    vm.dividir = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      operacionService.dividir(vm.selected.id, !vm.selected.dividida).then(function () {
        showSuccess(vm.selected.dividida ? 'Division retirada' : 'Cuenta marcada como dividida');
        vm.load();
      }).catch(handleError);
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
      operacionService.registrarPago(vm.selected.id, vm.pago).then(function () {
        vm.pago = { metodoPago: 'Efectivo', incluyePropina: false, valorPropina: 0 };
        showSuccess('Pago registrado');
        vm.load();
      }).catch(handleError);
    };

    vm.iniciarPagoPayU = function (metodo) {
      if (!vm.selected || !vm.canEdit || !vm.isEditable(vm.selected)) { return; }
      if ((vm.selected.saldoPendiente || 0) <= 0) {
        return showWarning('Sin saldo pendiente', 'La cuenta no tiene saldo pendiente para enviar a PayU.');
      }

      vm.payu.busy = true;
      paymentsService.create({
        cuentaId: vm.selected.id,
        metodoPago: metodo,
        valor: vm.selected.saldoPendiente,
        moneda: 'COP'
      }).then(function (session) {
        vm.payu.session = session;
        vm.payu.status = session;
        vm.payu.busy = false;
        if (session.checkoutUrlAbsolute) {
          $window.open(session.checkoutUrlAbsolute, '_blank');
        }
        showSuccess('Flujo PayU generado');
        startPolling(session.paymentId);
      }).catch(function (err) {
        vm.payu.busy = false;
        handleError(err);
      });
    };

    vm.reabrirCheckout = function () {
      var url = vm.payu.status && vm.payu.status.checkoutUrlAbsolute;
      if (url) {
        $window.open(url, '_blank');
      }
    };

    vm.consultarEstadoPago = function () {
      if (!vm.payu.session) { return; }
      refreshPaymentStatus(vm.payu.session.paymentId, false);
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
        operacionService.eliminarPago(vm.selected.id, pago.id).then(function () {
          showSuccess('Pago eliminado');
          vm.load();
        }).catch(handleError);
      });
    };

    vm.togglePropina = function () {
      if (!vm.pago.incluyePropina) {
        vm.pago.valorPropina = 0;
        return;
      }

      var saldo = getSaldoCuenta();
      var exceso = vm.pago.valor && vm.pago.valor > saldo ? vm.pago.valor - saldo : 0;
      if (!vm.pago.valorPropina || vm.pago.valorPropina <= 0) {
        vm.pago.valorPropina = exceso > 0 ? exceso : vm.propinaSugerida();
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

    vm.solicitarCierre = function () {
      if (!vm.selected || !vm.canEdit) { return; }
      if (vm.selected.saldoPendiente > 0) {
        return showWarning('Pago pendiente', 'La cuenta aun tiene saldo pendiente de ' + formatMoney(vm.selected.saldoPendiente) + '.');
      }
      Swal.fire({
        title: 'Solicitar cierre',
        text: 'Confirmas el cierre de la cuenta ' + vm.selected.numero + '?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Confirmar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        operacionService.solicitarCierre(vm.selected.id).then(function () {
          showSuccess('Cierre solicitado');
          vm.load();
        }).catch(handleError);
      });
    };

    $scope.$on('$destroy', function () {
      stopPolling();
    });

    function handleError(err) {
      var message = err.status === 403
        ? 'Tu rol no tiene permiso para esta accion. Revisa permisos del rol.'
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

    function refreshPaymentStatus(paymentId, silent) {
      paymentsService.status(paymentId).then(function (status) {
        vm.payu.status = status;
        if (status.estado !== 'PENDING') {
          stopPolling();
          vm.load();
          if (!silent) {
            if (status.estado === 'APPROVED') {
              showSuccess('Pago aprobado por PayU');
            } else {
              showWarning('Actualizacion PayU', status.message || 'El pago cambio de estado.');
            }
          }
        }
      }).catch(function (err) {
        if (!silent) {
          handleError(err);
        }
      });
    }

    var pollPromise = null;

    function startPolling(paymentId) {
      stopPolling();
      refreshPaymentStatus(paymentId, true);
      pollPromise = $interval(function () {
        refreshPaymentStatus(paymentId, true);
      }, 5000);
    }

    function stopPolling() {
      if (pollPromise) {
        $interval.cancel(pollPromise);
        pollPromise = null;
      }
    }

    vm.load();
  });
})();
