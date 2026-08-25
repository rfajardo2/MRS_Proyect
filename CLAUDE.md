# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

MRS Drunk is a modular administrative application for bars: a multi-tenant (multi-empresa) system covering auth, users/roles/permissions, sales operations (cuentas/mesas), cash register (caja), inventory, payroll (nomina), and PayU payment integration. Stack: ASP.NET Core Web API (.NET 8) backend, AngularJS 1.x static SPA frontend, SQL Server database with hand-written numbered SQL scripts (no EF migrations).

## Working rules

These rules govern how changes are made in this repository and take priority over default habits (e.g. proactive refactoring/cleanup):

- **Scope discipline.** No general refactors, no architecture changes, no renaming of existing classes/methods/endpoints/routes/public variables/contracts, and no removing existing code just because it looks redundant or improvable — unless explicitly authorized. Don't introduce new abstractions, services, helpers, DTOs, or layers if the case can be solved safely within the existing structure.
- **Dependencies.** Don't upgrade NuGet/npm packages or other dependencies unless explicitly requested.
- **Fix workflow for a specific case:** (1) analyze the affected flow, (2) identify the strictly necessary files, (3) before editing, briefly state the probable cause, the files involved, and the proposed change, (4) modify only those files, (5) preserve existing behavior outside the case's scope, (6) after implementing, summarize exactly what changed and how to test it. Pause and ask before proceeding when a change is sensitive or its impact is unclear.
- **Database changes** must be delivered as explicit SQL scripts following the existing `NNN_description.sql` convention (see Database section below) — do not introduce EF Core migrations unless explicitly requested.
- **Secrets.** Never print, document, or replicate secrets found in `appsettings.json`, connection strings, the JWT `SecretKey`, PayU keys, or DB credentials — and don't modify production config or credentials without explicit authorization.
- **Multi-tenant filtering, auth/session validation, and `RequirePermissionAttribute`** must always be preserved exactly as described in the Architecture section below — don't replace the existing permission system.
- **PayU changes**: verify the confirmation signature, preserve idempotency, never mark a payment approved without correct validation, avoid creating duplicates, and don't alter reconciliation logic without analyzing the full flow (see PayU section below).
- **Use `codebase-memory-mcp`** (search_graph/trace_path/get_code_snippet/query_graph) to locate references, dependencies, usages, and impact before modifying code, instead of broadly re-reading the whole repo when the index already has the answer.

## Commands

### Backend (from `backend/MRSDrunk.Api/`)
```powershell
dotnet restore
dotnet build
dotnet run --launch-profile https   # https://localhost:7271 ; http://localhost:5127
dotnet run --launch-profile http    # http://localhost:5127
```
There is no test project in this repo (`dotnet test` has nothing to run) and no lint config for either backend or frontend.

### Frontend (from `frontend/`)
Static site, no bundler/npm/build step — served as-is:
```powershell
python -m http.server 5500
```
Then open `http://localhost:5500`. The API base URL is hardcoded in `frontend/app/services/config.js` (`apiConfig.baseUrl`, currently `http://localhost:5127/api`) — update it there, not via env vars, if the backend port/host changes.

**Important:** the frontend has no module bundler. Every controller/service `.js` file must be added as an explicit `<script>` tag in `frontend/index.html` (in dependency order — services before the controllers that use them) or it silently won't load.

### Database
No EF Core migrations are used. Schema changes are plain numbered SQL scripts in `database/`, applied in order against SQL Server:
```
database/001_create_schema.sql
database/002_seed_initial_data.sql
... (run all NNN_*.sql files in ascending numeric order)
```
When adding a schema change, add a new `NNN_description.sql` file with the next number — do not edit past scripts. The connection string lives in `backend/MRSDrunk.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`).

Demo login: user `admin` / `admin@mrsdrunk.com`, password `Admin123*`, role `SuperUsuario`.

## Architecture

### Backend layering
`Controllers/` → `Services/` (business logic, interfaces `I*Service`) → `Data/MrsDrunkDbContext` (EF Core, one DbContext for all 30+ entities) → SQL Server. There is no repository layer (`Repositories/` is intentionally empty — see its README — controllers/services use `MrsDrunkDbContext` directly by design; don't introduce a repository abstraction unless a module's queries genuinely justify it). DTOs live in `DTOs/`, EF entities in `Models/`, typed options (`JwtSettings`, `PayUSettings`) in `Configuration/`.

### Auth and permissions
- JWT is issued by `AuthService.LoginAsync` and carries claims: `usuarioId`, `empresaId`, `sucursalId`, `rolId`, `nombreRol`, plus a `sessionId`.
- **Sessions are also tracked server-side** in `UsuarioSesiones`. `Program.cs` hooks `JwtBearerEvents.OnTokenValidated` to look up the session by `sessionId` and reject the request if it's inactive/expired — this is how server-side logout/session revocation works even though the JWT itself would still validate. Don't remove this check when touching JWT config.
- Fine-grained authorization uses `[RequirePermission("Modulo.Ventana.Accion")]` (see `Middleware/RequirePermissionAttribute.cs`), which calls `IPermissionService.HasPermissionAsync(usuarioId, rolId, codigo, ...)`. Permission codes follow the `Modulo.Ventana.Accion` convention (e.g. `Operacion.Cuentas.Editar`, `Operacion.PagosPayU.Conciliar`).
- `ClaimsPrincipalExtensions` (`Helpers/`) provides `User.GetEmpresaId()`, `GetUsuarioId()`, `GetSucursalId()`, `GetRolId()` — nearly every controller filters queries by `EmpresaId`/`SucursalId` from these claims for multi-tenant isolation. Always scope new queries the same way; there is no other tenant-isolation mechanism (no row-level security, no separate schemas).
- Passwords are hashed with BCrypt (`BCrypt.Net.BCrypt.Verify`/`HashPassword`).

### Adding a new module (established pattern, see `docs/01-plan-tecnico.md` and `docs/02-diseno-mrsdrunkv2-modulos.md`)
1. Add rows to `Modulos`, `Ventanas`, `Permisos`, `RolPermisos` via a new numbered SQL script in `database/`.
2. Add a controller with `[Authorize]` + `[RequirePermission("Modulo.Ventana.Accion")]` per endpoint.
3. Add an EF entity in `Models/` + `DbSet` + any fluent config in `MrsDrunkDbContext.OnModelCreating`, plus a service if there's real logic.
4. Add the AngularJS service + controller + view, register the route in `frontend/app/routes/routes.js`, and add the new `<script>` tags to `frontend/index.html`.
5. The sidebar menu is data-driven off `Modulos`/`Ventanas`/`RolPermisos` (`MenuService` on the backend, `layoutController`/`menuService` on the frontend) — a new window shows up automatically once the role has `PuedeVer` on it; no frontend menu hardcoding needed.

### Frontend structure
Classic AngularJS 1.x (`ng-app="mrsDrunkApp"`), no build tooling, loaded via CDN (`angular`, `angular-route`, SweetAlert2) plus local scripts. `routes.js` defines routes with `public: true` for unauthenticated pages (`/inicio`, `/login`, `/menu`, `/payment/result`) and gates everything else behind `authService.isAuthenticated()` in a `$routeChangeStart` handler. `httpInterceptor.js` attaches `Authorization: Bearer <token>` from `sessionStorage` to every request and redirects to `/inicio` on a 401.

### PayU payment integration
`PaymentsController` + `PaymentService`/`IPaymentService` (registered as a typed `HttpClient`) implement the checkout/webhook flow: `POST /api/payments/create` (starts a `PagoPasarela`), `GET /api/payments/{id}/checkout` (anonymous, renders the redirect form to PayU), `POST /api/payments/payu/confirmation` (anonymous webhook that PayU calls back — validates signature and writes to `PagoConfirmacionPayU`), and an admin surface (`admin`, `admin/{id}`, `admin/export`, `admin/executive*`, `admin/{id}/refresh-status|reprocess-approved|cancel-pending|reconcile-duplicates`) gated by `Operacion.PagosPayU.*` permissions for reconciliation/reporting (CSV and Excel export via ClosedXML). `PagoPasarela` links optionally to `CuentaPago` (the "mirrored" ledger payment) — reconciliation logic in `PaymentsController`/`PaymentService` exists specifically to detect and fix duplicate/orphaned `CuentaPago` rows created from PayU confirmations, so preserve that duplicate-detection logic when touching this flow.

### Multi-tenancy and operational model
Core entities: `Empresa` (tenant) → `Sucursal` (branch) → `Usuario`/`Rol`/`Permiso`/`RolPermiso`. Operational flow: a `DiaOperativo` is opened, meseros create `Cuenta` records (states: `Abierta`, `PendienteAprobacion`, `Cerrada`, `Rechazada`, `Anulada`) with `CuentaItem`s and `CuentaPago`s; `CajaTurno` tracks cash-register open/close and expected-vs-actual cash. Inventory (`InventarioStock`/`InventarioMovimiento`/`InventarioLote`/`InventarioCompra*`) can be decremented via `ProductoReceta` when a product has a recipe of insumos.
