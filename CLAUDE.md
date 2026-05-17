# Claude project notes — MRC.Agendia

Este fichero se carga **automáticamente** al inicio de cada sesión de Claude Code.
Lee con atención antes de tocar código.

## Qué es este proyecto

API REST de gestión de citas para negocios (peluquerías, clínicas, talleres…).
Cubre alta de negocios, empleados, servicios, clientes y citas, con un sistema de
horarios potente (plantillas anuales, festivos, vacaciones, overrides por día,
turnos partidos).

- **Idioma del producto:** Español
- **Idioma de los commits:** Español neutro sin tildes (algunos terminales no las muestran bien).
- **Idioma de los comentarios en código:** **Inglés**. Es la convención del equipo.
- **Idioma de la documentación visible al usuario** (Swagger summaries, mensajes de error de validación): Español.

## Stack

- **.NET 9.0** + ASP.NET Core Web API
- **Clean Architecture + DDD** (capas: Api / Application / Domain / Infrastructure)
- **CQRS con MediatR** — cada caso de uso es un Command/Query + Handler
- **AutoMapper** — Entity ↔ DTO
- **Entity Framework Core 8.0.11** + SQL Server
- **ASP.NET Identity** + **JWT Bearer** para auth + Refresh tokens con rotación
- **FluentValidation** con `ValidationBehavior` de MediatR (corre antes del handler)
- **Rate limiting** built-in de ASP.NET en endpoints de auth
- **Serilog + Seq** (`http://localhost:5341`) para logging
- **Swagger / OpenAPI** documentando todo

## Estructura de carpetas

```
src/
├── MRC.Agendia.Api/
│   ├── Configuration/         ← Módulos de wiring (LoggingSetup, AuthenticationSetup, RateLimitingSetup, SwaggerSetup, CorsSetup, PipelineExtensions)
│   ├── Controllers/           ← Auth, Business, Client, Employee, Service, Appointment, Schedule, Holiday, Availability
│   ├── Middleware/            ← ExceptionHandlingMiddleware (mapea excepciones a JSON)
│   ├── Services/              ← CurrentUserContext, CurrentUserService
│   └── Program.cs             ← Orquesta builder.ConfigureSerilog/AddInfrastructure/AddApplication/AddIdentityAndJwt + pipeline
├── MRC.Agendia.Application/
│   ├── Auth/                  ← IAuthService + Commands (Login, Register*, Refresh, Logout, ChangePassword) + DTOs
│   ├── Authorization/         ← ICurrentUserContext, IResourceAuthorizationService
│   ├── Availability/          ← Endpoint clave: huecos libres para reservar
│   ├── Appointments/          ← Incluye IAppointmentSchedulingValidator (reglas de cita válida)
│   ├── Behaviors/             ← ValidationBehavior (MediatR pipeline)
│   ├── Business/Clients/Employees/Services/Schedules/Holidays/
│   ├── Mappings/              ← Profiles de AutoMapper
│   └── DependencyInjection.cs ← AddApplication() — auto-discovery de MediatR/AutoMapper/FluentValidation + servicios
├── MRC.Agendia.Domain/
│   ├── Constants/             ← Roles.cs (Admin, BusinessOwner, Employee, Client)
│   ├── Entities/              ← Business, Client, Employee (con MaxConcurrentAppointments), Service, Appointment, ScheduleTemplate, ScheduleOverride, HolidayCalendar...
│   ├── Enums/                 ← AppointmentStatus, ScheduleOverrideType, HolidayScope...
│   ├── Interfaces/            ← I*Repository
│   └── Services/              ← IScheduleResolver (domain service)
├── MRC.Agendia.Infrastructure/
│   ├── Authorization/         ← ResourceAuthorizationService (impl)
│   ├── Identity/              ← ApplicationUser, JwtTokenService, AuthService, DbInitializer, RefreshTokenCleanupService (BackgroundService)
│   ├── Migrations/
│   ├── Repositories/
│   ├── Services/              ← ScheduleResolver (impl)
│   ├── AgendiaDbContext.cs    ← IdentityDbContext + DbSets
│   └── DependencyInjection.cs ← AddInfrastructure(config) — DbContext + repos + auth + cleanup hosted service
└── MRC.Agendia.Shared/        ← Poco usado
tests/
└── MRC.Agendia.API.Tests/     ← Esqueleto, sin cobertura todavía
scripts/
└── create-github-issues.sh    ← Backlog bulk creation
```

## Flujo de una petición

```
Controller (Api)
   → MediatR.Send(Command/Query)
      → ValidationBehavior (FluentValidation: si falla → ValidationException → 400 con errores estructurados)
         → Handler
            → IResourceAuthorizationService.EnsureCan*Async(...) ← Resource-based auth si aplica
            → Service (Application)
               → IAppointmentSchedulingValidator (en CreateAppointment/UpdateAppointment, valida horario + capacidad)
               → Repository (Infrastructure / EF Core)
                  → SQL Server
```

## Convenciones — IMPORTANTES, no las rompas

1. **Una clase por archivo.** Cada `class`, `record`, `enum`, `interface` en su propio `.cs`.
2. **Records inmutables** para todos los DTOs.
3. **Naming consistente:**
   - `*Command` + `*CommandHandler` + `*CommandValidator` (FluentValidation, opcional pero recomendado)
   - `*Query` + `*QueryHandler` + `*QueryValidator`
   - `*Repository` + `I*Repository`
   - `*Service` + `I*Service`
   - `*Dto`, `Create*Dto`, `Update*Dto`
4. **`async`/`await`** en todos los accesos a BD. Nunca `.Result` ni `.Wait()`.
5. **Validar autorización en handlers**, no en controllers. Inyecta `IResourceAuthorizationService` y llama al `EnsureCan*Async` correspondiente ANTES de delegar al servicio.
6. **Validar inputs con FluentValidation.** Cada Command/Query merece un Validator. El `ValidationBehavior` ya está registrado, basta con crear el archivo siguiendo el naming convention.
7. **Comentarios en código en inglés.** Mensajes de error visibles al usuario en español.
8. **Migraciones EF**: cada cambio de modelo va con su migración. Comprueba que `dotnet ef migrations add` no genera warnings inesperados.
9. **Sin secretos en `appsettings.json`.** Usa `dotnet user-secrets` en dev y variables de entorno en producción.
10. **No mergees a master.** Hay branch protection. Solo el humano.

## Decisiones de diseño tomadas (no las cuestiones sin razón fuerte)

### Auth
- **No usamos `Microsoft.AspNetCore.Identity.SignInManager`** (vive en el shared framework). La validación de password usa `UserManager.CheckPasswordAsync` directamente.
- **`ApplicationUser` vive en Infrastructure**, no en Domain. Domain solo tiene `string UserId` en Business/Employee/Client.
- **Authorization basada en recursos** vía `IResourceAuthorizationService` (en Application, impl en Infrastructure). No usamos `IAuthorizationHandler<TRequirement>` de ASP.NET — lanzamos `UnauthorizedAccessException` que el middleware mapea a 403.
- **Refresh tokens con rotación**: cada uso genera uno nuevo y revoca el anterior.
- **`RefreshTokenCleanupService`** (BackgroundService) limpia tokens expirados cada 24h.
- **Auto-creación de Employee al registrar Owner**: cuando alguien hace `POST /api/auth/register/owner`, se crea User + Business + Employee del owner. Un autónomo está listo para recibir reservas sin pasos extra.

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
- **`IScheduleResolver`** es la fuente de verdad para "qué horario aplica el día X" — úsalo siempre, no reimplementes.

### Errors
- **`ExceptionHandlingMiddleware`** mapea excepciones a JSON con código:
  - `KeyNotFoundException` → 404 `NOT_FOUND`
  - `UnauthorizedAccessException` → 403 `FORBIDDEN`
  - `InvalidOperationException` / `ArgumentException` → 400 `BAD_REQUEST`
  - `FluentValidation.ValidationException` → 400 `VALIDATION_ERROR` con `errors: { Field: ["msg"] }`
  - Otros → 500 `INTERNAL_ERROR`

### Rate limiting (en `/api/auth/*`)
- `auth-login`: 5 / IP / minuto
- `auth-refresh`: 10 / IP / minuto
- `auth-register`: 3 / IP / hora
- Respuesta 429 con header `Retry-After` y body `{ code: "RATE_LIMITED" }`.

### Listas y paginación
- **PR #73 (pagination) está abierto pero NO mergeado.** Cuando se mergee:
  - `PagedResult<T> { Items, Page, PageSize, TotalCount, TotalPages }` en `Application.Common`
  - `PaginationConstants` (DefaultPageSize=50, MaxPageSize=200)
  - Endpoints GET de Business/Client/Employee/Service/Appointment devolverán `PagedResult<TDto>` con `?page=&pageSize=`
  - Si está mergeado al iniciar la sesión, verifica el estado real antes de añadir cosas.

## Roles del sistema (auth)

| Rol | Quién |
|-----|-------|
| `Admin` | Super-usuario del sistema (`MRC.Agendia.Domain.Constants.Roles.Admin`) |
| `BusinessOwner` | Dueño de un negocio. Auto-creado como Employee al registrarse. |
| `Employee` | Trabajador de un negocio. |
| `Client` | Cliente final. |

Reglas de permisos: ver matriz en PR #35.

## Comandos típicos

```bash
# Build (debe estar 0 errores, 0 warnings)
dotnet build

# Tests
dotnet test

# Migración nueva
dotnet ef migrations add NombreMigracion --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api --output-dir Migrations

# Aplicar migraciones a BD local (solo si se arranca sin pasar por `dotnet run`,
# porque en Development la API las aplica automaticamente al arrancar)
dotnet ef database update --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Quitar la última migración (antes de pushear)
dotnet ef migrations remove --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Configurar secretos (en dev, en MI máquina ya están puestos)
cd src/MRC.Agendia.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
dotnet user-secrets set "AdminSeed:Email" "admin@agendia.local"
dotnet user-secrets set "AdminSeed:Password" "TuPasswordFuerte123!"
dotnet user-secrets set "AdminSeed:FullName" "Administrador"
```

## Workflow recomendado al implementar una issue

1. **Cambiar a `master` y pullear:** `git checkout master && git pull origin master`
2. **Crear rama** con el número de la issue: `git checkout -b <num>-<slug-corto>` (ej: `42-rate-limiting`).
3. **Implementar** siguiendo las convenciones.
4. **Build limpio:** `dotnet build` SIN warnings ni errores.
5. **Commit** en español, sin tildes:
   ```
   <tipo>: <descripción corta>

   Closes #<num>

   <detalle>

   Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
   ```
   Tipos: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`.
6. **Push** + abrir PR con `gh pr create --base master`.
7. **NO mergear automáticamente.** Esperar a review humana.

## Cosas que NO debes hacer sin permiso explícito

- ❌ **Mergear PRs a `master`.** Solo el humano (hay branch protection).
- ❌ **`git push --force`** o `--force-with-lease`. Bloqueado en `.claude/settings.json`.
- ❌ **`git reset --hard`** ni `git clean -f`.
- ❌ **`dotnet ef database drop`.** Borra la BD del usuario.
- ❌ **Cambiar la arquitectura sin discutirlo primero** (mover capas, cambiar de MediatR a otra cosa, etc.).
- ❌ **Borrar entidades del dominio** sin confirmar.
- ❌ **Modificar `appsettings.json` añadiendo valores reales.** Solo placeholders.
- ❌ **Crear ramas largas con muchas issues mezcladas.** Una rama = una issue (o un grupo cohesivo pequeño).
- ❌ **Comentarios en español en código nuevo.** Convención del equipo: inglés en código, español en docs y Swagger.

## Estado del backlog

El backlog vive en GitHub Issues. Issues abiertas priorizadas con labels `priority/critical|high|medium|low`. Script `scripts/create-github-issues.sh` para regenerar si hace falta.

**PRs abiertos a vigilar al inicio de sesión:**
- Comprueba con `gh pr list --state open` para ver qué hay pendiente de mergear.

## Si te quedas sin contexto / dudas

- **Sistema de horarios complejo:** lee `src/MRC.Agendia.Infrastructure/Services/ScheduleResolver.cs`.
- **Auth resource-based:** lee `src/MRC.Agendia.Infrastructure/Authorization/ResourceAuthorizationService.cs`.
- **Validación de citas:** lee `src/MRC.Agendia.Application/Appointments/AppointmentSchedulingValidator.cs`.
- **Algoritmo de disponibilidad:** lee `src/MRC.Agendia.Application/Availability/AvailabilityService.cs`.
- Si una decisión de diseño no es obvia, **pregúntale al usuario antes de codificar**.

## Cosas pendientes / fuera de scope conocidas

- Refactor "Resource" para soportar salas/equipos/cupos abstractos como ciudadanos de primera (vs. el actual `Employee` con `MaxConcurrentAppointments`). De momento Employee+capacity cubre los casos reales planteados.
- Configurar `UseForwardedHeaders` en producción para que el rate limiting use la IP real del cliente cuando esté detrás de un proxy.
- Tests (issues #45, #46, #47): la base no tiene cobertura todavía.
