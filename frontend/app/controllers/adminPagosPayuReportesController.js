(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('AdminPagosPayuReportesController', function ($window, $location, paymentsService, authService) {
    var vm = this;
    vm.report = null;
    vm.loading = false;
    vm.userOptions = [];
    vm.canView = authService.hasPermission('Operacion.PagosPayU.Reportes');
    vm.filters = defaultFilters();

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canView = authService.hasPermission('Operacion.PagosPayU.Reportes');
      });

      vm.loading = true;
      paymentsService.executiveReport(buildFilters()).then(function (data) {
        vm.report = data;
        vm.userOptions = (data && data.usuarios ? data.usuarios : []).map(function (item) {
          return { id: item.meseroId, nombre: item.mesero };
        });
      }).catch(handleError).finally(function () {
        vm.loading = false;
      });
    };

    vm.runFilters = function () {
      vm.load();
    };

    vm.resetFilters = function () {
      vm.filters = defaultFilters();
      vm.load();
    };

    vm.goToTraceability = function () {
      $location.path('/operacion/pagos-payu');
    };

    vm.exportExcel = function () {
      paymentsService.exportExecutive(buildFilters()).then(function (blob) {
        var url = $window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = 'payu-reporte-ejecutivo.xlsx';
        a.click();
        $window.URL.revokeObjectURL(url);
      }).catch(handleError);
    };

    vm.load();

    function buildFilters() {
      return {
        desde: vm.filters.desde || null,
        hasta: vm.filters.hasta || null,
        usuarioId: vm.filters.usuarioId || null,
        metodo: vm.filters.metodo || null,
        estado: vm.filters.estado || null
      };
    }

    function defaultFilters() {
      return {
        desde: formatDateInput(new Date(new Date().setDate(new Date().getDate() - 7))),
        hasta: formatDateInput(new Date()),
        usuarioId: '',
        metodo: '',
        estado: ''
      };
    }

    function formatDateInput(date) {
      var year = date.getFullYear();
      var month = String(date.getMonth() + 1).padStart(2, '0');
      var day = String(date.getDate()).padStart(2, '0');
      return year + '-' + month + '-' + day;
    }

    function handleError(err) {
      var message = err.status === 403
        ? 'Tu rol no tiene permiso para esta ventana. Revisa Operacion.PagosPayU.Reportes.'
        : (err.data && (err.data.message || err.data.title) ? (err.data.message || err.data.title) : 'No fue posible cargar el reporte ejecutivo.');
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: '#141417', color: '#f7f7f8', confirmButtonColor: '#ef233c' });
    }
  });
})();
