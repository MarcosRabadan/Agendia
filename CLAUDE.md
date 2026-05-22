# Claude project notes — MRC.Agendia

Este fichero se carga **automáticamente** al inicio de cada sesión de Claude Code.
Lee con atención antes de tocar código.

> 📌 **Última actualización mayor: 2026-05-22.** Sesión en cadena: cerradas #57 (reset password + email confirmation), #99 y #100 (refactors SRP de Auth y Schedule), #101 (eliminar Shared), #104 (.editorconfig), #105 (PaginationConstants a Domain) y #102 (este pase de docs). Lee la sección **"Estado actual del proyecto"** para el snapshot.

## Qué es este proyecto

API REST de gestión de citas para negocios (peluquerías, clínicas, talleres…). Cubre alta de negocios, empleados, servicios, clientes y citas, con un sistema de horarios potente (plantillas anuales, festivos, vacaciones, overrides por día, turnos partidos).

- **Idioma del producto:** Español
- **Idioma de los commits:** Español neutro sin tildes (algunos terminales no las muestran bien).
- **Idioma de los comentarios en código:** **Inglés**. Convención del equipo.
- **Idioma de la documentación visible al usuario** (Swagger summaries de endpoints, mensajes de error de validación, exception messages): **Español**.

## Stack

- **.NET 9.0** + ASP.NET Core Web API
- **Clean Architecture + DDD** (capas: Api / Application / Domain / Infrastructure)
- **CQRS con MediatR** — cada caso de uso es un Command/Query + Handler
- **AutoMapper** — Entity ↔ DTO (con cuidado en updates, ver "Decisiones de diseño")
- **Entity Framework Core 8.0.11** + SQL Server (LocalDB en Dev)
- **ASP.NET Identity** + **JWT Bearer** + Refresh tokens con rotación
- **FluentValidation** con `ValidationBehavior` de MediatR (corre antes del handler)
- **Rate limiting** built-in de ASP.NET en endpoints de auth
- **Serilog + Seq** (`http://localhost:5341`) para logging
- **Swagger / OpenAPI** documentando todo
- **xUnit + NSubstitute + EF InMemory** en tests unitarios
- **xUnit + WebApplicationFactory<Program>** en tests integration

## Estado actual del proyecto (2026-05-22)

> Snapshot tras cerrar el repaso general. La aplicación está **funcional, testeada y razonablemente pulida** para un MVP de un solo dev.

### Lo que SÍ funciona end-to-end

- ✅ **Auth completa**: registro de Client (público), registro de Owner (público desde #82), registro de Employee (Owner crea sus empleados), login, refresh con rotación, logout, change password, `GET /me`. Lockout tras 5 fallos. Rate limit en `/api/auth/*`.
- ✅ **Reset password + confirmación de email** (#57): `forgot-password` (anti-enumeración, siempre 204), `reset-password`, `confirm-email`. El registro envía email de confirmación. Token de reset 1h, confirmación 24h (configurables). Flag `Auth:RequireConfirmedEmail` (default false) bloquea login sin confirmar. Email vía `IEmailSender` (SMTP genérico en prod, Logging en Dev/Test).
- ✅ **CRUD completo** de Business, Client, Employee, Service, Appointment, Holiday.
- ✅ **Sistema de horarios** (Schedule): templates anuales, overrides por día, festivos, vacaciones, turnos partidos.
- ✅ **Disponibilidad** (`GET /api/businesses/{id}/availability`): calcula huecos libres con capacidad por empleado.
- ✅ **Capacidad por empleado** (`Employee.MaxConcurrentAppointments`): clase grupal, peluquera con tinte, etc.
- ✅ **Validación de citas**: `AppointmentSchedulingValidator` chequea horario, conflictos, capacidad, etc.
- ✅ **Auto-creación de Employee al registrar Owner**: un autónomo opera al instante sin pasos extra.
- ✅ **`BusinessPublicDto`** para `GET /api/business` anónimo (sin email, filtra inactivos).

### Robustez

- ✅ **97/97 tests verdes** (1 API.Tests placeholder + 66 unit + 30 integration).
- ✅ **Resource-based authorization** en handlers via `IResourceAuthorizationService` con 11 métodos `EnsureCan*Async`.
- ✅ **Defensa en profundidad** contra cross-tenant: handlers validan + tests defensivos cubren los casos.
- ✅ **Auto-migrate en Development** (`Database.MigrateAsync()` al arrancar) — fresh clone funciona con un solo `dotnet run`.
- ✅ **Fail-fast en producción** si faltan `Jwt:Key` o `Cors:AllowedOrigins`.
- ✅ **UseForwardedHeaders** activo solo en producción (IP real tras proxy).

### Lo que NO está hecho todavía

- ❌ Tests unitarios de CRUD básico (Business, Client, Employee, Service, Appointment handlers). Cubiertos por integration solo en los casos de cross-tenant.
- ❌ Envío SMTP real verificado contra un relay (Mailtrap/SES). En Dev/Test el email se loguea; el envío real (`SmtpEmailSender`) está implementado pero sin probar contra un servidor.
- ❌ Sistema de notificaciones de citas (email + push para confirmación/recordatorio — issue #51). El `IEmailSender` de #57 es reutilizable para esto.
- ❌ Cosmético restante: comentarios ES→EN en el resto de archivos (#103).
- ❌ Cloud secret manager (aparcado, sin cloud decidido).

## Estructura de carpetas

```
src/
├── MRC.Agendia.Api/
│   ├── Configuration/         ← Módulos de wiring (LoggingSetup, AuthenticationSetup, RateLimitingSetup, SwaggerSetup, CorsSetup, EmailSetup, PipelineExtensions) + ShortLivedTokenProvider (token de reset 1h)
│   ├── Controllers/           ← Auth, Business, Client, Employee, Service, Appointment, Schedule, Holiday, Availability
│   ├── Middleware/            ← ExceptionHandlingMiddleware (mapea excepciones a JSON)
│   ├── Services/              ← CurrentUserContext, CurrentUserService
│   └── Program.cs             ← Orquesta ConfigureSerilog/AddInfrastructure/AddApplication/AddIdentityAndJwt + auto-migrate (Dev) + seed + pipeline
├── MRC.Agendia.Application/
│   ├── Auth/                  ← IAuthService (login/refresh/logout/changePw/forgotPw/resetPw/confirmEmail/getCurrent) + IUserRegistrationService (register Client/Owner/Employee) + Commands + DTOs + Validators
│   ├── Authorization/         ← ICurrentUserContext, IResourceAuthorizationService
│   ├── Availability/          ← Endpoint clave: huecos libres para reservar
│   ├── Appointments/          ← Incluye IAppointmentSchedulingValidator (reglas de cita válida)
│   ├── Behaviors/             ← ValidationBehavior (MediatR pipeline)
│   ├── Business/Clients/Employees/Services/Schedules/Holidays/   ← CRUD CQRS por feature
│   ├── Common/                ← PagedResult<T> + Email/IEmailSender (abstracción de envío de correo)
│   ├── Mappings/              ← Profiles de AutoMapper
│   └── DependencyInjection.cs ← AddApplication() — auto-discovery de MediatR/AutoMapper/FluentValidation + servicios
├── MRC.Agendia.Domain/
│   ├── Constants/             ← Roles.cs + RolePolicies.cs (combos para [Authorize(Roles=...)]) + PaginationConstants.cs
│   ├── Entities/              ← Business, Client, Employee (con MaxConcurrentAppointments), Service, Appointment, ScheduleTemplate, ScheduleOverride, HolidayCalendar...
│   ├── Enums/                 ← AppointmentStatus, ScheduleOverrideType, HolidayScope...
│   ├── Exceptions/            ← AuthenticationException (→ 401)
│   ├── Interfaces/            ← I*Repository
│   └── Services/              ← IScheduleResolver (domain service)
├── MRC.Agendia.Infrastructure/
│   ├── Authorization/         ← ResourceAuthorizationService (impl)
│   ├── Email/                 ← SmtpEmailSender (prod) + LoggingEmailSender (Dev/Test) — impl de IEmailSender
│   ├── Identity/              ← ApplicationUser, JwtTokenService, AuthService, UserRegistrationService, AuthEmailService, AuthResponseFactory, DbInitializer, RefreshTokenCleanupService (BackgroundService)
│   ├── Migrations/
│   ├── Repositories/
│   ├── Services/              ← ScheduleResolver (impl)
│   ├── AgendiaDbContext.cs    ← IdentityDbContext + DbSets
│   └── DependencyInjection.cs ← AddInfrastructure(config) — DbContext + repos + auth + cleanup hosted service
tests/
├── MRC.Agendia.API.Tests/        ← Placeholder UnitTest1.cs vacío. No tocar.
├── MRC.Agendia.Tests.Unit/       ← 66 tests con xUnit + NSubstitute + EF InMemory
│   ├── TestDoubles/              ← FakeCurrentUserContext
│   └── Infrastructure/
│       ├── Authorization/        ← ResourceAuthorizationServiceTests (54)
│       └── Services/             ← ScheduleResolverTests (12)
└── MRC.Agendia.Tests.Integration/← 30 tests con xUnit + WebApplicationFactory<Program> + InMemory
    ├── Infrastructure/           ← CustomWebApplicationFactory (InMemory + env vars) + FakeEmailSender + RequireConfirmedEmailWebApplicationFactory
    ├── Auth/                     ← AuthFlowIntegrationTests (9), OwnerRegistrationIntegrationTests (3), PasswordResetIntegrationTests (3), EmailConfirmationIntegrationTests (2), RequireConfirmedEmailIntegrationTests (1)
    ├── Business/                 ← BusinessPublicEndpointTests (4)
    ├── Employees/                ← EmployeeCrossTenantTests (3)
    ├── Services/                 ← ServiceCrossTenantTests (3)
    └── Schedules/                ← ScheduleCrossTenantTests (2)
scripts/
└── create-github-issues.sh       ← Backlog bulk creation
```

## Flujo de una petición

```
Request
   ↓
[Pipeline en Api/Configuration/PipelineExtensions.cs]
   ↓
1. ForwardedHeaders   (solo si Environment != Development && != Testing)
2. Swagger UI         (solo si Environment == Development)
3. HttpsRedirection   (skip en Testing)
4. CORS
5. RateLimiter        (skip en Testing)
6. ExceptionHandlingMiddleware
7. Authentication (JWT Bearer)
8. Authorization
9. MapControllers
   ↓
Controller (Api)
   ↓
MediatR.Send(Command/Query)
   ↓
ValidationBehavior (FluentValidation: si falla → ValidationException → 400 con errores estructurados)
   ↓
Handler
   ↓
IResourceAuthorizationService.EnsureCan*Async(...)   ← Resource-based auth si aplica (la mayoría de Update/Delete/GetById)
   ↓
Service (Application)
   ↓
IAppointmentSchedulingValidator (en CreateAppointment/UpdateAppointment, valida horario + capacidad)
   ↓
Repository (Infrastructure / EF Core)
   ↓
SQL Server
```

## Convenciones — IMPORTANTES, no las rompas

1. **Una clase por archivo.** Cada `class`, `record`, `enum`, `interface` en su propio `.cs`. Cumplido al 100%.
2. **Records inmutables** para todos los DTOs.
3. **Naming consistente:**
   - `*Command` + `*CommandHandler` + `*CommandValidator` (FluentValidation, opcional pero recomendado)
   - `*Query` + `*QueryHandler` + `*QueryValidator`
   - `*Repository` + `I*Repository`
   - `*Service` + `I*Service`
   - `*Dto`, `Create*Dto`, `Update*Dto`. Para DTOs públicos (sin auth): `*PublicDto` (ej. `BusinessPublicDto`).
4. **`async`/`await`** en todos los accesos a BD. Nunca `.Result` ni `.Wait()`.
5. **Validar autorización en handlers**, no en controllers. Inyecta `IResourceAuthorizationService` y llama al `EnsureCan*Async` correspondiente ANTES de delegar al servicio.
   - **Excepción:** para listados Admin-only (`[Authorize(Roles = Roles.Admin)]`), el handler puede delegar directo al service sin auth extra.
6. **Validar inputs con FluentValidation.** Cada Command/Query merece un Validator. El `ValidationBehavior` ya está registrado, basta con crear el archivo siguiendo el naming convention.
7. **Comentarios en código en inglés.** Mensajes visibles al usuario o al dev en logs/exceptions en español.
8. **Combinaciones de roles → `RolePolicies`**, no concatenar strings. Existen `RolePolicies.AdminOrOwner`, `RolePolicies.Staff` (Admin+Owner+Employee), `RolePolicies.AdminOrSelfClient`. Si necesitas un combo nuevo, añádelo a `RolePolicies.cs`.
9. **DTOs de Update**: NO incluir `BusinessId` si el recurso pertenece a un Business. AutoMapper de `UpdateXDto → X` debe usar `.ForMember(BusinessId, opt => opt.Ignore())` para evitar cross-tenant takeover (ver #91/#92 como referencia). Para máxima seguridad: en `ScheduleService` se hace asignación campo a campo (sin AutoMapper) en Update — patrón más blindado, considerar para nuevos services.
10. **Migraciones EF**: cada cambio de modelo va con su migración. Comprueba que `dotnet ef migrations add` no genera warnings inesperados. En Development las migraciones se aplican **automáticamente** al arrancar (issue #85, PR #86).
11. **Sin secretos en `appsettings.json`.** Usa `dotnet user-secrets` en dev y variables de entorno en producción. En Dev el connection string apunta a `(localdb)\MSSQLLocalDB` por defecto (#97).
12. **No mergees a master.** Hay branch protection. Solo el humano.
13. **Routing — 3 patrones (NO se refactorizan, ver #102).** El proyecto convive con 3 patrones de ruta. Al crear un controller nuevo, elige según el tipo de recurso. La capitalización del archivo es la que sale en Swagger UI y en logs.

    | Patrón | Atributo | Ejemplos actuales | Cuándo usarlo |
    |---|---|---|---|
    | **Top-level singular** | `[Route("api/[controller]")]` | `/api/Business`, `/api/Client`, `/api/Service`, `/api/Employee`, `/api/Appointment`, `/api/Holiday` | Recurso de primer nivel. Heredado de `dotnet new webapi`: `[controller]` se expande al nombre del controller en PascalCase. Para un recurso nuevo (ej. `Invoice`, `Notification`) → `[Route("api/[controller]")]`. |
    | **Sub-recurso anidado plural** | `[Route("api/businesses/{businessId}/<recurso>")]` | `ScheduleController` (`/api/businesses/{id}/schedules`), `AvailabilityController` (`/api/businesses/{id}/availability`) | El recurso **pertenece a** un Business y no tiene sentido sin él. La URL refleja la jerarquía REST. Para un sub-recurso nuevo (ej. sucursales) → `[Route("api/businesses/{businessId}/branches")]`. |
    | **Operación / agrupador lowercase** | `[Route("api/<nombre>")]` literal en minúscula | `AuthController` (`/api/auth/login`, `/api/auth/register/owner`...) | No es un recurso CRUD sino un agrupador de operaciones. Para un grupo nuevo (ej. pagos) → `[Route("api/payment")]`. |

    - **NO pluralizar los top-level** (`/api/Clients`): el frontend móvil ya está acoplado a `/api/Client`, `/api/Service`, etc. Cambiarlo requiere versionado + deprecation. Lo mismo para kebab/snake_case.
    - ASP.NET routing es **case-insensitive**: `/api/client` y `/api/Client` resuelven al mismo endpoint. La convención del archivo solo afecta a lo que se muestra en Swagger/logs.

## Decisiones de diseño tomadas (no las cuestiones sin razón fuerte)

### Auth

- **No usamos `Microsoft.AspNetCore.Identity.SignInManager`** (vive en el shared framework). La validación de password usa `UserManager.CheckPasswordAsync` directamente.
- **`ApplicationUser` vive en Infrastructure**, no en Domain. Domain solo tiene `string UserId` en Business/Employee/Client.
- **Authorization basada en recursos** vía `IResourceAuthorizationService` (interfaz en Application, impl en Infrastructure). No usamos `IAuthorizationHandler<TRequirement>` de ASP.NET — lanzamos `UnauthorizedAccessException` que el middleware mapea a 403.
- **Excepciones tipadas en auth**: `AuthenticationException` (en `Domain.Exceptions/`) → middleware → **401** `UNAUTHENTICATED`. La usa el `AuthService` para credenciales malas, cuenta bloqueada, refresh token revocado, etc. Distinta de `UnauthorizedAccessException` → **403** `FORBIDDEN` (autorización de recursos). Patrón establecido en PR #80.
- **Refresh tokens con rotación**: cada uso genera uno nuevo y revoca el anterior.
- **`RefreshTokenCleanupService`** (BackgroundService) limpia tokens expirados cada 24h.
- **Auto-creación de Employee al registrar Owner**: cuando alguien hace `POST /api/auth/register/owner`, se crea User + Business + Employee del owner. Un autónomo está listo para recibir reservas sin pasos extra (lógica del PR #72). Ahora vive en `UserRegistrationService`, no en `AuthService`.
- **Autoregistro de Owner es público** (PR #82). Mismo modelo que `register/client`. Rate-limited con `auth-register` (3/h/IP).
- **SRP de Auth (#99):** `AuthService` se quedó con credenciales/cuenta (login, refresh, logout, change/forgot/reset password, confirm email, getCurrent). El registro se extrajo a **`IUserRegistrationService`**; la construcción+persistencia de tokens a **`IAuthResponseFactory`** (compartido por registro/login/refresh, sin duplicar); la composición/envío de correos a **`IAuthEmailService`**. Las 3 impls están en `Infrastructure/Identity`.
- **Reset password + confirmación de email (#57):**
  - `POST /api/auth/forgot-password` → **anti-enumeración**: responde siempre 204, solo envía email si la cuenta existe y está activa.
  - `POST /api/auth/reset-password` (email + token + nueva password). Un reset correcto limpia el lockout. Errores de token → mensaje uniforme; errores de política de password → se propagan.
  - `POST /api/auth/confirm-email` (userId + token). El registro de Client/Owner/Employee envía email de confirmación.
  - **Tokens auto-contenidos de Identity** (data protection), no se persisten → sin migración. Reset usa un **token provider dedicado de 1h** (`ShortLivedTokenProvider`); confirmación usa el provider por defecto a 24h. Configurables: `Auth:PasswordResetTokenHours`, `Auth:EmailConfirmationTokenHours`.
  - **`Auth:RequireConfirmedEmail`** (default `false` para no romper flujos/tests): si `true`, `LoginAsync` bloquea usuarios con email sin confirmar.
- **`IEmailSender`** (abstracción en `Application/Common/Email`): `SmtpEmailSender` genérico (`System.Net.Mail`, sin vendor lock-in) en prod, `LoggingEmailSender` (vuelca el link a Seq) en Dev/Test. `EmailSetup` elige por entorno con **fail-fast** si faltan `Email:Smtp:Host`/`From` fuera de Dev/Test. Reutilizable para el sistema de notificaciones (#51).

### Citas y disponibilidad

- **`Employee.MaxConcurrentAppointments`** (default 1): permite modelar profe de música con clase grupal de 5, peluquera con tinte que atiende a 2 clientes en paralelo, monitor de yoga con 15 plazas...
- **`IAppointmentSchedulingValidator`** valida en `CreateAppointment`/`UpdateAppointment`:
  - Fechas válidas, EndDate > StartDate, no pasado
  - Client/Employee/Service existen, Employee activo
  - Service y Employee del mismo Business
  - Duración coincide con `service.DurationMinutes`
  - Día abierto (consulta `IScheduleResolver`)
  - Encaja en una franja continua (no cruza descanso de turno partido)
  - **Capacidad del empleado no excedida** (overlappingCount < MaxConcurrentAppointments)
- **`AvailabilityService`** calcula `Capacity` por slot = suma de `(MaxConcurrent - overlapping)` por empleado libre. El front pinta `slot.capacity` directamente.

### Horarios

- **One Business → many ScheduleTemplates** que no se solapan en fechas. La elección del horario efectivo es por fecha (no por flag `IsDefault` — existe pero no afecta resolución).
- **ScheduleOverride** prevalece sobre la plantilla para un día concreto.
- **`IScheduleResolver`** es la fuente de verdad para "qué horario aplica el día X" — úsalo siempre, no reimplementes. Vive en `Domain/Services/`, impl en `Infrastructure/Services/`.
- **`ScheduleService.UpdateTemplateAsync` / `UpdateOverrideAsync` usan asignación campo a campo, NO AutoMapper.** Es defensa en profundidad — los DTOs no tienen BusinessId y el servicio no lo tocaría aunque viniera. Patrón que el resto de services podrían adoptar (issue futuro si se decide).
- **SRP de Schedule (#100):** la generación masiva (el "wizard" de configuración inicial) se extrajo a **`IScheduleGenerationService`** (`GenerateScheduleAsync` + `ValidateTemplatesDoNotOverlap`). `ScheduleService` se queda con CRUD de templates/overrides + `GetEffectiveSchedule` + `GetCalendar`. El `GenerateScheduleCommandHandler` inyecta el servicio nuevo.

### Errors

- **`ExceptionHandlingMiddleware`** mapea excepciones a JSON con código:
  - `AuthenticationException` → **401** `UNAUTHENTICATED` (PR #80)
  - `KeyNotFoundException` → 404 `NOT_FOUND`
  - `UnauthorizedAccessException` → 403 `FORBIDDEN`
  - `InvalidOperationException` / `ArgumentException` → 400 `BAD_REQUEST`
  - `FluentValidation.ValidationException` → 400 `VALIDATION_ERROR` con `errors: { Field: ["msg"] }`
  - Otros → 500 `INTERNAL_ERROR`
- **Orden importa** en el switch: `AuthenticationException` antes que las base (aunque no comparten jerarquía).

### Rate limiting (en `/api/auth/*`)

- `auth-login`: 5 / IP / minuto
- `auth-refresh`: 10 / IP / minuto
- `auth-register`: 3 / IP / hora
- Respuesta 429 con header `Retry-After` y body `{ code: "RATE_LIMITED" }`.
- **Skipeado en environment "Testing"** (porque el rate limit de login 5/min colisiona con el test de lockout 5 fallos).

### CORS

- Configurable por `Cors:AllowedOrigins` (issue #50, PR #75).
- **Development + Testing**: fallback permisivo (`AllowAnyOrigin`) con warning si lista vacía.
- **Production / Staging / cualquier otro**: **fail-fast** si lista vacía (la app no arranca).

### ForwardedHeaders en producción (issue #83, PR #84)

- Activo solo si `Environment != Development && != Testing`.
- Lee `X-Forwarded-For` para que el rate limiter use la IP real del cliente tras proxy.
- **Trust loopback only por defecto**. Para proxies remotos (Cloudflare, ALB) configurar `ForwardedHeaders:KnownProxies` o `KnownNetworks`.

### Auto-migración EF Core en Development (issue #85, PR #86)

- `Program.cs` llama a `db.Database.MigrateAsync()` al arrancar **solo si `IsDevelopment()`**.
- Producción NO usa esto — las migraciones van en el pipeline de deploy.
- Testing tampoco (usa InMemory que no soporta `Migrate()`).

### Listas y paginación

- `PagedResult<T> { Items, Page, PageSize, TotalCount, TotalPages }` en `Application.Common`.
- `PaginationConstants` (DefaultPage=1, DefaultPageSize=50, MaxPageSize=200) en `Domain/Constants/` (movido en #105 junto a Roles/RolePolicies).
- Todos los endpoints `GET *` de listados devuelven `PagedResult<TDto>` con `?page=&pageSize=`.

### DTOs públicos vs autenticados

- `BusinessDto` (completo: Id, Name, Description, Address, Phone, Email, IsActive) → usado en `POST/PUT` (con `[Authorize]`).
- `BusinessPublicDto` (Id, Name, Description, Address, Phone — **sin email, sin IsActive**) → usado en `GET /api/business` y `GET /api/business/{id}` (con `[AllowAnonymous]`). Filtra inactivos. PR #90.
- **`ServiceDto` también es público** (`GET /api/service[/{id}]` es `[AllowAnonymous]`) pero NO tiene equivalente Public porque sus campos son catálogo (Name, Price, DurationMinutes, BusinessId) sin info sensible. Decisión consciente.

## Roles del sistema (auth)

| Rol | Quién |
|-----|-------|
| `Admin` | Super-usuario del sistema. Se crea al arrancar la app via `AdminSeed:Email/Password/FullName` en user-secrets. |
| `BusinessOwner` | Dueño de un negocio. Auto-creado como Employee al registrarse. |
| `Employee` | Trabajador de un negocio. Creado por su Owner. |
| `Client` | Cliente final. Autoregistro público (`POST /api/auth/register/client`). |

### Combos en `[Authorize(Roles = ...)]` (usar `RolePolicies`, no concatenar)

- `RolePolicies.AdminOrOwner` → `Admin + BusinessOwner`. Usado en CRUD de Business, Employee, Service.
- `RolePolicies.Staff` → `Admin + BusinessOwner + Employee`. Usado en Schedule y Appointment.
- `RolePolicies.AdminOrSelfClient` → `Admin + Client`. Usado en Update/Delete de Client.

## Comandos típicos

```bash
# Build (debe estar 0 errores, 0 warnings)
dotnet build

# Tests (97/97 verdes a fecha de hoy)
dotnet test

# Migración nueva
dotnet ef migrations add NombreMigracion --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api --output-dir Migrations

# Aplicar migraciones a BD local (en Development NO hace falta — la API las aplica al arrancar)
dotnet ef database update --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Quitar la última migración (antes de pushear)
dotnet ef migrations remove --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Configurar secretos (en dev, en MI máquina ya están puestos)
cd src/MRC.Agendia.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
dotnet user-secrets set "AdminSeed:Email" "admin@agendia.local"
dotnet user-secrets set "AdminSeed:Password" "TuPasswordFuerte123!"
dotnet user-secrets set "AdminSeed:FullName" "Administrador"

# Lanzar la API (en Development arranca + auto-migra + seed admin)
dotnet run --project src/MRC.Agendia.Api
```

## Workflow recomendado al implementar una issue

1. **Cambiar a `master` y pullear:** `git checkout master && git pull origin master`
2. **Crear rama** con el número de la issue: `git checkout -b <num>-<slug-corto>` (ej: `42-rate-limiting`).
3. **Implementar** siguiendo las convenciones.
4. **Build limpio:** `dotnet build` SIN warnings ni errores.
5. **Tests:** `dotnet test` SIN regresiones (97/97 actual).
6. **Commit** en español, sin tildes (SIN trailer de atribución a Claude):
   ```
   <tipo>: <descripción corta>

   Closes #<num>

   <detalle>
   ```
   Tipos: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`.
7. **Push** + abrir PR con `gh pr create --base master` (sin footer "Generated with Claude Code").
8. **NO mergear automáticamente.** Esperar a review humana.
9. **Una PR cada vez.** No empezar la siguiente issue hasta que el humano apruebe y mergee la actual, y se haga `git pull` de master. Encadenar PRs sin mergear arriesga conflictos.

## Cosas que NO debes hacer sin permiso explícito

- ❌ **Mergear PRs a `master`.** Solo el humano (hay branch protection).
- ❌ **`git push --force`** o `--force-with-lease`. Bloqueado en `.claude/settings.json`.
- ❌ **`git reset --hard`** ni `git clean -f`.
- ❌ **`dotnet ef database drop`.** Borra la BD del usuario.
- ❌ **Cambiar la arquitectura sin discutirlo primero** (mover capas, cambiar de MediatR a otra cosa, etc.).
- ❌ **Borrar entidades del dominio** sin confirmar.
- ❌ **Modificar `appsettings.json` añadiendo valores reales.** Solo placeholders.
- ❌ **Crear ramas largas con muchas issues mezcladas.** Una rama = una issue (o un grupo cohesivo pequeño).
- ❌ **Comentarios en español en código nuevo.** Convención del equipo: inglés en código, español en docs visibles al usuario (Swagger summaries de endpoints, exception messages, validation messages).
- ❌ **Concatenar `Roles.X + "," + Roles.Y`** en los `[Authorize]`. Usa `RolePolicies.<combo>`.
- ❌ **Incluir `BusinessId` en `UpdateXDto`** de un recurso scoped a Business. Si AutoMapper lo mapea, abre vector de cross-tenant takeover (#91). En su lugar: omitir el campo del DTO, o usar `Ignore` en el Profile.
- ❌ **Validar `dto.BusinessId` en handlers de Update**. Validar siempre sobre el recurso **actual** (`EnsureCanManageXAsync(dto.Id)`).
- ❌ **Refactor de las rutas singular/plural** sin coordinar con el frontend. Acoplado a las rutas actuales.

## Workflow GitHub — detalle operativo

- **`gh` CLI no está en el PATH del bash de Claude.** Invocar siempre con ruta completa: `"/c/Program Files/GitHub CLI/gh.exe"`. (Memoria persistida en `reference_gh_cli.md`.)
- **Labels disponibles** en el repo: `area/auth`, `area/api`, `area/db`, `area/schedules`, `area/appointments`, `priority/{critical,high,medium,low}`, `type/{feature,bug,refactor,chore,tests,security}`. No hay `area/persistence` ni `area/auth` aceptan combos.

## Estado del backlog (2026-05-22) — 12 issues abiertas

> Cerradas esta sesión: #57, #99, #100, #101, #104, #105 (+ #102 con este PR de docs).

### Priority/medium (4 issues — siguiente sprint razonable)

| # | Título | Esfuerzo | Notas |
|---|---|---|---|
| [#49](https://github.com/MarcosRabadan/Agendia/issues/49) | Propagar CancellationToken hasta el repositorio | Mediano | Refactor mecánico. Bajo riesgo. |
| [#51](https://github.com/MarcosRabadan/Agendia/issues/51) | Sistema de notificaciones (email + push) | Grande | Feature core. **El `IEmailSender` de #57 ya cubre el envío de email**; falta el push (FCM) y los triggers de cita. |
| [#52](https://github.com/MarcosRabadan/Agendia/issues/52) | Soft delete + audit fields globales | Grande | Overlap con #56 y #58. Coordinar si se atacan. |
| [#54](https://github.com/MarcosRabadan/Agendia/issues/54) | Endpoint preview de schedule (sin persistir) | Pequeño-mediano | Bien acotada. La feature más rentable del backlog (front la necesita). |

### Priority/low (8 issues)

| # | Título | Notas |
|---|---|---|
| [#55](https://github.com/MarcosRabadan/Agendia/issues/55) | Caching de festivos y plantillas | Optimización. No atacar sin métricas. |
| [#56](https://github.com/MarcosRabadan/Agendia/issues/56) | Audit log de eventos sensibles | Coordinar con #52. |
| [#58](https://github.com/MarcosRabadan/Agendia/issues/58) | Global query filter por BusinessId | Defense in depth (los bugs reales se cerraron en #87/#91/#93). |
| [#59](https://github.com/MarcosRabadan/Agendia/issues/59) | Logout-all | Simple. Tras #99 el sitio natural es `AuthService` + `IRefreshTokenStore`. |
| [#60](https://github.com/MarcosRabadan/Agendia/issues/60) | Códigos de error más descriptivos | Parcialmente hecho con `AuthenticationException` (#80). |
| [#61](https://github.com/MarcosRabadan/Agendia/issues/61) | Correlation ID en headers + logs | Existe `traceId`, esto lo amplía. |
| [#62](https://github.com/MarcosRabadan/Agendia/issues/62) | Health checks ricos (SQL, Seq…) | Útil para deploy. |
| [#103](https://github.com/MarcosRabadan/Agendia/issues/103) | Pase global comentarios ES→EN | Mediano. Última deuda cosmética que queda. |

### Pendientes "fuera de scope" (no en el backlog)

- **Cloud secret manager** (Azure Key Vault / AWS Secrets Manager / GCP). Aparcado hasta decidir cloud. Memoria persistida en `project_prod_secrets.md`.
- **Refactor `Resource` para salas/equipos** (generalizar Employee → Resource abstracto). Marcado "solo si surge necesidad". Hoy Employee+capacity cubre los casos reales.

## Cómo retomar el desarrollo (recomendación honesta)

Próximas sesiones, mi voto razonado por orden:

1. **Antes de producción real**: verificar el **envío SMTP real** (`SmtpEmailSender`) contra un relay (Mailtrap/SES) — la lógica de #57 está hecha y testeada con un fake, pero el envío real no se ha probado contra un servidor.
2. **Si quieres valor de producto nuevo**: **#54 (preview schedule)** es la feature más rentable (front la necesita) y la más acotada del backlog.
3. **Si quieres seguir cerrando deuda**: **#49 (propagar CancellationToken)** es mecánico y de bajo riesgo; **#103 (comentarios ES→EN)** cierra la última cosmética; **#59 (logout-all)** es simple y encaja tras el refactor de #99.
4. **Feature grande**: **#51 (notificaciones)** — el `IEmailSender` ya está, falta push (FCM) y los triggers de cita.
5. **NO atacar #58 (global query filter)** salvo que entren más devs al equipo o haya un incidente real de leak. Defense in depth con coste operativo alto.

## Si te quedas sin contexto / dudas

- **Sistema de horarios complejo:** lee `src/MRC.Agendia.Infrastructure/Services/ScheduleResolver.cs`.
- **Auth resource-based:** lee `src/MRC.Agendia.Infrastructure/Authorization/ResourceAuthorizationService.cs`.
- **Validación de citas:** lee `src/MRC.Agendia.Application/Appointments/AppointmentSchedulingValidator.cs`.
- **Algoritmo de disponibilidad:** lee `src/MRC.Agendia.Application/Availability/AvailabilityService.cs`.
- **Cómo se mapean excepciones:** `src/MRC.Agendia.Api/Middleware/ExceptionHandlingMiddleware.cs`.
- **Cómo se construye un test de integration:** mira `tests/MRC.Agendia.Tests.Integration/Infrastructure/CustomWebApplicationFactory.cs` + cualquier `*IntegrationTests.cs`. Hay helpers para registrar Owner y obtener su `BusinessDto`.
- Si una decisión de diseño no es obvia, **pregúntale al usuario antes de codificar**.

## Cosas pendientes / out of scope conocidas

- Refactor "Resource" para soportar salas/equipos abstractos (vs. el actual `Employee` con `MaxConcurrentAppointments`). Hoy `Employee + capacity` cubre los casos reales planteados.
- Cloud secret manager para producción. Aparcado por decisión de cloud.
- Tests unitarios de CRUD handlers (Business, Client, Employee, Service, Appointment). Los críticos ya están cubiertos vía integration. Issue no creada — deuda conocida.
- Verificación del envío SMTP real contra un relay (la lógica de #57 está hecha y testeada con un fake; el `SmtpEmailSender` no se ha probado contra un servidor real).
