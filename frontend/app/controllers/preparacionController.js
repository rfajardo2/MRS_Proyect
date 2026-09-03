(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('PreparacionController', function ($scope, comandasService, authService, realtimeService, swalTheme) {
    var vm = this;
    vm.areas = [];
    vm.areaId = '';
    vm.columnas = { PENDIENTE: [], EN_PREPARACION: [], LISTO: [] };
    vm.historial = [];
    vm.canGestionar = authService.hasPermission('Preparacion.Comandas.Gestionar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canGestionar = authService.hasPermission('Preparacion.Comandas.Gestionar');
      });
      comandasService.areas().then(function (data) { vm.areas = data; }).catch(handleError);
      vm.actualizar();
    };

    vm.actualizar = function () {
      comandasService.tablero(vm.areaId || null).then(function (data) {
        var buckets = { PENDIENTE: [], EN_PREPARACION: [], LISTO: [] };
        data.forEach(function (item) {
          var clave = item.estado === 'CANCELADO' ? 'LISTO' : item.estado;
          if (buckets[clave]) {
            buckets[clave].push(item);
          }
        });
        vm.columnas = {
          PENDIENTE: agruparPorComanda(buckets.PENDIENTE),
          EN_PREPARACION: agruparPorComanda(buckets.EN_PREPARACION),
          LISTO: agruparPorComanda(buckets.LISTO)
        };
      }).catch(handleError);

      comandasService.historial(vm.areaId || null).then(function (data) {
        vm.historial = data;
      }).catch(handleError);
    };

    function agruparPorComanda(items) {
      var grupos = {};
      var orden = [];
      items.forEach(function (item) {
        item.preparar = true;
        if (!grupos[item.comandaId]) {
          grupos[item.comandaId] = {
            comandaId: item.comandaId,
            comandaNumero: item.comandaNumero,
            cuentaNumero: item.cuentaNumero,
            mesa: item.mesa,
            mesero: item.mesero,
            fechaCreacion: item.fechaCreacion,
            tienePreparacion: false,
            tieneListos: false,
            detalles: []
          };
          orden.push(item.comandaId);
        }
        if (item.requierePreparacion) {
          grupos[item.comandaId].tienePreparacion = true;
        }
        if (item.estado === 'LISTO') {
          grupos[item.comandaId].tieneListos = true;
        }
        grupos[item.comandaId].detalles.push(item);
      });
      return orden.map(function (id) { return grupos[id]; });
    }

    vm.tomarComanda = function (grupo) {
      if (!vm.canGestionar) { return; }
      comandasService.tomarComanda(grupo.comandaId).then(function () {
        showSuccess('Comanda tomada');
        vm.actualizar();
      }).catch(handleError);
    };

    vm.marcarListoComanda = function (grupo) {
      if (!vm.canGestionar) { return; }
      var detallesNoPreparar = grupo.detalles
        .filter(function (item) { return !item.preparar; })
        .map(function (item) { return item.id; });

      comandasService.marcarListoComanda(grupo.comandaId, { detallesNoPreparar: detallesNoPreparar }).then(function () {
        showSuccess('Comanda actualizada');
        vm.actualizar();
      }).catch(handleError);
    };

    vm.despacharComanda = function (grupo) {
      if (!vm.canGestionar) { return; }
      comandasService.despacharComanda(grupo.comandaId).then(function () {
        showSuccess('Comanda despachada');
        vm.actualizar();
      }).catch(handleError);
    };

    vm.despachar = function (item) {
      if (!vm.canGestionar) { return; }
      comandasService.despachar(item.id).then(function () {
        showSuccess('Producto despachado');
        vm.actualizar();
      }).catch(handleError);
    };

    vm.tiempoTranscurrido = function (fecha) {
      var minutos = Math.max(0, Math.round((Date.now() - new Date(fecha).getTime()) / 60000));
      var horas = Math.floor(minutos / 60);
      var restoMinutos = minutos % 60;
      return (horas > 0 ? (horas + 'h ') : '') + restoMinutos + 'm';
    };

    function handleError(err) {
      var message = err.status === 403
        ? 'Tu rol no tiene permiso para esta accion. Revisa permisos de Preparacion.'
        : (err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.');
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: swalTheme.background, color: swalTheme.color, confirmButtonColor: swalTheme.confirmButtonColor });
    }

    function showSuccess(title) {
      Swal.fire({ title: title, icon: 'success', timer: 1000, showConfirmButton: false, background: swalTheme.background, color: swalTheme.color });
    }

    function onTableroChanged() {
      vm.actualizar();
    }

    realtimeService.on('NuevaComanda', onTableroChanged);
    realtimeService.on('ComandaActualizada', onTableroChanged);

    $scope.$on('$destroy', function () {
      realtimeService.off('NuevaComanda', onTableroChanged);
      realtimeService.off('ComandaActualizada', onTableroChanged);
    });

    vm.load();
  });
})();
