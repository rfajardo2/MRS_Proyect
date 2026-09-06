(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('ReservasController', function (mesasService, reservasService, authService, swalTheme) {
    var vm = this;
    vm.mesas = [];
    vm.reservas = [];
    vm.mesaForm = {};
    vm.reservaForm = {};
    vm.mesaModal = false;
    vm.reservaModal = false;
    vm.saving = false;
    vm.filtroFecha = new Date().toISOString().slice(0, 10);

    vm.estadosMesa = ['Libre', 'Ocupada', 'Reservada', 'FueraDeServicio'];
    vm.estadosReserva = ['Pendiente', 'Confirmada', 'Cancelada', 'Completada', 'NoShow'];

    vm.canCreateMesa = authService.hasPermission('Reservas.Mesas.Crear');
    vm.canEditMesa = authService.hasPermission('Reservas.Mesas.Editar');
    vm.canCreateReserva = authService.hasPermission('Reservas.Reservas.Crear');
    vm.canEditReserva = authService.hasPermission('Reservas.Reservas.Editar');

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canCreateMesa = authService.hasPermission('Reservas.Mesas.Crear');
        vm.canEditMesa = authService.hasPermission('Reservas.Mesas.Editar');
        vm.canCreateReserva = authService.hasPermission('Reservas.Reservas.Crear');
        vm.canEditReserva = authService.hasPermission('Reservas.Reservas.Editar');
      });

      mesasService.list().then(function (data) { vm.mesas = data || []; }).catch(handleError);
      vm.loadReservas();
    };

    vm.loadReservas = function () {
      var desde = vm.filtroFecha ? new Date(vm.filtroFecha + 'T00:00:00') : null;
      var hasta = vm.filtroFecha ? new Date(vm.filtroFecha + 'T23:59:59') : null;
      reservasService.list({ desde: desde ? desde.toISOString() : null, hasta: hasta ? hasta.toISOString() : null })
        .then(function (data) { vm.reservas = data || []; })
        .catch(handleError);
    };

    vm.mesaClase = function (mesa) {
      return 'mesa-card mesa-' + (mesa.estado || 'Libre').toLowerCase();
    };

    var iconosEstadoMesa = {
      Libre: 'fa-circle-check',
      Ocupada: 'fa-utensils',
      Reservada: 'fa-clock',
      FueraDeServicio: 'fa-ban'
    };
    // Ley de Pragnanz: un icono grande y distinto por estado se lee de un
    // vistazo desde lejos, algo que el color solo (o texto pequeno) no logra.
    vm.iconoEstadoMesa = function (estado) {
      return iconosEstadoMesa[estado] || 'fa-circle-question';
    };

    vm.reservasDeMesa = function (mesaId) {
      return vm.reservas.filter(function (r) {
        return r.mesaId === mesaId && (r.estado === 'Pendiente' || r.estado === 'Confirmada');
      });
    };

    vm.cambiarEstadoMesa = function (mesa, estado) {
      if (!vm.canEditMesa) { return warn('No tienes permiso para editar mesas.'); }
      // Actualizacion optimista (Umbral de Doherty): el select ya cambio el modelo
      // de inmediato (ng-model), no se espera la respuesta del servidor para que se
      // vea reflejado en el plano. Si falla, se resincroniza desde el servidor.
      mesasService.cambiarEstado(mesa.id, estado).catch(function (err) {
        handleError(err);
        mesasService.list().then(function (data) { vm.mesas = data || []; });
      });
    };

    vm.newMesa = function () {
      if (!vm.canCreateMesa) { return warn('No tienes permiso para crear mesas.'); }
      vm.mesaForm = { nombre: '', capacidad: 4, posicionX: 0, posicionY: 0, activa: true };
      vm.mesaModal = true;
    };

    vm.editMesa = function (mesa) {
      if (!vm.canEditMesa) { return warn('No tienes permiso para editar mesas.'); }
      vm.mesaForm = angular.copy(mesa);
      vm.mesaModal = true;
    };

    vm.saveMesa = function (form) {
      if (form && form.$invalid) { return warn('Completa el nombre y la capacidad de la mesa.'); }
      vm.saving = true;
      var payload = vm.mesaForm;
      var action = payload.id ? mesasService.editar(payload.id, payload) : mesasService.crear(payload);
      action.then(function () {
        vm.mesaModal = false;
        vm.load();
        success('Mesa guardada');
      }).catch(handleError).finally(function () {
        vm.saving = false;
      });
    };

    vm.newReserva = function (mesa) {
      if (!vm.canCreateReserva) { return warn('No tienes permiso para crear reservas.'); }
      var hoy = new Date();
      hoy.setMinutes(0, 0, 0);
      hoy.setHours(hoy.getHours() + 1);
      vm.reservaForm = {
        mesaId: mesa ? mesa.id : (vm.mesas[0] && vm.mesas[0].id),
        cliente: '',
        telefono: '',
        numeroPersonas: mesa ? mesa.capacidad : 2,
        fechaHora: hoy,
        duracionMinutos: 90,
        observacion: ''
      };
      vm.reservaModal = true;
    };

    vm.editReserva = function (reserva) {
      if (!vm.canEditReserva) { return warn('No tienes permiso para editar reservas.'); }
      vm.reservaForm = angular.copy(reserva);
      vm.reservaForm.fechaHora = new Date(vm.reservaForm.fechaHora);
      vm.reservaModal = true;
    };

    vm.saveReserva = function (form) {
      if (form && form.$invalid) { return warn('Completa el cliente, la mesa y la fecha de la reserva.'); }
      vm.saving = true;
      var payload = vm.reservaForm;
      var action = payload.id ? reservasService.editar(payload.id, payload) : reservasService.crear(payload);
      action.then(function () {
        vm.reservaModal = false;
        vm.loadReservas();
        success('Reserva guardada');
      }).catch(handleError).finally(function () {
        vm.saving = false;
      });
    };

    vm.cambiarEstadoReserva = function (reserva, estado) {
      if (!vm.canEditReserva) { return warn('No tienes permiso para editar reservas.'); }
      // Actualizacion optimista: se refleja de inmediato en la agenda y se revierte si falla.
      var estadoAnterior = reserva.estado;
      reserva.estado = estado;
      reservasService.cambiarEstado(reserva.id, estado).catch(function (err) {
        reserva.estado = estadoAnterior;
        handleError(err);
      });
    };

    function handleError(err) {
      var message = err && err.data && err.data.message ? err.data.message : 'No fue posible completar la operacion.';
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: swalTheme.background, color: swalTheme.color, confirmButtonColor: swalTheme.confirmButtonColor });
    }

    function warn(message) {
      Swal.fire({ title: 'Validacion', text: message, icon: 'warning', background: swalTheme.background, color: swalTheme.color, confirmButtonColor: swalTheme.confirmButtonColor });
      return false;
    }

    function success(title) {
      Swal.fire({ title: title, icon: 'success', timer: 1200, showConfirmButton: false, background: swalTheme.background, color: swalTheme.color });
    }

    vm.load();
  });
})();
