(function () {
  'use strict';

  angular.module('mrsDrunkApp').controller('PermisosController', function ($q, rolesService, permisosService, authService) {
    var vm = this;
    vm.roles = [];
    vm.selectedRole = null;
    vm.grid = [];
    vm.message = null;
    vm.permissionTooltip = 'No tiene permisos para realizar esta accion';
    vm.canConfigure = authService.hasPermission('Seguridad.Permisos.Editar');

    function refreshPermissions() {
      return authService.loadPermissions().then(function () {
        vm.canConfigure = authService.hasPermission('Seguridad.Permisos.Editar');
      });
    }

    vm.loadRoles = function () {
      refreshPermissions().finally(function () {
        rolesService.list().then(function (roles) {
          vm.roles = roles;
          vm.selectedRole = vm.roles.length ? vm.roles[0].id : null;
          if (vm.selectedRole) {
            vm.loadGrid();
          }
        });
      });
    };

    vm.loadGrid = function () {
      $q.all([permisosService.windows(), permisosService.byRole(vm.selectedRole)])
        .then(function (responses) {
          var windows = responses[0];
          var assigned = responses[1];
          var assignedMap = {};

          assigned.forEach(function (item) {
            assignedMap[item.ventanaId + '-' + item.permisoId] = item;
          });

          vm.grid = [];
          windows.forEach(function (win) {
            var row = {
              modulo: win.modulo,
              ventana: win.nombre,
              ventanaId: win.id,
              ver: createEmptyPermission(win.id, 'Ver'),
              crear: createEmptyPermission(win.id, 'Crear'),
              consultar: createEmptyPermission(win.id, 'Consultar'),
              editar: createEmptyPermission(win.id, 'Editar'),
              eliminar: createEmptyPermission(win.id, 'Eliminar'),
              adicionales: []
            };

            assigned.filter(function (perm) {
              return perm.ventanaId === win.id;
            }).forEach(function (perm) {
              var saved = assignedMap[win.id + '-' + perm.permisoId] || {};
              var action = normalizeAction(perm.accion || getAction(perm.codigo));
              var model = {
                permisoId: perm.permisoId,
                ventanaId: win.id,
                nombre: perm.permiso,
                codigo: perm.codigo,
                action: action,
                puedeVer: !!saved.puedeVer,
                puedeCrear: !!saved.puedeCrear,
                puedeConsultar: !!saved.puedeConsultar,
                puedeEditar: !!saved.puedeEditar,
                puedeEliminar: !!saved.puedeEliminar
              };

              if (action === 'Ver') {
                row.ver = model;
              } else if (action === 'Crear') {
                row.crear = model;
              } else if (action === 'Consultar') {
                row.consultar = model;
              } else if (action === 'Editar') {
                row.editar = model;
              } else if (action === 'Eliminar') {
                row.eliminar = model;
              } else {
                row.adicionales.push(model);
              }
            });

            vm.grid.push(row);
          });
        });
    };

    vm.save = function () {
      if (!vm.canConfigure || !vm.selectedRole) {
        return;
      }

      permisosService.saveRole(vm.selectedRole, buildRequest()).then(function () {
        vm.message = 'Permisos guardados correctamente.';
        vm.loadGrid();
      });
    };

    function createEmptyPermission(ventanaId, action) {
      return {
        permisoId: 0,
        ventanaId: ventanaId,
        nombre: action,
        action: action,
        puedeVer: false,
        puedeCrear: false,
        puedeConsultar: false,
        puedeEditar: false,
        puedeEliminar: false,
        missing: true
      };
    }

    function getAction(code) {
      var parts = (code || '').split('.');
      return parts.length ? parts[parts.length - 1] : '';
    }

    function normalizeAction(action) {
      var normalized = (action || '').trim();
      if (normalized === 'Inactivar') {
        return 'Eliminar';
      }
      return normalized;
    }

    function toRequest(item, fieldName) {
      var request = {
        permisoId: item.permisoId,
        ventanaId: item.ventanaId,
        puedeVer: false,
        puedeCrear: false,
        puedeConsultar: false,
        puedeEditar: false,
        puedeEliminar: false
      };

      if (!item.permisoId) {
        return null;
      }

      request[fieldName] = !!item[fieldName];
      return request;
    }

    function buildRequest() {
      var result = [];
      vm.grid.forEach(function (row) {
        [
          { item: row.ver, field: 'puedeVer' },
          { item: row.crear, field: 'puedeCrear' },
          { item: row.consultar, field: 'puedeConsultar' },
          { item: row.editar, field: 'puedeEditar' },
          { item: row.eliminar, field: 'puedeEliminar' }
        ].forEach(function (entry) {
          var request = toRequest(entry.item, entry.field);
          if (request) {
            result.push(request);
          }
        });

        row.adicionales.forEach(function (item) {
          var request = toRequest(item, 'puedeVer');
          if (request) {
            result.push(request);
          }
        });
      });

      return result;
    }

    vm.loadRoles();
  });
})();
