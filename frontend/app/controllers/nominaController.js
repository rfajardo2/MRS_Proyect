(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('NominaController', function ($filter, $location, $q, $window, authService, nominaService) {
    var vm = this;
    vm.activeTab = resolveTab();
    vm.periodos = [];
    vm.periodoId = null;
    vm.control = null;
    vm.cells = {};
    vm.saving = {};
    vm.message = null;
    vm.modalEmpleado = false;
    vm.modalPeriodo = false;
    vm.empleadoForm = buildEmpleadoDefaults();
    vm.periodoForm = {};
    vm.novedadForm = {};
    vm.dailyDate = null;
    vm.dailyRows = [];
    vm.dailyNovedadForms = {};
    vm.dailyNovedadOpen = {};
    vm.modalDailyNovedad = false;
    vm.dailyNovedadRow = null;
    vm.canExportControl = authService.hasPermission('Nomina.Control.Exportar');
    vm.treeOpen = {
      period: true,
      empleados: {},
      devengados: {},
      deducciones: {}
    };
    vm.conceptosNovedad = {
      Devengo: [
        { codigo: 'Salario', nombre: 'Salario' },
        { codigo: 'AuxilioTransporte', nombre: 'Auxilio de transporte' },
        { codigo: 'HorasExtrasDiurnas', nombre: 'Horas extras diurnas' },
        { codigo: 'HorasExtrasNocturnas', nombre: 'Horas extras nocturnas' },
        { codigo: 'RecargoNocturno', nombre: 'Recargo nocturno' },
        { codigo: 'DominicalFestivo', nombre: 'Dominical o festivo' },
        { codigo: 'Vacaciones', nombre: 'Vacaciones' },
        { codigo: 'PrimaServicios', nombre: 'Prima de servicios' },
        { codigo: 'Cesantias', nombre: 'Cesantias' },
        { codigo: 'InteresesCesantias', nombre: 'Intereses de cesantias' },
        { codigo: 'Incapacidad', nombre: 'Incapacidad' },
        { codigo: 'LicenciaRemunerada', nombre: 'Licencia remunerada' },
        { codigo: 'LicenciaMaternidadPaternidad', nombre: 'Licencia maternidad/paternidad' },
        { codigo: 'Comision', nombre: 'Comision' },
        { codigo: 'Bonificacion', nombre: 'Bonificacion' },
        { codigo: 'ViaticoSalarial', nombre: 'Viatico salarial' },
        { codigo: 'Reintegro', nombre: 'Reintegro' }
      ],
      Deduccion: [
        { codigo: 'Salud', nombre: 'Salud' },
        { codigo: 'Pension', nombre: 'Pension' },
        { codigo: 'FondoSolidaridadPensional', nombre: 'Fondo solidaridad pensional' },
        { codigo: 'RetencionFuente', nombre: 'Retencion en la fuente' },
        { codigo: 'Libranza', nombre: 'Libranza' },
        { codigo: 'PrestamoEmpresa', nombre: 'Prestamo empresa' },
        { codigo: 'EmbargoJudicial', nombre: 'Embargo judicial' },
        { codigo: 'Cooperativa', nombre: 'Cooperativa' },
        { codigo: 'Afc', nombre: 'Ahorro AFC' },
        { codigo: 'PlanComplementarioSalud', nombre: 'Plan complementario salud' },
        { codigo: 'Sancion', nombre: 'Sancion' },
        { codigo: 'OtraDeduccion', nombre: 'Otra deduccion' }
      ]
    };
    vm.tiposNovedad = ['Devengo', 'Deduccion'];
    vm.tiposDocumento = ['CC', 'CE', 'TI', 'PPT', 'PAS'];
    vm.generos = ['Masculino', 'Femenino', 'Otro', 'No informa'];
    vm.estadosCiviles = ['Soltero', 'Casado', 'Union libre', 'Separado', 'Viudo', 'No informa'];
    vm.tiposContrato = ['Termino indefinido', 'Termino fijo', 'Obra o labor', 'Aprendizaje', 'Prestacion de servicios'];
    vm.tiposTrabajador = ['Dependiente', 'Independiente', 'Aprendiz', 'Pensionado'];
    vm.subtiposCotizante = ['No aplica', 'Dependiente pensionado', 'Cotizante con requisitos cumplidos', 'Aprendiz Sena'];
    vm.tiposSalario = ['Ordinario', 'Integral'];
    vm.periodicidadesPago = ['Diario', 'Semanal', 'Quincenal', 'Mensual'];
    vm.jornadasLaborales = ['Tiempo completo', 'Medio tiempo', 'Por turnos', 'Por dias'];
    vm.nivelesRiesgoArl = ['I', 'II', 'III', 'IV', 'V'];
    vm.tiposCuenta = ['Ahorros', 'Corriente', 'Nequi', 'Daviplata', 'Efectivo'];
    vm.paises = ['CO - Colombia', 'VE - Venezuela', 'PE - Peru', 'EC - Ecuador', 'US - Estados Unidos', 'ES - Espana', 'Otro'];
    vm.departamentosColombia = [
      'Amazonas', 'Antioquia', 'Arauca', 'Atlantico', 'Bogota D.C.', 'Bolivar', 'Boyaca', 'Caldas', 'Caqueta',
      'Casanare', 'Cauca', 'Cesar', 'Choco', 'Cordoba', 'Cundinamarca', 'Guainia', 'Guaviare', 'Huila',
      'La Guajira', 'Magdalena', 'Meta', 'Narino', 'Norte de Santander', 'Putumayo', 'Quindio', 'Risaralda',
      'San Andres y Providencia', 'Santander', 'Sucre', 'Tolima', 'Valle del Cauca', 'Vaupes', 'Vichada'
    ];
    vm.municipiosPorDepartamento = {
      'Antioquia': ['Medellin', 'Bello', 'Envigado', 'Itagui', 'Sabaneta', 'Rionegro', 'Apartado', 'Turbo', 'Caucasia'],
      'Atlantico': ['Barranquilla', 'Soledad', 'Malambo', 'Sabanalarga', 'Puerto Colombia'],
      'Bogota D.C.': ['Bogota D.C.'],
      'Bolivar': ['Cartagena', 'Magangue', 'Turbaco', 'Arjona', 'El Carmen de Bolivar'],
      'Boyaca': ['Tunja', 'Duitama', 'Sogamoso', 'Chiquinquira', 'Paipa'],
      'Caldas': ['Manizales', 'La Dorada', 'Chinchina', 'Villamaria', 'Riosucio'],
      'Caqueta': ['Florencia', 'San Vicente del Caguan', 'Cartagena del Chaira'],
      'Casanare': ['Yopal', 'Aguazul', 'Villanueva', 'Tauramena'],
      'Cauca': ['Popayan', 'Santander de Quilichao', 'Puerto Tejada', 'Patia'],
      'Cesar': ['Valledupar', 'Aguachica', 'Bosconia', 'Codazzi'],
      'Choco': ['Quibdo', 'Istmina', 'Tado', 'Acandi'],
      'Cordoba': ['Monteria', 'Lorica', 'Sahagun', 'Cereté', 'Planeta Rica'],
      'Cundinamarca': ['Soacha', 'Facatativa', 'Chia', 'Zipaquira', 'Girardot', 'Fusagasuga', 'Mosquera'],
      'Huila': ['Neiva', 'Pitalito', 'Garzon', 'La Plata'],
      'La Guajira': ['Riohacha', 'Maicao', 'Uribia', 'San Juan del Cesar'],
      'Magdalena': ['Santa Marta', 'Cienaga', 'Fundacion', 'El Banco'],
      'Meta': ['Villavicencio', 'Acacias', 'Granada', 'Puerto Lopez'],
      'Narino': ['Pasto', 'Tumaco', 'Ipiales', 'Tuquerres'],
      'Norte de Santander': ['Cucuta', 'Ocaña', 'Pamplona', 'Villa del Rosario'],
      'Putumayo': ['Mocoa', 'Puerto Asis', 'Orito', 'Valle del Guamuez'],
      'Quindio': ['Armenia', 'Calarca', 'Montenegro', 'Quimbaya'],
      'Risaralda': ['Pereira', 'Dosquebradas', 'Santa Rosa de Cabal', 'La Virginia'],
      'Santander': ['Bucaramanga', 'Floridablanca', 'Giron', 'Piedecuesta', 'Barrancabermeja'],
      'Sucre': ['Sincelejo', 'Corozal', 'Sampues', 'Tolu'],
      'Tolima': ['Ibague', 'Espinal', 'Melgar', 'Honda'],
      'Valle del Cauca': ['Cali', 'Palmira', 'Buenaventura', 'Tulua', 'Buga', 'Cartago', 'Jamundi', 'Yumbo'],
      'Amazonas': ['Leticia', 'Puerto Narino'],
      'Arauca': ['Arauca', 'Saravena', 'Tame'],
      'Guainia': ['Inirida'],
      'Guaviare': ['San Jose del Guaviare'],
      'San Andres y Providencia': ['San Andres', 'Providencia'],
      'Vaupes': ['Mitu'],
      'Vichada': ['Puerto Carreno']
    };

    vm.load = function () {
      authService.loadPermissions().then(function () {
        vm.canExportControl = authService.hasPermission('Nomina.Control.Exportar');
      });

      nominaService.periodos().then(function (periodos) {
        vm.periodos = periodos;
        if (!vm.periodoId && periodos.length) {
          vm.periodoId = periodos[0].id;
        }
        if (vm.periodoId) {
          vm.loadControl();
        }
      });
    };

    vm.loadControl = function () {
      if (!vm.periodoId) {
        vm.control = null;
        return;
      }

      nominaService.control(vm.periodoId).then(function (data) {
        vm.control = data;
        buildCells();
        buildDailyRows();
        buildDailyNovedadForms();
        resetNovedadForm();
      });
    };

    vm.setTab = function (tab) {
      var routes = {
        resumen: '/nomina/resumen',
        registro: '/nomina/registro-diario',
        control: '/nomina/control-diario',
        novedades: '/nomina/novedades',
        empleados: '/nomina/empleados',
        periodos: '/nomina/periodos'
      };
      $location.path(routes[tab] || '/nomina/resumen');
    };

    vm.openPeriodo = function () {
      var today = new Date();
      vm.periodoForm = buildPeriodForm(today.getFullYear(), today.getMonth() + 1);
      vm.modalPeriodo = true;
    };

    vm.configPeriodo = function (periodo) {
      var source = periodo || (vm.control ? vm.control.periodo : null);
      if (!source) {
        return;
      }

      var noLaborados = vm.control && source.id === vm.control.periodo.id ? vm.control.diasNoLaborados : [];
      vm.periodoForm = {
        id: source.id,
        anio: source.anio,
        mes: source.mes,
        nombre: source.nombre,
        fechaInicio: toInputDate(source.fechaInicio),
        fechaFin: toInputDate(source.fechaFin),
        diasNoLaborados: (noLaborados || []).map(function (dia) {
          return { fecha: toInputDate(dia.fecha), motivo: dia.motivo || '' };
        })
      };
      vm.modalPeriodo = true;
    };

    vm.savePeriodo = function () {
      var payload = angular.copy(vm.periodoForm);
      payload.diasNoLaborados = (payload.diasNoLaborados || []).filter(function (dia) { return !!dia.fecha; });

      var action = payload.id ? nominaService.editarPeriodo(payload.id, payload) : nominaService.crearPeriodo(payload);
      action.then(function (periodo) {
        if (periodo && periodo.id) {
          vm.periodoId = periodo.id;
        }
        vm.modalPeriodo = false;
        vm.load();
      }).catch(showError);
    };

    vm.updatePeriodoDefaults = function () {
      if (!vm.periodoForm.anio || !vm.periodoForm.mes || vm.periodoForm.id) {
        return;
      }

      var defaults = buildPeriodForm(vm.periodoForm.anio, vm.periodoForm.mes);
      vm.periodoForm.fechaInicio = defaults.fechaInicio;
      vm.periodoForm.fechaFin = defaults.fechaFin;
      vm.periodoForm.nombre = defaults.nombre;
      vm.periodoForm.diasNoLaborados = [];
    };

    vm.addNoLaborado = function () {
      vm.periodoForm.diasNoLaborados = vm.periodoForm.diasNoLaborados || [];
      vm.periodoForm.diasNoLaborados.push({ fecha: vm.periodoForm.fechaInicio, motivo: 'No laborado' });
    };

    vm.removeNoLaborado = function (index) {
      vm.periodoForm.diasNoLaborados.splice(index, 1);
    };

    vm.openEmpleado = function (empleado) {
      vm.empleadoForm = empleado ? normalizeEmpleadoForm(angular.copy(empleado)) : buildEmpleadoDefaults();
      vm.modalEmpleado = true;
    };

    vm.saveEmpleado = function () {
      ensureEmpleadoNames();
      var action = vm.empleadoForm.id
        ? nominaService.editarEmpleado(vm.empleadoForm.id, vm.empleadoForm)
        : nominaService.crearEmpleado(vm.empleadoForm);

      action.then(function () {
        vm.modalEmpleado = false;
        vm.loadControl();
      }).catch(showError);
    };

    vm.toggleEmpleado = function (empleado) {
      Swal.fire({
        title: empleado.estado ? 'Inactivar empleado' : 'Activar empleado',
        text: 'Desea actualizar a ' + empleado.nombreCompleto + '?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Si, continuar',
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (result.isConfirmed) {
          nominaService.toggleEmpleado(empleado.id).then(vm.loadControl);
        }
      });
    };

    vm.getCell = function (empleadoId, fecha) {
      return vm.cells[empleadoId + '|' + normalizeDate(fecha)] || { estadoDia: 'No', valor: 0 };
    };

    vm.setQuickValue = function (empleado, dia, estadoDia) {
      var value = estadoDia === 'Trabajado' ? empleado.valorDiaBase : 0;
      vm.saveCell(empleado, dia, estadoDia, value);
    };

    vm.saveCell = function (empleado, dia, estadoDia, valor) {
      if (!vm.periodoId || vm.control.periodo.cerrado) {
        return;
      }

      var key = empleado.id + '|' + normalizeDate(dia.fecha);
      vm.saving[key] = true;
      nominaService.guardarRegistro(vm.periodoId, {
        empleadoId: empleado.id,
        fecha: dia.fecha,
        concepto: 'Dia',
        tipoNovedad: null,
        codigoNovedad: null,
        estadoDia: estadoDia,
        horas: null,
        porcentaje: null,
        baseCalculo: null,
        valor: parseMoney(valor),
        observacion: null
      }).then(function () {
        vm.saving[key] = false;
        vm.loadControl();
      }).catch(function (err) {
        vm.saving[key] = false;
        showError(err);
      });
    };

    vm.setDailyDate = function () {
      buildDailyRows();
      buildDailyNovedadForms();
    };

    vm.applyDailyWorked = function () {
      vm.dailyRows.forEach(function (row) {
        row.estadoDia = 'Trabajado';
        row.valor = row.empleado.valorDiaBase;
      });
    };

    vm.applyDailyNoWork = function () {
      vm.dailyRows.forEach(function (row) {
        row.estadoDia = 'No';
        row.valor = 0;
      });
    };

    vm.saveDailyControl = function () {
      if (!vm.periodoId || !vm.dailyDate || vm.control.periodo.cerrado) {
        return;
      }

      var requests = vm.dailyRows.map(function (row) {
        return nominaService.guardarRegistro(vm.periodoId, {
          empleadoId: row.empleado.id,
          fecha: vm.dailyDate,
          concepto: 'Dia',
          tipoNovedad: null,
          codigoNovedad: null,
          estadoDia: row.estadoDia,
          horas: null,
          porcentaje: null,
          baseCalculo: null,
          valor: parseMoney(row.valor),
          observacion: row.observacion || null
        });
      });

      $q.all(requests).then(function () {
        Swal.fire({ title: 'Guardado', text: 'Control diario guardado correctamente.', icon: 'success', timer: 1300, showConfirmButton: false, background: '#141417', color: '#f7f7f8' });
        vm.loadControl();
      }).catch(showError);
    };

    vm.saveNovedad = function () {
      if (!vm.control || vm.control.periodo.cerrado) {
        return;
      }

      var concepto = vm.selectedConceptoNovedad();
      nominaService.guardarRegistro(vm.periodoId, {
        empleadoId: vm.novedadForm.empleadoId,
        fecha: vm.novedadForm.fecha,
        fechaFin: vm.novedadForm.fechaFin || null,
        concepto: concepto ? concepto.nombre : vm.novedadForm.codigoNovedad,
        tipoNovedad: vm.novedadForm.tipoNovedad,
        codigoNovedad: vm.novedadForm.codigoNovedad,
        estadoDia: 'Novedad',
        horas: vm.novedadForm.horas || null,
        porcentaje: vm.novedadForm.porcentaje || null,
        baseCalculo: parseOptionalMoney(vm.novedadForm.baseCalculo),
        valor: parseMoney(vm.novedadForm.valor),
        observacion: vm.novedadForm.observacion
      }).then(function () {
        vm.loadControl();
      }).catch(showError);
    };

    vm.toggleDailyNovedades = function (empleadoId) {
      vm.dailyNovedadOpen[empleadoId] = !vm.dailyNovedadOpen[empleadoId];
    };

    vm.openDailyNovedad = function (row) {
      vm.dailyNovedadRow = row;
      vm.dailyNovedadForms[row.empleado.id] = buildDailyNovedadForm();
      vm.modalDailyNovedad = true;
    };

    vm.closeDailyNovedad = function () {
      vm.modalDailyNovedad = false;
      vm.dailyNovedadRow = null;
    };

    vm.isDailyNovedadesOpen = function (empleadoId) {
      return !!vm.dailyNovedadOpen[empleadoId];
    };

    vm.dailyNovedades = function (empleadoId) {
      if (!vm.control || !vm.dailyDate) {
        return [];
      }

      return (vm.control.registros || []).filter(function (registro) {
        return registro.empleadoId === empleadoId &&
          registro.concepto !== 'Dia' &&
          normalizeDate(registro.fecha) === normalizeDate(vm.dailyDate);
      });
    };

    vm.saveDailyNovedad = function () {
      var row = vm.dailyNovedadRow;
      if (!row) {
        return;
      }

      if (!vm.control || vm.control.periodo.cerrado) {
        return;
      }

      var form = vm.dailyNovedadForms[row.empleado.id] || buildDailyNovedadForm();
      var concepto = vm.conceptosNovedad[form.tipoNovedad].filter(function (item) {
        return item.codigo === form.codigoNovedad;
      })[0];

      nominaService.guardarRegistro(vm.periodoId, {
        empleadoId: row.empleado.id,
        fecha: vm.dailyDate,
        fechaFin: null,
        concepto: concepto ? concepto.nombre : form.codigoNovedad,
        tipoNovedad: form.tipoNovedad,
        codigoNovedad: form.codigoNovedad,
        estadoDia: 'Novedad',
        horas: form.horas || null,
        porcentaje: form.porcentaje || null,
        baseCalculo: parseOptionalMoney(form.baseCalculo),
        valor: parseMoney(form.valor),
        observacion: form.observacion || null
      }).then(function () {
        vm.dailyNovedadForms[row.empleado.id] = buildDailyNovedadForm();
        vm.dailyNovedadOpen[row.empleado.id] = true;
        vm.closeDailyNovedad();
        vm.loadControl();
      }).catch(showError);
    };

    vm.deleteRegistro = function (registro) {
      Swal.fire({
        title: 'Eliminar novedad',
        text: 'Desea eliminar este registro de nomina?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (result.isConfirmed) {
          nominaService.eliminarRegistro(vm.periodoId, registro.id).then(vm.loadControl).catch(showError);
        }
      });
    };

    vm.cerrarPeriodo = function () {
      Swal.fire({
        title: 'Cerrar periodo',
        text: 'Despues de cerrar no se podran editar registros de este periodo.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef233c',
        cancelButtonColor: '#2b2b33',
        confirmButtonText: 'Cerrar periodo',
        cancelButtonText: 'Cancelar',
        background: '#141417',
        color: '#f7f7f8'
      }).then(function (result) {
        if (result.isConfirmed) {
          nominaService.cerrarPeriodo(vm.periodoId).then(vm.loadControl).catch(showError);
        }
      });
    };

    vm.totalDia = function (dia) {
      if (!vm.control) {
        return 0;
      }

      return vm.control.empleados.reduce(function (total, empleado) {
        return total + (vm.getCell(empleado.id, dia.fecha).valor || 0);
      }, 0);
    };

    vm.totalEmpleado = function (empleadoId) {
      if (!vm.control) {
        return 0;
      }

      var row = (vm.control.totalesEmpleado || []).filter(function (item) { return item.empleadoId === empleadoId; })[0];
      return row ? row.total : 0;
    };

    vm.registrosDia = function () {
      return vm.control ? (vm.control.registros || []).filter(function (x) { return x.concepto === 'Dia'; }) : [];
    };

    vm.novedades = function () {
      return vm.control ? (vm.control.registros || []).filter(function (x) { return x.concepto !== 'Dia'; }) : [];
    };

    vm.conceptosDisponibles = function () {
      return vm.conceptosNovedad[vm.novedadForm.tipoNovedad] || [];
    };

    vm.selectedConceptoNovedad = function () {
      return vm.conceptosDisponibles().filter(function (x) { return x.codigo === vm.novedadForm.codigoNovedad; })[0];
    };

    vm.onTipoNovedadChange = function () {
      var first = vm.conceptosDisponibles()[0];
      vm.novedadForm.codigoNovedad = first ? first.codigo : null;
    };

    vm.onDailyTipoNovedadChange = function (empleadoId) {
      var form = vm.dailyNovedadForms[empleadoId];
      var list = vm.conceptosNovedad[form.tipoNovedad] || [];
      form.codigoNovedad = list.length ? list[0].codigo : null;
    };

    vm.municipiosDisponibles = function () {
      return vm.municipiosPorDepartamento[vm.empleadoForm.departamento] || [];
    };

    vm.onPaisChange = function () {
      if (vm.empleadoForm.pais && vm.empleadoForm.pais.indexOf('CO') === 0) {
        vm.empleadoForm.pais = 'CO';
      }
    };

    vm.onDepartamentoChange = function () {
      var municipios = vm.municipiosDisponibles();
      if (municipios.length && municipios.indexOf(vm.empleadoForm.municipio) === -1) {
        vm.empleadoForm.municipio = '';
      }
    };

    vm.empleadoNombre = function (id) {
      var source = vm.control ? (vm.control.todosEmpleados || vm.control.empleados || []) : [];
      var empleado = source.filter(function (x) { return x.id === id; })[0];
      return empleado ? empleado.nombreCompleto : 'Empleado';
    };

    vm.totalNovedades = function () {
      return vm.novedades().reduce(function (sum, item) { return sum + signedValue(item); }, 0);
    };

    vm.totalDevengos = function () {
      return vm.novedades().filter(function (item) { return item.tipoNovedad === 'Devengo'; }).reduce(function (sum, item) { return sum + (item.valor || 0); }, 0);
    };

    vm.totalDeducciones = function () {
      return vm.novedades().filter(function (item) { return item.tipoNovedad === 'Deduccion'; }).reduce(function (sum, item) { return sum + (item.valor || 0); }, 0);
    };

    vm.signedValue = function (item) {
      return signedValue(item);
    };

    vm.totalDiasTrabajo = function () {
      return vm.registrosDia().filter(function (x) { return x.estadoDia === 'Trabajado'; }).length;
    };

    vm.resumenEmpleados = function () {
      if (!vm.control) {
        return [];
      }

      var registrados = {};
      (vm.control.registros || []).forEach(function (registro) {
        registrados[registro.empleadoId] = true;
      });

      return (vm.control.todosEmpleados || vm.control.empleados || []).filter(function (empleado) {
        return !!registrados[empleado.id];
      });
    };

    vm.controlGridEmpleados = function () {
      if (!vm.control) {
        return [];
      }

      var registrados = {};
      (vm.control.registros || []).forEach(function (registro) {
        registrados[registro.empleadoId] = true;
      });

      return (vm.control.todosEmpleados || vm.control.empleados || []).filter(function (empleado) {
        return !!registrados[empleado.id];
      });
    };

    vm.controlCell = function (empleadoId, fecha) {
      if (!vm.control) {
        return { texto: '', valor: 0, estado: 'empty' };
      }

      var registros = (vm.control.registros || []).filter(function (registro) {
        return registro.empleadoId === empleadoId && normalizeDate(registro.fecha) === normalizeDate(fecha);
      });

      var dia = registros.filter(function (registro) { return registro.concepto === 'Dia'; })[0];
      var novedades = registros.filter(function (registro) { return registro.concepto !== 'Dia'; });
      var total = registros.reduce(function (sum, registro) { return sum + signedValue(registro); }, 0);

      if (total > 0) {
        return { texto: vm.formatMoney(total), valor: total, estado: 'value' };
      }

      if (dia) {
        return { texto: dia.estadoDia || 'NO', valor: 0, estado: 'off' };
      }

      if (novedades.length) {
        return { texto: vm.formatMoney(total), valor: total, estado: total < 0 ? 'deduction' : 'value' };
      }

      return { texto: '', valor: 0, estado: 'empty' };
    };

    vm.totalNovedadesDia = function (fecha) {
      if (!vm.control) {
        return 0;
      }

      return (vm.control.registros || []).filter(function (registro) {
        return registro.concepto !== 'Dia' && normalizeDate(registro.fecha) === normalizeDate(fecha);
      }).reduce(function (sum, registro) {
        return sum + signedValue(registro);
      }, 0);
    };

    vm.novedadesDiaDetalle = function (fecha) {
      if (!vm.control) {
        return '';
      }

      return novedadesDia(fecha).map(function (registro) {
        var detalle = buildNovedadDetalle(registro);
        var empleado = vm.empleadoNombre(registro.empleadoId);
        var parts = [
          empleado,
          registro.tipoNovedad || 'Novedad',
          registro.concepto || registro.codigoNovedad,
          detalle,
          registro.observacion
        ].filter(function (value) { return !!value; });

        return parts.join(' - ') + ': ' + vm.formatMoney(signedValue(registro));
      }).join('\n');
    };

    vm.novedadesDiaTooltip = function (fecha) {
      return novedadesDia(fecha).map(function (registro) {
        var tipo = registro.tipoNovedad || 'Novedad';
        var concepto = registro.concepto || registro.codigoNovedad || 'Sin concepto';
        return {
          empleado: empleado,
          titulo: tipo + ' - ' + concepto,
          detalle: buildNovedadDetalle(registro) || 'Sin detalle adicional',
          observacion: registro.observacion,
          valor: signedValue(registro)
        };
      });
    };

    vm.totalControlDia = function (fecha) {
      if (!vm.control) {
        return 0;
      }

      return (vm.control.registros || []).filter(function (registro) {
        return normalizeDate(registro.fecha) === normalizeDate(fecha);
      }).reduce(function (sum, registro) {
        return sum + signedValue(registro);
      }, 0);
    };

    vm.exportControlExcel = function () {
      if (!vm.canExportControl || !vm.control || !vm.controlGridEmpleados().length) {
        return;
      }

      var html = buildControlExcelHtml();
      var blob = new Blob(['\ufeff', html], { type: 'application/vnd.ms-excel;charset=utf-8;' });
      var url = $window.URL.createObjectURL(blob);
      var link = $window.document.createElement('a');
      link.href = url;
      link.download = 'MRS_Drunk_Control_Diario_' + normalizeFileName(vm.control.periodo.nombre) + '.xls';
      $window.document.body.appendChild(link);
      link.click();
      $window.document.body.removeChild(link);
      $window.URL.revokeObjectURL(url);
    };

    vm.controlEmpleadoTree = function () {
      if (!vm.control) {
        return [];
      }

      return (vm.control.todosEmpleados || vm.control.empleados || []).map(function (empleado) {
        var registros = (vm.control.registros || []).filter(function (registro) {
          return registro.empleadoId === empleado.id;
        });

        var devengados = registros.filter(function (registro) {
          return registro.concepto === 'Dia' ? (registro.valor || 0) > 0 : registro.tipoNovedad === 'Devengo';
        }).map(buildDetalleNomina);

        var deducciones = registros.filter(function (registro) {
          return registro.tipoNovedad === 'Deduccion';
        }).map(buildDetalleNomina);

        var totalDevengados = devengados.reduce(function (sum, item) { return sum + (item.valor || 0); }, 0);
        var totalDeducciones = deducciones.reduce(function (sum, item) { return sum + (item.valor || 0); }, 0);

        return {
          empleado: empleado,
          devengados: devengados,
          deducciones: deducciones,
          totalDevengados: totalDevengados,
          totalDeducciones: totalDeducciones,
          total: totalDevengados - totalDeducciones
        };
      }).filter(function (item) {
        return item.devengados.length || item.deducciones.length;
      });
    };

    vm.toggleTree = function (group, key) {
      if (group === 'period') {
        vm.treeOpen.period = !vm.treeOpen.period;
        return;
      }

      vm.treeOpen[group] = vm.treeOpen[group] || {};
      vm.treeOpen[group][key] = !vm.isTreeOpen(group, key);
    };

    vm.isTreeOpen = function (group, key) {
      if (group === 'period') {
        return vm.treeOpen.period;
      }

      vm.treeOpen[group] = vm.treeOpen[group] || {};
      if (vm.treeOpen[group][key] === undefined) {
        vm.treeOpen[group][key] = true;
      }

      return vm.treeOpen[group][key];
    };

    vm.empleadoMeta = function (empleado) {
      if (!empleado) {
        return '';
      }

      var cargo = empleado.cargo || 'Empleado';
      var documento = [empleado.tipoDocumento, empleado.numeroDocumento || empleado.documento]
        .filter(function (value) { return !!value; })
        .join(' ');

      return documento ? cargo + ' - ' + documento : cargo;
    };

    vm.formatMoney = function (value) {
      return $filter('currency')(value || 0, '$', 0);
    };

    function buildCells() {
      vm.cells = {};
      (vm.control.registros || []).filter(function (registro) {
        return registro.concepto === 'Dia';
      }).forEach(function (registro) {
        vm.cells[registro.empleadoId + '|' + normalizeDate(registro.fecha)] = registro;
      });

      (vm.control.empleados || []).forEach(function (empleado) {
        (vm.control.dias || []).forEach(function (dia) {
          var key = empleado.id + '|' + normalizeDate(dia.fecha);
          if (!vm.cells[key]) {
            vm.cells[key] = {
              id: 0,
              empleadoId: empleado.id,
              fecha: dia.fecha,
        concepto: 'Dia',
        tipoNovedad: null,
        codigoNovedad: null,
        estadoDia: dia.esNoLaborado ? 'No laborado' : 'No',
        horas: null,
        porcentaje: null,
        baseCalculo: null,
        valor: 0,
        observacion: null
            };
          }
        });
      });
    }

    function buildDailyRows() {
      if (!vm.control) {
        vm.dailyRows = [];
        return;
      }

      if (!vm.dailyDate) {
        vm.dailyDate = toInputDate(vm.control.periodo.fechaInicio);
      }

      var dayInfo = (vm.control.dias || []).filter(function (dia) {
        return normalizeDate(dia.fecha) === normalizeDate(vm.dailyDate);
      })[0];

      vm.dailyRows = (vm.control.empleados || []).map(function (empleado) {
        var cell = vm.getCell(empleado.id, vm.dailyDate);
        return {
          empleado: empleado,
          estadoDia: cell.estadoDia || (dayInfo && dayInfo.esNoLaborado ? 'No laborado' : 'No'),
          valor: cell.valor || 0,
          observacion: cell.observacion || '',
          esNoLaborado: !!(dayInfo && dayInfo.esNoLaborado),
          motivoNoLaborado: dayInfo ? dayInfo.motivoNoLaborado : null
        };
      });
    }

    function buildDailyNovedadForms() {
      vm.dailyNovedadForms = vm.dailyNovedadForms || {};
      (vm.dailyRows || []).forEach(function (row) {
        if (!vm.dailyNovedadForms[row.empleado.id]) {
          vm.dailyNovedadForms[row.empleado.id] = buildDailyNovedadForm();
        }
      });
    }

    function buildDailyNovedadForm() {
      return {
        tipoNovedad: 'Devengo',
        codigoNovedad: 'Comision',
        horas: null,
        porcentaje: null,
        baseCalculo: null,
        valor: 0,
        observacion: ''
      };
    }

    function resolveTab() {
      var path = $location.path();
      if (path.indexOf('/nomina/registro-diario') === 0) {
        return 'registro';
      }
      if (path.indexOf('/nomina/control-diario') === 0) {
        return 'control';
      }
      if (path.indexOf('/nomina/novedades') === 0) {
        return 'novedades';
      }
      if (path.indexOf('/nomina/empleados') === 0) {
        return 'empleados';
      }
      if (path.indexOf('/nomina/periodos') === 0) {
        return 'periodos';
      }
      return 'resumen';
    }

    function resetNovedadForm() {
      vm.novedadForm = {
        empleadoId: vm.control && vm.control.empleados.length ? vm.control.empleados[0].id : null,
        fecha: vm.control ? toInputDate(vm.control.periodo.fechaInicio) : new Date(),
        fechaFin: null,
        tipoNovedad: 'Devengo',
        codigoNovedad: 'Comision',
        horas: null,
        porcentaje: null,
        baseCalculo: null,
        valor: 0,
        observacion: ''
      };
    }

    function normalizeDate(value) {
      if (!value) {
        return '';
      }
      if (angular.isDate(value)) {
        return dateToInput(value);
      }
      return String(value).substring(0, 10);
    }

    function toInputDate(value) {
      if (!value) {
        return null;
      }
      if (angular.isDate(value)) {
        return value;
      }
      var parts = normalizeDate(value).split('-');
      return new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
    }

    function buildPeriodForm(year, month) {
      var first = new Date(year, month - 1, 1);
      var last = new Date(year, month, 0);
      return { anio: year, mes: month, nombre: monthName(first), fechaInicio: first, fechaFin: last, diasNoLaborados: [] };
    }

    function buildEmpleadoDefaults() {
      return {
        tipoDocumento: 'CC',
        numeroDocumento: '',
        primerNombre: '',
        segundoNombre: '',
        primerApellido: '',
        segundoApellido: '',
        nombreCompleto: '',
        documento: '',
        pais: 'CO',
        cargo: '',
        fechaIngreso: new Date(),
        tipoContrato: 'Termino indefinido',
        tipoTrabajador: 'Dependiente',
        subtipoCotizante: 'No aplica',
        tipoSalario: 'Ordinario',
        salarioIntegral: false,
        salarioBase: 0,
        valorDiaBase: 60000,
        periodicidadPago: 'Mensual',
        jornadaLaboral: 'Tiempo completo',
        nivelRiesgoArl: 'I',
        estado: true
      };
    }

    function normalizeEmpleadoForm(empleado) {
      var form = angular.extend(buildEmpleadoDefaults(), empleado);
      form.fechaExpedicionDocumento = toInputDate(form.fechaExpedicionDocumento);
      form.fechaNacimiento = toInputDate(form.fechaNacimiento);
      form.fechaIngreso = toInputDate(form.fechaIngreso);
      form.fechaRetiro = toInputDate(form.fechaRetiro);
      if (!form.numeroDocumento && form.documento) {
        form.numeroDocumento = form.documento;
      }
      return form;
    }

    function ensureEmpleadoNames() {
      var parts = [
        vm.empleadoForm.primerNombre,
        vm.empleadoForm.segundoNombre,
        vm.empleadoForm.primerApellido,
        vm.empleadoForm.segundoApellido
      ].filter(function (value) { return !!value; });

      if (!vm.empleadoForm.nombreCompleto) {
        vm.empleadoForm.nombreCompleto = parts.join(' ');
      }
      if (!vm.empleadoForm.documento) {
        vm.empleadoForm.documento = vm.empleadoForm.numeroDocumento;
      }
    }

    function dateToInput(date) {
      var month = String(date.getMonth() + 1).padStart(2, '0');
      var day = String(date.getDate()).padStart(2, '0');
      return date.getFullYear() + '-' + month + '-' + day;
    }

    function monthName(date) {
      var names = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
      return names[date.getMonth()] + ' ' + date.getFullYear();
    }

    function parseMoney(value) {
      if (value === null || value === undefined || value === '') {
        return 0;
      }
      return Number(String(value).replace(/\./g, '').replace(',', '.')) || 0;
    }

    function parseOptionalMoney(value) {
      if (value === null || value === undefined || value === '') {
        return null;
      }
      return parseMoney(value);
    }

    function signedValue(item) {
      return item && item.tipoNovedad === 'Deduccion' ? -(item.valor || 0) : (item.valor || 0);
    }

    function buildDetalleNomina(registro) {
      var detalle = [];
      if (registro.estadoDia && registro.concepto === 'Dia') {
        detalle.push(registro.estadoDia);
      }
      if (registro.horas) {
        detalle.push(registro.horas + ' horas');
      }
      if (registro.porcentaje) {
        detalle.push(registro.porcentaje + '%');
      }
      if (registro.baseCalculo) {
        detalle.push('Base ' + vm.formatMoney(registro.baseCalculo));
      }
      if (registro.observacion) {
        detalle.push(registro.observacion);
      }

      return {
        fecha: registro.fecha,
        fechaFin: registro.fechaFin,
        concepto: registro.concepto === 'Dia' ? 'Dia trabajado' : registro.concepto,
        detalle: detalle.join(' · '),
        valor: registro.valor || 0
      };
    }

    function buildControlExcelHtml() {
      var empleados = vm.controlGridEmpleados();
      var rows = [];

      rows.push('<html><head><meta charset="utf-8"></head><body>');
      rows.push('<table border="1">');
      rows.push('<tr><th colspan="' + (empleados.length + 4) + '">MRS Drunk - Control diario - ' + escapeHtml(vm.control.periodo.nombre) + '</th></tr>');
      rows.push('<tr><th>Fecha</th><th>Dia</th>');
      empleados.forEach(function (empleado) {
        rows.push('<th>' + escapeHtml(empleado.nombreCompleto) + '</th>');
      });
      rows.push('<th>Novedades</th><th>Total</th></tr>');

      (vm.control.dias || []).forEach(function (dia) {
        var novedadesDetalle = vm.novedadesDiaDetalle(dia.fecha);
        rows.push('<tr>');
        rows.push('<td>' + $filter('date')(dia.fecha, 'dd/MM/yyyy') + '</td>');
        rows.push('<td>' + escapeHtml(dia.dia || '') + '</td>');
        empleados.forEach(function (empleado) {
          var cell = vm.controlCell(empleado.id, dia.fecha);
          rows.push('<td>' + escapeHtml(cell.texto || '') + '</td>');
        });
        rows.push('<td title="' + escapeHtml(novedadesDetalle) + '">' + formatExcelMoney(vm.totalNovedadesDia(dia.fecha)) + excelDetalle(novedadesDetalle) + '</td>');
        rows.push('<td>' + formatExcelMoney(vm.totalControlDia(dia.fecha)) + '</td>');
        rows.push('</tr>');
      });

      rows.push('<tr><th>TOTAL</th><th></th>');
      empleados.forEach(function (empleado) {
        rows.push('<th>' + formatExcelMoney(vm.totalEmpleado(empleado.id)) + '</th>');
      });
      rows.push('<th>' + formatExcelMoney(vm.totalNovedades()) + '</th>');
      rows.push('<th>' + formatExcelMoney(vm.control.totalGeneral) + '</th></tr>');
      rows.push('</table></body></html>');

      return rows.join('');
    }

    function formatExcelMoney(value) {
      return value ? vm.formatMoney(value) : '';
    }

    function excelDetalle(value) {
      return value ? '<br><small>' + escapeHtml(value).replace(/\n/g, '<br>') + '</small>' : '';
    }

    function novedadesDia(fecha) {
      return (vm.control.registros || []).filter(function (registro) {
        return registro.concepto !== 'Dia' && normalizeDate(registro.fecha) === normalizeDate(fecha);
      });
    }

    function buildNovedadDetalle(registro) {
      var detalle = [];
      if (registro.horas) {
        detalle.push(registro.horas + ' horas');
      }
      if (registro.porcentaje) {
        detalle.push(registro.porcentaje + '%');
      }
      if (registro.baseCalculo) {
        detalle.push('Base ' + vm.formatMoney(registro.baseCalculo));
      }

      return detalle.join(' · ');
    }

    function escapeHtml(value) {
      return String(value || '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
    }

    function normalizeFileName(value) {
      return String(value || 'Periodo')
        .replace(/[^a-z0-9]+/gi, '_')
        .replace(/^_+|_+$/g, '');
    }

    function showError(err) {
      var message = err && err.data && err.data.message ? err.data.message : 'No fue posible completar la accion.';
      Swal.fire({ title: 'Atencion', text: message, icon: 'error', background: '#141417', color: '#f7f7f8' });
    }

    vm.load();
  });
})();
