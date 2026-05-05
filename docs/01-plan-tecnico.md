# MRS Drunk - Plan tecnico inicial

## Objetivo
Construir la base comercializable de MRS Drunk como plataforma multiempresa para bares. La primera entrega cubre autenticacion JWT, dashboard, usuarios, roles, permisos, empresas y menu dinamico. Inventario, nomina, gastos fijos, ventas, compras y reportes quedan previstos como modulos futuros.

## Arquitectura general
- Frontend: AngularJS 1.x como SPA estatica, responsive y separada del backend.
- Backend: ASP.NET Core Web API con controladores REST, JWT, servicios de dominio y EF Core.
- Base de datos: SQL Server con esquema multiempresa y eliminacion logica por `Estado`.
- Seguridad: JWT en cada API protegida, hash BCrypt para contrasenas y validacion de permisos en backend.

## Carpetas
```text
MRS_Proyect/
  backend/
    MRSDrunk.Api/
      Configuration/     Opciones tipadas como JwtSettings
      Controllers/       APIs REST
      Data/              DbContext y seeder inicial
      DTOs/              Contratos de entrada/salida
      Helpers/           Utilidades de claims y respuestas
      Middleware/        Filtro/atributo de permisos
      Models/            Entidades EF Core
      Services/          Logica de autenticacion, permisos y menu
  database/
    001_create_schema.sql
    002_seed_initial_data.sql
  frontend/
    index.html
    app/
      controllers/
      services/
      routes/
      views/
      assets/
      styles/
```

## Modelo de datos inicial
- `Empresas`: tenant comercial.
- `Sucursales`: puntos de venta por empresa.
- `Usuarios`: credenciales, empresa, sucursal y rol.
- `Roles`: globales o por empresa; `EsSuperUsuario` omite restricciones.
- `Modulos`: agrupadores de menu y permisos.
- `Ventanas`: rutas funcionales por modulo.
- `Permisos`: catalogo flexible por codigo.
- `RolPermisos`: acciones permitidas por rol y ventana.
- `RefreshTokens`: preparado para renovacion de sesiones.

## Orden de implementacion
1. Crear scripts SQL y seed demo.
2. Crear backend: entidades, DbContext, JWT, permisos, APIs REST.
3. Crear frontend AngularJS: rutas, interceptor JWT, login, layout, menu dinamico, vistas CRUD.
4. Verificar compilacion backend y consistencia de archivos frontend.
5. Documentar ejecucion local y reglas para crecer por modulos.

## Modulos futuros
Cada modulo futuro debe agregar registros en `Modulos`, `Ventanas`, `Permisos` y `RolPermisos`, luego exponer APIs con `[RequirePermission("Modulo.Ventana.Accion")]` y vistas AngularJS con permisos de UI.
