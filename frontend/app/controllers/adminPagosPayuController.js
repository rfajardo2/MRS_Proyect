(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AdminPagosPayUController', function ($window, $location, paymentsService, authService) {
    var vm = this;
    vm.dashboard = null;
    vm.filtered = [];
    vm.summary = null;
    vm.userSummaries = [];
    vm.userOptions = [];
    vm.orphans = [];
    vm.selectedPayments = {};
    vm.filters = defaultFilters();
    vm.expanded = {};
    vm.details = {};
    vm.loadingDetail = {};
    vm.actionBusy = {};
    vm.loading = false;
    vm.canView = authService.hasPermission('Operacion.PagosPayU.Ver');
    vm.canConciliate = authService.hasPermission('Operacion.PagosPayU.Conciliar');
    vm.canViewReports = authService.hasPermission('Operacion.PagosPayU.Reportes');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canView = authService.hasPermission('Operacion.PagosPayU.Ver');
        vm.canConciliate = authService.hasPermission('Operacion.PagosPayU.Conciliar');
        vm.canViewReports = authService.hasPermission('Operacion.PagosPayU.Reportes');
      });

      vm.loading = true;
      paymentsService.adminDashboard(buildQueryFilters()).then(function (data) {
        vm.dashboard = data || { payments: [], orphanConfirmations: [] };
        vm.userOptions = buildUserOptions(vm.dashboard.payments || []);
        vm.applyFilters();
      }).catch(handleError).finally(function () {
        vm.loading = false;
      });
    };

    vm.applyFilters = function () {
      var payments = (vm.dashboard && vm.dashboard.payments ? vm.dashboard.payments : []).filter(function (payment) {
        return !!payment;
      });

      vm.filtered = payments;
      vm.userSummaries = buildUserSummaries(payments);
      vm.summary = buildSummary(payments, vm.dashboard ? vm.dashboard.orphanConfirmations : []);
      vm.orphans = filterOrphans(vm.dashboard ? vm.dashboard.orphanConfirmations : [], vm.filters.texto);
      pruneSelection(payments);
    };

    vm.runFilters = function () {
      vm.load();
    };

    vm.resetFilters = function () {
      vm.filters = defaultFilters();
      vm.load();
    };

    vm.selectUserSummary = function (summary) {
      vm.filters.usuarioId = vm.filters.usuarioId === summary.usuarioId ? '' : summary.usuarioId;
      vm.load();
    };

    vm.toggleDetail = function (payment) {
      vm.expanded[payment.id] = !vm.expanded[payment.id];
      if (!vm.expanded[payment.id] || vm.details[payment.id] || vm.loadingDetail[payment.id]) {
        return;
      }

      vm.loadingDetail[payment.id] = true;
      paymentsService.adminDetail(payment.id).then(function (detail) {
        vm.details[payment.id] = detail;
      }).catch(handleError).finally(function () {
        vm.loadingDetail[payment.id] = false;
      });
    };

    vm.statusClass = function (estado) {
      estado = (estado || '').toUpperCase();
      if (estado === 'APPROVED') { return 'ok'; }
      if (estado === 'PENDING') { return 'pending'; }
      if (estado === 'REJECTED' || estado === 'DECLINED' || estado === 'ERROR' || estado === 'EXPIRED' || estado === 'CANCELLED') { return 'danger'; }
      return '';
    };

    vm.openCheckout = function (payment) {
      if (!payment || !payment.checkoutUrlAbsolute) { return; }
      window.open(payment.checkoutUrlAbsolute, '_blank');
    };

    vm.exportCsv = function () {
      paymentsService.exportAdmin(buildQueryFilters()).then(function (blob) {
        var url = $window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = 'payu-trazabilidad.csv';
        a.click();
        $window.URL.revokeObjectURL(url);
      }).catch(handleError);
    };

    vm.selectVisibleDiscrepancies = function () {
      vm.filtered.forEach(function (payment) {
        if (vm.canReconcileDuplicates(payment)) {
          vm.selectedPayments[payment.id] = true;
        }
      });
    };

    vm.clearSelection = function () {
      vm.selectedPayments = {};
    };

    vm.bulkReconcileSelected = function () {
      var ids = selectedPaymentIds();
      if (!ids.length) {
        Swal.fire({
          title: 'Sin seleccion',
          text: 'Selecciona al menos un intento con discrepancias conciliables.',
          icon: 'info',
          background: '#141417',
          color: '#f7f7f8',
          confirmButtonColor: '#ef233c'
        });
        return;
      }

      Swal.fire({
        title: 'Conciliar seleccionados',
        text: 'Vamos a revisar y conciliar en bloque los duplicados historicos de PayU para los intentos seleccionados.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Conciliar ' + ids.length + ' intento(s)',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        runAction('reconcile-bulk', function () {
          return paymentsService.reconcileDuplicatesBulk(ids).then(function (data) {
            vm.clearSelection();
            showBulkResult(data);
            return vm.load();
          });
        });
      });
    };

    vm.goToReports = function () {
      $location.path('/operacion/pagos-payu-reportes');
    };

    vm.refreshStatus = function (payment) {
      runAction('refresh-' + payment.id, function () {
        return paymentsService.refreshAdminStatus(payment.id).then(function (result) {
          showSuccess(result.message || 'Estado actualizado');
          clearExpanded(payment.id);
          return vm.load();
        });
      });
    };

    vm.reprocessApproved = function (payment) {
      Swal.fire({
        title: 'Reprocesar pago aprobado',
        text: 'Esto intentara crear el espejo del pago y cerrar la cuenta si corresponde.',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Reprocesar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        runAction('reprocess-' + payment.id, function () {
          return paymentsService.reprocessApproved(payment.id).then(function (data) {
            showSuccess(data.message || 'Pago reprocesado');
            clearExpanded(payment.id);
            return vm.load();
          });
        });
      });
    };

    vm.cancelPending = function (payment) {
      Swal.fire({
        title: 'Cancelar intento pendiente',
        text: 'El intento quedara cancelado y no seguira esperando confirmacion.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Cancelar intento',
        cancelButtonText: 'Volver',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        runAction('cancel-' + payment.id, function () {
          return paymentsService.cancelPending(payment.id).then(function (data) {
            showSuccess(data.message || 'Intento cancelado');
            clearExpanded(payment.id);
            return vm.load();
          });
        });
      });
    };

    vm.reconcileDuplicates = function (payment) {
      Swal.fire({
        title: 'Conciliar duplicados PayU',
        text: 'Se dejara un CuentaPago como canonico y los duplicados historicos quedaran anulados para no seguir inflando los totales.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Conciliar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef233c',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (!result.isConfirmed) { return; }
        runAction('reconcile-' + payment.id, function () {
          return paymentsService.reconcileDuplicates(payment.id).then(function (data) {
            showSuccess(data.message || 'Duplicados conciliados');
            clearExpanded(payment.id);
            return vm.load();
          });
        });
      });
    };

    vm.canRefreshStatus = function (payment) {
      return !!payment && (payment.estado === 'PENDING' || (payment.discrepancias && payment.discrepancias.length > 0));
    };

    vm.canReprocessApproved = function (payment) {
      return !!payment &&
        payment.estado === 'APPROVED' &&
        (!payment.cuentaCerrada || !payment.tienePagoAplicado || (payment.discrepancias && payment.discrepancias.length > 0));
    };

    vm.canCancelPending = function (payment) {
      return !!payment && payment.estado === 'PENDING';
    };

    vm.canReconcileDuplicates = function (payment) {
      return !!payment &&
        vm.canConciliate &&
        payment.discrepancias &&
        payment.discrepancias.some(function (issue) {
          return issue.indexOf('CuentaPagos') >= 0;
        });
    };

    vm.isSelected = function (payment) {
      return !!vm.selectedPayments[payment.id];
    };

    vm.hasSelection = function () {
      return selectedPaymentIds().length > 0;
    };

    vm.selectedCount = function () {
      return selectedPaymentIds().length;
    };

    vm.isActionBusy = function (key) {
      return !!vm.actionBusy[key];
    };

    vm.load();

    function buildSummary(payments, orphans) {
      var expected = 0;
      var paid = 0;
      var byStatus = {
        PENDING: 0,
        APPROVED: 0,
        REJECTED: 0,
        DECLINED: 0,
        ERROR: 0,
        EXPIRED: 0,
        CANCELLED: 0
      };
      var discrepancies = 0;

      payments.forEach(function (payment) {
        expected += Number(payment.valorEsperado || 0);
        paid += Number(payment.valorPagado || 0);
        if (Object.prototype.hasOwnProperty.call(byStatus, payment.estado)) {
          byStatus[payment.estado] += 1;
        }
        if (payment.discrepancias && payment.discrepancias.length) {
          discrepancies += 1;
        }
      });

      return {
        totalIntentos: payments.length,
        pending: byStatus.PENDING,
        approved: byStatus.APPROVED,
        rejected: byStatus.REJECTED + byStatus.DECLINED,
        expired: byStatus.EXPIRED,
        errors: byStatus.ERROR,
        cancelled: byStatus.CANCELLED,
        discrepancies: discrepancies,
        orphanConfirmations: (orphans || []).length,
        expected: expected,
        paid: paid
      };
    }

    function buildUserSummaries(payments) {
      var map = {};
      payments.forEach(function (payment) {
        var key = payment.meseroId || 0;
        if (!map[key]) {
          map[key] = {
            usuarioId: payment.meseroId,
            nombre: payment.mesero || 'Sin usuario',
            intentos: 0,
            approved: 0,
            pending: 0,
            rejected: 0,
            discrepancies: 0,
            expected: 0,
            paid: 0
          };
        }

        map[key].intentos += 1;
        map[key].expected += Number(payment.valorEsperado || 0);
        map[key].paid += Number(payment.valorPagado || 0);

        if (payment.estado === 'APPROVED') {
          map[key].approved += 1;
        } else if (payment.estado === 'PENDING') {
          map[key].pending += 1;
        } else if (payment.estado === 'REJECTED' || payment.estado === 'DECLINED' || payment.estado === 'ERROR' || payment.estado === 'EXPIRED' || payment.estado === 'CANCELLED') {
          map[key].rejected += 1;
        }

        if (payment.discrepancias && payment.discrepancias.length) {
          map[key].discrepancies += 1;
        }
      });

      return Object.keys(map).map(function (key) { return map[key]; }).sort(function (a, b) {
        return b.expected - a.expected || a.nombre.localeCompare(b.nombre);
      });
    }

    function buildUserOptions(payments) {
      var map = {};
      payments.forEach(function (payment) {
        if (!payment.meseroId) { return; }
        map[payment.meseroId] = {
          usuarioId: payment.meseroId,
          nombre: payment.mesero || 'Sin usuario'
        };
      });
      return Object.keys(map).map(function (key) { return map[key]; }).sort(function (a, b) {
        return a.nombre.localeCompare(b.nombre);
      });
    }

    function filterOrphans(orphanConfirmations, text) {
      var needle = (text || '').toLowerCase();
      return (orphanConfirmations || []).filter(function (item) {
        if (!needle) { return true; }
        var searchable = [item.referencia, item.transaccionPayU, item.cuentaNumero, item.usuario, item.observacion].join(' ').toLowerCase();
        return searchable.indexOf(needle) >= 0;
      });
    }

    function buildQueryFilters() {
      return {
        desde: vm.filters.desde || null,
        hasta: vm.filters.hasta || null,
        usuarioId: vm.filters.usuarioId || null,
        estado: vm.filters.estado || null,
        metodo: vm.filters.metodo || null,
        texto: vm.filters.texto || null,
        soloDiscrepancias: vm.filters.soloDiscrepancias
      };
    }

    function defaultFilters() {
      return {
        desde: formatDateInput(new Date(new Date().setDate(new Date().getDate() - 7))),
        hasta: formatDateInput(new Date()),
        usuarioId: '',
        estado: '',
        metodo: '',
        texto: '',
        soloDiscrepancias: false
      };
    }

    function clearExpanded(paymentId) {
      delete vm.expanded[paymentId];
      delete vm.details[paymentId];
      delete vm.loadingDetail[paymentId];
    }

    function selectedPaymentIds() {
      return Object.keys(vm.selectedPayments).filter(function (key) {
        return vm.selectedPayments[key];
      }).map(function (key) {
        return parseInt(key, 10);
      }).filter(function (id) {
        return !isNaN(id);
      });
    }

    function pruneSelection(payments) {
      var allowed = {};
      (payments || []).forEach(function (payment) {
        if (vm.canReconcileDuplicates(payment)) {
          allowed[payment.id] = true;
        }
      });

      Object.keys(vm.selectedPayments).forEach(function (key) {
        if (!allowed[key]) {
          delete vm.selectedPayments[key];
        }
      });
    }

    function runAction(key, action) {
      vm.actionBusy[key] = true;
      return action().catch(handleError).finally(function () {
        vm.actionBusy[key] = false;
      });
    }

    function showSuccess(message) {
      Swal.fire({
        title: 'Listo',
        text: message,
        icon: 'success',
        timer: 1300,
        showConfirmButton: false,
        background: '#141417',
        color: '#f7f7f8'
      });
    }

    function showBulkResult(result) {
      var message = [
        'Procesados: ' + (result.procesados || 0),
        'Exitos: ' + (result.exitos || 0),
        'Fallidos: ' + (result.fallidos || 0),
        'Duplicados anulados: ' + (result.duplicadosAnulados || 0)
      ].join('\n');

      Swal.fire({
        title: 'Conciliacion masiva completada',
        text: message,
        icon: result.fallidos > 0 ? 'warning' : 'success',
        background: '#141417',
        color: '#f7f7f8',
        confirmButtonColor: '#ef233c'
      });
    }

    function formatDateInput(date) {
      var year = date.getFullYear();
      var month = String(date.getMonth() + 1).padStart(2, '0');
      var day = String(date.getDate()).padStart(2, '0');
      return year + '-' + month + '-' + day;
    }

    function handleError(err) {
      var message = err.status === 403
        ? 'Tu rol no tiene permiso para esta accion. Revisa Operacion.PagosPayU.Ver, Operacion.PagosPayU.Conciliar o Operacion.PagosPayU.Reportes.'
        : (err.data && (err.data.message || err.data.title) ? (err.data.message || err.data.title) : 'No fue posible completar la operacion.');
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
    }

  });
})();
