# MRS Drunk

Aplicacion administrativa modular para bares, construida con frontend AngularJS 1.x, backend ASP.NET Core Web API .NET 8, JWT y SQL Server. Esta primera base incluye login, dashboard, usuarios, roles, permisos, empresas y menu dinamico multiempresa.

## Estructura

```text
backend/MRSDrunk.Api/      Web API REST en C#
database/                  Scripts SQL Server de esquema y seed
frontend/                  SPA AngularJS 1.x
docs/                      Plan tecnico y arquitectura
```

## Ejecucion local

1. Crear la base de datos en SQL Server ejecutando:
   - `database/001_create_schema.sql`
   - `database/002_seed_initial_data.sql`
2. Revisar la cadena `ConnectionStrings:DefaultConnection` en `backend/MRSDrunk.Api/appsettings.json`.
3. Levantar backend:
   ```powershell
   dotnet run --project backend/MRSDrunk.Api/MRSDrunk.Api.csproj --launch-profile https
   ```
4. Levantar frontend estatico desde `frontend/`:
   ```powershell
   python -m http.server 5500
   ```
5. Abrir `http://localhost:5500`.

## Usuario demo

- Usuario: `admin`
- Correo: `admin@mrsdrunk.com`
- Contrasena: `Admin123*`
- Rol: `SuperUsuario`

## Seguridad

- El login usa `POST /api/auth/login`.
- El token JWT incluye `UsuarioId`, `EmpresaId`, `RolId`, `NombreUsuario` y `NombreRol`.
- El frontend envia `Authorization: Bearer <token>` con un interceptor.
- El backend valida permisos con atributos como `[RequirePermission("Seguridad.Usuarios.Crear")]`.
- Las contrasenas se guardan con hash BCrypt.

## Crecimiento por modulos

Para agregar inventario, nomina, gastos fijos u otros modulos:

1. Crear registros en `Modulos`, `Ventanas`, `Permisos` y `RolPermisos`.
2. Agregar controladores REST protegidos por permisos.
3. Agregar servicios y vistas AngularJS.
4. El menu aparecera automaticamente cuando el rol tenga `PuedeVer` en la ventana.
