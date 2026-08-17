# Claude project notes — MRC.Agendia

Este fichero se carga **automáticamente** al inicio de cada sesión de Claude Code.
Lee con atención antes de tocar código.

> **Estado (Fase 7 del epic #241, 2026-08-15).** Agendia es un **microservicio de
> reservas puras**: gestiona agenda (negocios como contenedores, empleados/recursos,
> servicios, horarios, citas, lista de espera) y **nada más**. La identidad la posee
> **Harmony** (Agendia solo valida sus JWT); el perfil del negocio y el catálogo/precio
> del servicio los posee el **servicio de gestión**. Persistencia en **PostgreSQL**,
> PK/FK en **GUID (UUIDv7)**, y las notificaciones salen como **eventos de integración**
> por un outbox transaccional (Agendia ya no envía email/push). Contexto de datos en
> [`docs/bounded-context.md`](docs/bounded-context.md).

## Qué es este proyecto

API REST de gestión de citas para negocios (peluquerías, clínicas, talleres…). Cubre alta
de negocios, empleados/recursos, servicios y citas, con un sistema de horarios potente
(plantillas anuales, festivos, vacaciones, overrides por día, turnos partidos), lista de
espera, series recurrentes, disponibilidad y estadísticas.

Forma parte de un ecosistema de microservicios:
- **Harmony** — identidad: registra usuarios, firma los JWT, gestiona credenciales/roles.
- **Gestión / catálogo** — perfil del negocio (nombre, dirección, contacto) y catálogo de
  servicios (nombre, descripción, **precio**).
- **Agendia** (este repo) — la **agenda**.
- **Consumidor de notificaciones** — recibe los eventos de Agendia y entrega email/push.

## Idiomas (convención del equipo)

- **Mensajes de runtime** (validación de FluentValidation, `exception messages`, logs de
  Serilog, mensajes `Skip.IfNot` y asserts de test): **INGLÉS** (barrido #274). El front
  ramifica por `code`, no por `message`, así que el idioma del `message` le da igual. Al
  escribir uno nuevo, en inglés desde el principio (no copiar el patrón español viejo).
- **Commits:** español neutro **sin tildes** (algunos terminales no las muestran bien).
- **Comentarios en código:** **Inglés**.
- **Documentación XML** (`<summary>`/`<param>`/`<returns>` de interfaces/servicios y
  summaries de endpoints, que salen en Swagger): **Inglés**.

## Stack

- **.NET 9.0** + ASP.NET Core Web API
- **Clean Architecture + DDD** (Api / Application / Domain / Infrastructure)
- **CQRS con MediatR** — cada caso de uso es un Command/Query + Handler
- **AutoMapper** — Entity ↔ DTO (con cuidado en updates, ver "Decisiones de diseño")
- **Entity Framework Core 9.0.0** + **PostgreSQL** (Npgsql 9.0.0). Contenedor de dev en el
  **puerto 5433** (hay un Postgres nativo en 5432 que chocaba).
- **JWT Bearer** — Agendia **solo valida** tokens que firma Harmony (HS256, clave simétrica
  compartida). No tiene Identity, ni tablas `AspNet*`, ni emite tokens de usuario.
- **Auth máquina-a-máquina (M2M)** — `POST /api/auth/service-token` (client-credentials)
  para servicios de confianza; Agendia sí firma ese token (misma clave).
- **FluentValidation** con `ValidationBehavior` de MediatR (corre antes del handler)
- **Eventos de integración** — outbox transaccional + dispatcher + `IEventTransport`
  swappable (hoy `LoggingEventTransport`, log-only; broker por decidir).
- **Serilog + Seq** (`http://localhost:5341`) para logging
- **Swagger / OpenAPI** (con XML docs de endpoints)
- **Tests:** xUnit + NSubstitute + EF InMemory (unit); WebApplicationFactory + **Testcontainers**
  (Postgres real por HTTP y a nivel de `DbContext`) (integration). Ver "Estrategia de tests".

### Gotchas de entorno (IMPORTANTES)

- **La pila EF está fijada en 9.0.0 a propósito.** Los `AspNetCore.HealthChecks.UI.*`
  arrastran EF 9.0.0; subir la pila rompe CI con `-warnaserror` (conflicto en el paquete
  Relational). No subas EF sin resolver eso.
- **`dotnet ef` no está en el PATH del bash de Claude.** Usar
  `"C:/Users/marco/.dotnet/tools/dotnet-ef.exe"` y **el tool debe ser EF 9+** (un
  `Microsoft.EntityFrameworkCore.Design.dll` 8.x obsoleto en el `bin` rompe el scaffolding;
  ver memoria `reference_dotnet_ef.md`).
- **Hay 2 DbContext** (`AgendiaDbContext` + el de HealthChecks UI): en los comandos `ef`
  pasar **siempre** `--context AgendiaDbContext`.
- **`gh` CLI tampoco está en el PATH:** invocar `"/c/Program Files/GitHub CLI/gh.exe"`.
- **Índices únicos con columnas NULL en Postgres:** Postgres trata `NULL != NULL`, así que un
  índice único que deba deduplicar filas con NULL (p. ej. waitlist "cualquier empleado")
  necesita `NULLS NOT DISTINCT` en la migración.

## Política de trabajo y autonomía (LEE ESTO PRIMERO)

Instrucciones explícitas del usuario (vigentes):

- **Bugs, refactors y chores → AUTOMÁTICO de principio a fin**, incluido mergear a master
  con `"/c/Program Files/GitHub CLI/gh.exe" pr merge <num> --admin --merge --delete-branch`
  (el `--admin` salta la branch protection; la gh CLI está autenticada como el owner). NO
  esperar revisión humana.
- **Features nuevas → PR para el humano.** Crear la PR contra master y **NO** auto-mergear.
- **Analizar las decisiones con agentes varias veces** (reviewers + verificadores en varias
  rondas) antes de auto-mergear un bug/refactor: confirmar que el cambio es correcto, no
  rompe flujos y no añade código innecesario. Al usuario le preocupa introducir bugs.
- **No pedir permiso para ejecutar** tests/comandos. Solo consultar: **(a) dudas**, **(b)
  decisiones de producto/diseño y preferencias**, **(c) antes de abrir una PR**, **(d)
  confirmar creación de issues**.
- **Reutilizar código** siempre que se pueda; **no dejar código muerto** (pero sin
  abstracciones prematuras: 3 líneas repetidas no justifican una abstracción).
- **`docs/error-codes.md` se mantiene al día** con cada excepción nueva.
- **Aislar los agentes revisores** (worktree o git solo-lectura): en el árbol compartido
  pueden perder cambios sin commitear.
- **Si una opción implica cientos/miles de ediciones mecánicas**, dar el conteo real y dejar
  re-escoger alcance antes de lanzarse.

## Estado del epic #241 (reservas puras)

Refactor multi-fase que convirtió Agendia en microservicio de reservas puro:

| Fase | Qué | Estado |
|---|---|---|
| 1–4 | Data ownership: matar `Client`, adelgazar `Business`/`Employee`/`Service` (fuera perfil/precio), identidad a Harmony | ✅ |
| 5 (#246) | Notificaciones por eventos: outbox + dispatcher + `IEventTransport`; fuera email/push/DeviceToken | ✅ (PR #253) |
| 6 paso 1 (#247) | Migración SQL Server → **PostgreSQL** (Npgsql, `pg_advisory_xact_lock`, Testcontainers) | ✅ (PR #255/#256) |
| 6 paso 2 (#247) | PK/FK `int` → **GUID (UUIDv7)** | ✅ (PR #257) |
| 7 (#248) | Reubicar revenue de stats + docs/limpieza (**esta fase**) | 🟢 en curso |

### Trabajo post-epic (auditoría #241 → follow-ups mergeados)

Tras cerrar el epic se hizo una auditoría a fondo y una tanda de fixes/mejoras:

- **Outbox endurecido** (#276): `OutboxProcessor` testeable; el poll excluye venenosos
  (`Attempts >= MaxAttempts`, dead-letter), purga por retención, y **claim `FOR UPDATE SKIP
  LOCKED`** → N instancias no se pisan. Config vía `OutboxOptions`.
- **Reminder N-instancias** (#278): `ReminderProcessor` con **`pg_try_advisory_lock`**
  (exclusión mutua, forma two-int, no colisiona con el guard de reservas). Config `ReminderOptions`.
- **Eventos de dominio en el agregado** (#279): ver la decisión de diseño más abajo. Eliminados
  `IEventPublisher`/`OutboxEventPublisher`. Las series emiten eventos por ocurrencia.
- **Evento `AppointmentRescheduled`** (#280): al mover una cita (individual o de serie).
- **Menores** (#281): `SeriesId` UUIDv7; M2M en **tiempo constante** (PBKDF2 dummy anti
  enumeración); default de `CreatedAt` → `now()`; docs de R4 (guard Npgsql-only) y R7 (404-vs-403).
- **Runtime a inglés** (#274): logs, `exception messages` y validación FluentValidation en inglés.
- **Bug cazado y arreglado**: `Appointment.StartDate/EndDate` estaban en `timestamptz`; con hora
  de pared (`Kind=Unspecified`) Npgsql lanza → ahora `timestamp without time zone` (#273).
- **Tests full-stack sobre Postgres real** (#272): `PostgresWebApplicationFactory` (API + Npgsql
  de Testcontainers). Ver "Estrategia de tests" más abajo.

**Pendiente:** cutover del front · elegir + cablear el **broker real** (RabbitMQ/ASB/Kafka) en
`IEventTransport` · **features** de producto (#266 idempotencia, #267 no-show, #268 auto-rebooking, #269 analítica,
#270 cancelación por tramos, #271 time-off) · **B9** (concurrencia optimista `xmin`, aplazada) ·
**pruebas a fondo** (el usuario las quiere al final).

## Estructura de carpetas

```
src/
├── MRC.Agendia.Api/
│   ├── Configuration/         ← Wiring: AuthenticationSetup (valida JWT de Harmony + M2M),
│   │                            CorsSetup, HealthChecksSetup, LoggingSetup, SwaggerSetup,
│   │                            PipelineExtensions
│   ├── Controllers/           ← Auth (solo M2M service-token), Business, Employee, Service,
│   │                            Appointment, Schedule, Holiday, Availability, BusinessStats,
│   │                            CancellationPolicy, ClientReliability, EmployeeTimeOff,
│   │                            Waitlist,
│   │                            DelayNotification, AuditLog
│   ├── Filters/               ← IdempotencyFilter (cabecera Idempotency-Key)
│   ├── Middleware/            ← ExceptionHandlingMiddleware + CorrelationIdMiddleware
│   ├── Services/              ← CurrentUserContext (lee sub/roles del token)
│   └── Program.cs             ← Wiring + auto-migrate (Dev). Sin seed de admin/roles.
├── MRC.Agendia.Application/
│   ├── Appointments/          ← CRUD + series recurrentes + delay + IAppointmentSchedulingValidator
│   │                            + IBookingConcurrencyGuard
│   ├── Auditing/ Authorization/ Availability/ Behaviors/
│   ├── Business/ Employees/ Services/ Schedules/ Holidays/  ← CRUD CQRS por feature
│   ├── Events/                ← IEventPublisher (enlista en el outbox, no hace Save)
│   ├── ServiceAuth/           ← client-credentials (AuthenticateServiceCommand + DTOs + puertos)
│   ├── Statistics/ Waitlist/ Common/ Mappings/
│   └── DependencyInjection.cs ← AddApplication() (auto-discovery MediatR/AutoMapper/FluentValidation)
├── MRC.Agendia.Domain/
│   ├── Common/                ← AuditableEntity (IAuditable + ISoftDelete)
│   ├── Constants/             ← Roles, RolePolicies, PaginationConstants, AuditActions,
│   │                            SupportedLanguages, SchedulingLimits
│   ├── Entities/              ← Business, Employee, Service, Appointment (+ ExtraServices),
│   │                            WaitlistEntry, ScheduleTemplate, ScheduleOverride,
│   │                            HolidayCalendar, AuditLog (Id long)
│   ├── Enums/ Exceptions/ Interfaces/ Services/ Statistics/
│   └── Events/                ← IIntegrationEvent + los 5 eventos (records inmutables)
├── MRC.Agendia.Infrastructure/
│   ├── AgendiaDbContext.cs    ← DbContext (Npgsql) + query filters + índices
│   ├── Auditing/ Authorization/ Caching/ Notifications/ (AppointmentReminderService)
│   ├── Idempotency/           ← IdempotencyRecord, IdempotencyStore, IdempotencyPurgeService
│   ├── Messaging/             ← OutboxMessage, OutboxEventPublisher, OutboxDispatcherService,
│   │                            IEventTransport, LoggingEventTransport
│   ├── Persistence/           ← AuditableSaveChangesInterceptor, BookingConcurrencyGuard
│   │                            (pg_advisory_xact_lock), UuidV7ValueGenerator
│   ├── Repositories/          ← RepositoryBase<T> + repos EF
│   ├── ServiceAuth/           ← ConfigurationServiceClientAuthenticator, JwtServiceTokenIssuer,
│   │                            ServiceClientSecretHasher (PBKDF2)
│   ├── Services/ Time/ (BusinessClock) UnitOfWork.cs
│   └── DependencyInjection.cs ← AddInfrastructure(config)
docs/
├── bounded-context.md        ← Propiedad de datos (perfil vs agenda), IDs sin FK, aprovisionamiento
├── harmony-token-contract.md ← Contrato del JWT Harmony → Agendia
├── events-contract.md        ← Contrato de eventos de integración (Agendia → consumidor)
├── service-auth-contract.md  ← Contrato del token M2M (client-credentials)
└── error-codes.md            ← Catálogo de códigos de error de la API
tests/
├── MRC.Agendia.Tests.Unit/       ← xUnit + NSubstitute + EF InMemory
└── MRC.Agendia.Tests.Integration/← WebApplicationFactory (InMemory) + PostgresWebApplicationFactory
                                    (API sobre Postgres real) + Testcontainers a pelo
deploy/
└── docker-compose.yml            ← Seq (5341) + RabbitMQ (15672, user/pass agendia) + Postgres (5433)
```

## Flujo de una petición

```
Request
   ↓
1. ForwardedHeaders     (solo si Environment != Development && != Testing)
2. CorrelationIdMiddleware (lee/genera X-Correlation-Id, lo fija como TraceIdentifier)
3. Swagger UI           (solo en Development)
4. HttpsRedirection     (skip en Testing)
5. CORS
6. RateLimiter          (skip en Testing)  ← solo protege el M2M service-token
7. ExceptionHandlingMiddleware
8. Authentication (JWT Bearer, valida el token de Harmony; un token ausente/invalido → 401 sin cuerpo)
9. Authorization
10. Controllers + HealthChecks
   ↓
Controller → MediatR.Send(Command/Query)
   ↓
ValidationBehavior (FluentValidation → 400 estructurado si falla)
   ↓
Handler → IResourceAuthorizationService.EnsureCan*Async(...)  ← auth por recurso
   ↓
Service → IAppointmentSchedulingValidator / IBookingConcurrencyGuard (en alta/reschedule de cita)
   ↓
Repository (EF Core / Npgsql) → PostgreSQL
```

## Convenciones — IMPORTANTES, no las rompas

1. **Una clase por archivo.** Cada `class`/`record`/`enum`/`interface` en su `.cs`.
2. **Records inmutables** para todos los DTOs, Commands, Queries y eventos.
3. **Naming:** `*Command`+`*CommandHandler`+`*CommandValidator`, `*Query`+…, `*Repository`+
   `I*Repository`, `*Service`+`I*Service`, `*Dto`/`Create*Dto`/`Update*Dto`. DTOs públicos:
   `*PublicDto`.
4. **`async`/`await`** en todo acceso a BD. Nunca `.Result` ni `.Wait()`.
5. **Validar autorización en handlers**, no en controllers: inyecta `IResourceAuthorizationService`
   y llama al `EnsureCan*Async` correspondiente ANTES de delegar. Excepción: listados Admin-only.
6. **Validar inputs con FluentValidation** (un Validator por Command/Query).
7. **Comentarios en inglés; mensajes runtime (validación/excepciones/logs) en español.**
8. **Combos de roles → `RolePolicies`** (`AdminOrOwner`, `Staff`, `AdminOrSelfClient`), no
   concatenar strings.
9. **DTOs de `Update`:** NUNCA incluir `BusinessId`, `OwnerUserId` ni `UserId` de un recurso
   scoped (vector cross-tenant/takeover). Si AutoMapper mapea, usa `.Ignore()` en el Profile;
   `ScheduleService` va campo a campo. **OJO FKs editables (Appointment):** `UpdateAppointment`
   sí cambia `ClientUserId/EmployeeId/ServiceId`, así que el handler re-autoriza el destino.
10. **Migraciones EF:** cada cambio de modelo con su migración, `--context AgendiaDbContext`.
    En Development se aplican **automáticamente** al arrancar.
11. **Sin secretos en `appsettings.json`.** `dotnet user-secrets` en dev, env en prod. El
    connection string de dev vive en `appsettings.Development.json` (Postgres 5433).
12. **Merge:** bugs/refactors/chores → `gh pr merge --admin`; features → PR y esperar al humano.
13. **Routing — 3 patrones (no refactorizar, front acoplado):**
    - Top-level singular `[Route("api/[controller]")]` → `/api/Business`, `/api/Service`…
    - Sub-recurso anidado `[Route("api/businesses/{businessId:guid}/<recurso>")]` → schedules,
      availability. **Las rutas `{businessId}` son `:guid`** desde la Fase 6.
    - Operación/agrupador lowercase → `AuthController` (`/api/auth/service-token`).
    ASP.NET routing es case-insensitive; la capitalización del archivo es lo que sale en Swagger/logs.
14. **Hora "ahora" en el flujo de citas → `IClock.BusinessNow`, NUNCA `DateTime.UtcNow`.** Las
    fechas de cita son hora de pared (zona `Scheduling:TimeZone`, default `Europe/Madrid`).
    `UtcNow` se reserva para instantes reales (audit, `CreatedAt`, `OccurredOnUtc`).
    **Y entran por el borde HTTP SIN zona** (#290): las fechas de pared que llegan del
    cliente (cita y time-off, body o query) se validan con `MustBeWallClock()`
    (`Application/Common/WallClockDateRules`), que exige `Kind=Unspecified` y rechaza `Z` y
    los offsets con 400 `VALIDATION_ERROR`. Sin esa regla, `Z` reventaba en Npgsql contra la
    columna `timestamp without time zone` y un offset se persistía **desplazado en
    silencio**. NO aplicarla a inputs que sí son instantes UTC (el filtro de audit-logs).
15. **Crear/reprogramar citas pasa por `IBookingConcurrencyGuard`** (envuelve validar+insertar
    en un `pg_advisory_xact_lock` keyed por empleado+día; la clave es un hash bigint FNV-1a).
16. **PK/FK son `Guid` (UUIDv7)** generados client-side en el `Add` por `UuidV7ValueGenerator`
    (ordenables por tiempo, buenos para índices). Excepción: `AuditLog.Id` es `long`.
    Validadores de id nullable con `NotEqual(Guid.Empty)`; en tests, helper `TestIds.Of(int)`.
17. **Firmas con 4+ parámetros → multilínea "paren-aligned"** (primer parámetro pegado al `(`,
    los demás alineados debajo; `)` pegado al último). Es lo que genera Visual Studio y respeta
    `dotnet format`. **Excepción: los records posicionales** van con salto-tras-`(`.

## Decisiones de diseño (no las cuestiones sin razón fuerte)

### Identidad y autorización
- **Agendia no autentica usuarios.** Valida los JWT de Harmony (HS256, clave simétrica
  compartida `Jwt:Key`; `iss`=`MRC.Agendia`, `aud`=`MRC.Agendia.Clients`; `MapInboundClaims=true`
  → `sub`→`NameIdentifier`, `role`→`Role`). Un token ausente/invalido lo rechaza el middleware
  JWT con 401 sin cuerpo. Contrato: [`docs/harmony-token-contract.md`](docs/harmony-token-contract.md).
- **El `sub` es clave de negocio:** se persiste en `Business.OwnerUserId`, `Employee.UserId`
  (opcional), `Appointment.ClientUserId`, `WaitlistEntry.ClientUserId`, `AuditLog.UserId`. Es
  inmutable de por vida (si cambia, el usuario pierde acceso a lo suyo).
- **El cliente no tiene entidad:** la cita guarda su `sub` directamente. No se aprovisiona.
- **Aprovisionamiento** (lo hace Harmony/gestión): `POST /api/Business` (Admin, con `OwnerUserId`),
  `POST /api/Employee` (Admin u owner, `UserId` opcional). Ningún `Update` acepta esos campos.
- **Auth M2M (client-credentials):** `POST /api/auth/service-token` → `{accessToken, expiresAt,
  tokenType}`, sin refresh. Cliente de confianza por **configuración** (`ServiceClients[]`),
  secreto **hasheado PBKDF2** con verificación en tiempo constante. El token lleva rol `Admin`
  (acceso transversal) + `token_use=service`. `401 INVALID_SERVICE_CREDENTIALS` (mensaje uniforme).
  Auditado (`SERVICE_TOKEN_ISSUED`/`SERVICE_TOKEN_DENIED`). Contrato:
  [`docs/service-auth-contract.md`](docs/service-auth-contract.md).
- **Authorization por recurso** vía `IResourceAuthorizationService` (impl en Infrastructure):
  lanza `UnauthorizedAccessException` → 403. No usamos `IAuthorizationHandler` de ASP.NET.
- **`ICurrentBusinessScope` + filtro global por negocio:** Owner/Employee ven solo lo suyo;
  Admin/M2M sin restricción; lecturas públicas con `IgnoreQueryFilters`.

### Notificaciones = eventos de integración (no las envía Agendia)
- Agendia **publica eventos** en un **outbox** (`OutboxMessage`) en la MISMA transacción que la
  operación que los origina. `OutboxDispatcherService` (background) los entrega por
  `IEventTransport`. **Hoy `LoggingEventTransport` (log-only)**; cuando se elija broker, se
  sustituye ese único registro en `Infrastructure/DependencyInjection` (publisher/outbox/dispatcher
  son agnósticos). Entrega **at-least-once** → el consumidor debe ser idempotente.
- 5 eventos: `AppointmentConfirmed`, `AppointmentCancelled`, `AppointmentReminder` (job 24h,
  idempotente por `ReminderSentAt`), `AppointmentDelayed`, `WaitlistSlotAvailable`. Llevan solo
  **ids + idioma**; el consumidor resuelve el contacto por `clientUserId`. Contrato:
  [`docs/events-contract.md`](docs/events-contract.md).
- **Eventos de dominio en el agregado (#263):** las entidades registran eventos con
  `Entity.RaiseEvent(...)` (interfaz `IHasDomainEvents`) al cambiar de estado; el **override de
  `SaveChanges` de `AgendiaDbContext`** los vuelca al outbox en la MISMA transacción que el
  cambio y los limpia. No hay publisher aparte. Las series emiten los eventos por ocurrencia.
- **El payload se construye desde la ENTIDAD, nunca releyendo la fila (#293).** El evento se
  levanta sobre la entidad *trackeada*, que ya tiene el estado nuevo, mientras la fila en disco
  sigue siendo la vieja (EF **no** vuelca los cambios pendientes antes de una query). De la BD
  se pide solo lo que no vive en la cita: negocio + idioma, vía
  `GetNotificationBusinessByEmployeeAsync(appointment.EmployeeId)`. Releer el contexto por
  `appointmentId` describía el estado ANTERIOR: mover una cita a otra empleada anunciaba la
  empleada de la que venía. Si añades un evento, cópialo de ahí y no de la fila.

### Citas y disponibilidad
- **`Employee.MaxConcurrentAppointments`** (default 1): modela capacidad (clase grupal, sala…).
- **`IAppointmentSchedulingValidator`** valida fechas, existencia, mismo negocio, duración =
  `service.DurationMinutes`, día abierto (`IScheduleResolver`), franja continua y capacidad no
  excedida (`AppointmentStatus.OccupiesCapacity()` = Pending|Confirmed). El negocio no debe estar
  soft-deleted.
- **`AvailabilityService`** calcula capacidad por slot y **omite huecos pasados** (`< BusinessNow`).
- **Anti doble-reserva:** `IBookingConcurrencyGuard` serializa validar+insertar con
  `pg_advisory_xact_lock` por empleado+día. En InMemory (tests) ejecuta directo.
- **Anti cita duplicada (#266):** cabecera **opcional** `Idempotency-Key` en `POST
  /api/Appointment` y `POST /api/Appointment/series` (`IdempotencyFilter` + `IIdempotencyStore`).
  La clave se **reclama antes** de ejecutar la acción (es la PK de `IdempotencyRecords`, así que
  el gemelo concurrente pierde el INSERT); un reintento idéntico **reproduce** la respuesta
  guardada, misma clave con otro cuerpo → 409, y un intento rechazado **libera** la clave. TTL
  por `IdempotencyOptions` + `IdempotencyPurgeService`. Sin cabecera no cambia nada (opt-in).
  Ojo: el guard evita el **overbooking**; esto evita la **cita duplicada** (doble submit).
- **Transiciones de estado:** un Client solo puede poner `Cancelled`; el resto es del personal.
  Cambiar el estado de una cita terminal (Completed/NoShow/Cancelled) → 400
  `INVALID_APPOINTMENT_STATUS_TRANSITION`.
- **Analítica de utilización (#269):** `GET /api/businesses/{id}/stats/utilization?from&to`
  (Staff, rango ≤ 92 días) → ocupación global, por hora, por día de la semana y por empleado,
  más el lead time medio. **La unidad es el minuto de agenda**: ofertado = minutos abiertos del
  horario efectivo × capacidad del empleado − su time-off; reservado = minutos de citas no
  canceladas (un no-show ocupó la agenda igual). `UtilizationCalculator` es puro. El lead time
  cruza los dos mundos de tiempo: `IClock.ToBusinessTime` pasa el `CreatedAt` (UTC) a hora de
  pared antes de restarlo del `StartDate`.
- **Multiservicio (aditivo):** `Appointment.ServiceId` principal + colección `ExtraServices`
  (duración total = suma). Aditivo para no romper el front. Stats cuenta solo el principal.
- **Series recurrentes:** materializa una `Appointment` por ocurrencia (reusa validador+guard),
  comparten `SeriesId`; "saltar y avisar" en choques de fecha; gestión por serie (cancelar/mover/
  borrar futuras). En creación masiva no se publica evento por cita (solo el recordatorio 24h).
  El **estado inicial** es el `DefaultAppointmentStatus` del negocio, igual que el alta
  individual, resuelto una sola vez para toda la serie (#294).
  **Qué salta y qué aborta (#291):** cada ocurrencia commitea en su propia transacción, así que
  `RecurringAppointmentService.IsRequestLevel` enumera lo que tumba la petición entera (404,
  empleado inactivo, mismatch de negocio, duración) y **todo lo demás se salta y se reporta**.
  Si añades una excepción propia de UNA fecha, no toques nada: ya degrada a skip. Si añades una
  de nivel petición, métela en esa lista o se reportará N veces como skip.
- **Cancelación self-service:** `Business.CancellationWindowHours` (null = sin restricción); un
  Client no cancela/reprograma dentro de la ventana → 400 `CANCELLATION_WINDOW_ELAPSED`.
- **Política por tramos (#270), aditiva:** un negocio puede definir `CancellationPolicyTier`s
  (`MinHoursBefore` + `PenaltyKind` None/Percentage/FixedAmount/NotAllowed) vía
  `GET|PUT /api/businesses/{id}/cancellation-policy` (PUT = reemplazo completo, Owner/Admin).
  **Si hay tramos mandan los tramos; si no, sigue `CancellationWindowHours`** (el front viejo no
  se entera). El validador exige un tramo de 0h (así todo momento cae en uno) y umbrales únicos.
  El tramo `NotAllowed` lanza el mismo `CANCELLATION_WINDOW_ELAPSED` de siempre; los demás
  **permiten** cancelar y devuelven el tramo aplicado en `AppointmentDto.AppliedCancellationTier`.
  **Agendia NO cobra la penalización**: solo expone la regla (el dinero es de gestión/pagos, #172).
- **Time-off de empleado (#271):** `EmployeeTimeOff` (rango **hora de pared**, semiabierto
  `[Start, End)`) bloquea a **un** empleado sin tocar la plantilla anual:
  `GET|POST /api/employees/{employeeId:guid}/time-off` + `DELETE .../{timeOffId:guid}` (Staff).
  `AvailabilityService` lo descuenta (también en `GetSlotCapacityAsync`, que usa la waitlist) y
  `AppointmentSchedulingValidator` lo rechaza → 400 `EMPLOYEE_UNAVAILABLE`. El bloqueo saca al
  empleado **entero** del rango aunque tenga `MaxConcurrentAppointments > 1`. Las citas ya
  reservadas dentro **no se tocan**: se devuelven en `collidingAppointmentIds` al crear el bloqueo.
- **Reserva prioritaria de la waitlist (#268):** al avisar al primero de la cola se le da un
  **hold** (`WaitlistEntry.HoldUntil`, UTC, `Waitlist:HoldMinutes` default 15). Mientras dura,
  `AvailabilityService` no ofrece la franja a nadie más (sí al titular) y el validador rechaza a
  terceros con 400 `SLOT_ON_HOLD`. Si el titular reserva, `ConsumeHoldAsync` cierra la entrada
  (`Booked`); si no, `WaitlistHoldExpiryService`/`WaitlistHoldProcessor` la marca `Expired` y
  pasa el turno al siguiente (FIFO), todo dentro del `IBookingConcurrencyGuard`.
- **Lista de espera:** apuntarse a una franja completa; al liberarse un hueco, aviso FIFO
  (evento `WaitlistSlotAvailable`) tras re-chequear capacidad, serializado por el guard. Índice
  único filtrado `IX_WaitlistEntry_UniqueWaiting` con `NULLS NOT DISTINCT`.

### Horarios
- **One Business → many ScheduleTemplates** sin solape en fechas; el efectivo es por fecha.
- **`ScheduleOverride`** prevalece sobre la plantilla para un día concreto (único por fecha).
- **`IScheduleResolver`** es la fuente de verdad de "qué horario aplica el día X" (`Resolve`
  puro + `ResolveRange`). Úsalo, no reimplementes.
- **Generación anual** en `IScheduleGenerationService` (deduplica overrides con un `HashSet`;
  festivos/vacaciones/cierres). **Preview** sin persistir. Tie-break de plantilla: `IsDefault`
  gana (en resolver, repo y decorador de caché).
- **Caché** (`IMemoryCache`) de festivos/año y plantillas/negocio (decoradores).
- **Cap de fechas** `Domain/Constants/SchedulingLimits` (2000-01-01..2100-12-31) en los
  validadores de rango (evita overflow de `DateOnly.AddDays` → 400, no 500).

### Persistencia
- **GUID UUIDv7** client-side (`UuidV7ValueGenerator` en el `Add`). `AuditLog.Id` = `long`.
- **Soft delete + audit fields** (`AuditableEntity`) en Business, Employee, Service, Appointment,
  WaitlistEntry: `AuditableSaveChangesInterceptor` rellena audit fields y convierte `Delete` en
  soft delete; global query filters `!IsDeleted`; `POST /api/{recurso}/{id}/restore` (Admin).
  **Sin cascada, se conserva el historial.** Las lecturas que cargan padres usan
  `IgnoreQueryFilters()` + `Where(!IsDeleted)` para no descartar la cita por un padre soft-deleted.
  **Restaurar una CITA valida capacidad** (#294): si es futura y sigue ocupando plaza
  (Pending/Confirmed), la comprobación va dentro del `IBookingConcurrencyGuard` y devuelve 400
  `APPOINTMENT_CONFLICT` cuando la franja se ocupó mientras estaba borrada. Una cita pasada o en
  estado terminal vuelve tal cual: no puede provocar overbooking.
- **`RepositoryBase<T>`** centraliza el CRUD plano; los repos lo heredan. Preservar la semántica
  (FindAsync, AsNoTracking, IgnoreQueryFilters) al tocarlo.
- **`FindAsync` aplica los query filters** (decisión aceptada; `GetByIdAsync` devuelve null para
  soft-deleted — correcto).

### Errores
- Jerarquía `DomainException` (400, con `Code`) / `NotFoundException` (404) + concretas.
  `ExceptionHandlingMiddleware` mapea por tipo. Respuesta `{ code, message, traceId, [errors] }`,
  `traceId` == correlation id. **Mensajes en español; `code` estable.** Catálogo:
  [`docs/error-codes.md`](docs/error-codes.md) — mantenerlo al día.

### Otros transversales
- **CORS** por `Cors:AllowedOrigins`; fail-fast si vacío fuera de Dev/Testing.
- **Rate limiting** solo aplica al M2M service-token (auth de usuario no existe). Skip en Testing.
- **ForwardedHeaders** solo fuera de Dev/Testing (IP real tras proxy).
- **Health checks:** `/health`, `/health/ready` (Postgres+Seq), `/health/live`, `/health-ui`
  (solo Dev). Cuerpo detallado solo en Development.
- **Correlation ID:** `X-Correlation-Id` (cap 64, charset seguro anti log-forging).
- **CancellationToken** propagado handlers → services → repos → EF.
- **Auditoría** (`IAuditLogger`, best-effort, tras persistir): login/estado de cita/horarios/
  service-token… `GET /api/admin/audit-logs` (Admin, con filtros).

### Estrategia de tests (#272)

Tres niveles, de más rápido a más fiel. **Elige el más barato que cubra el riesgo:**

| Nivel | Cómo | Para qué |
|---|---|---|
| Unit | xUnit + NSubstitute (+ EF InMemory) | Lógica de handlers/servicios/validadores. |
| Full-stack InMemory | `CustomWebApplicationFactory` | **Camino por defecto** del grueso de los tests de API: rutas, auth, códigos de error, flujos. |
| Full-stack Postgres real | `PostgresWebApplicationFactory` (`[Collection(PostgresApiCollection.Name)]`) | Solo lo que InMemory **no puede** ver: tipos de columna (hora de pared), constraints/índices únicos, transacciones, `pg_advisory_xact_lock`, escritura del outbox. |
| `DbContext` + Postgres real | `PostgresContainerFixture` (`PostgresCollection`) | Persistencia/concurrencia sin necesidad de pasar por la API. |

- **No migres la suite entera a Postgres real:** es más lenta y no aporta nada donde no hay
  semántica de BD en juego.
- **Aislamiento:** un contenedor por colección; cada test empieza con
  `await _factory.ResetDatabaseAsync()` → `TRUNCATE` de todas las tablas **derivadas del modelo
  EF** (`PostgresDatabaseReset`), conservando esquema e historial de migraciones. Las dos
  colecciones Postgres van separadas para que el truncate de una no borre datos de la otra
  (xUnit serializa clases dentro de una colección, pero paraleliza colecciones).
- La factory de Postgres **quita los hosted services** (outbox dispatcher y reminder): si no,
  entregarían/marcarían las filas del outbox que el test va a comprobar y consultarían tablas
  mientras otro test las trunca. Su lógica tiene tests propios.
- Todos los tests de Postgres son `[SkippableFact]` con `Skip.IfNot(available, ...)`: **sin
  Docker se omiten**, no fallan.

### Advisories de seguridad aceptados
`Directory.Build.props` suprime **solo** 2 advisories (con `NuGetAuditSuppress`, cualquier otro
nuevo sigue saltando): **AutoMapper** (GHSA-rvv3-g6hj-g44x, DoS por recursión ~25k — no alcanzable:
JSON `MaxDepth` 64 + profiles estáticos) y **KubernetesClient** (transitivo de HealthChecks.UI,
cuyo dashboard solo se monta en Dev). Rationale en el comentario del fichero y en la memoria
`project_accepted_audit_decisions.md`. No re-levantar en re-auditorías.

## Roles del sistema (vienen en el JWT de Harmony)

| Rol | Quién |
|-----|-------|
| `Admin` | Super-usuario / servicio M2M (acceso transversal a todos los negocios). |
| `BusinessOwner` | Dueño de un negocio. |
| `Employee` | Trabajador/recurso de un negocio. |
| `Client` | Cliente final (identificado por su `sub`; sin entidad en Agendia). |

Combos en `[Authorize(Roles = ...)]` → `RolePolicies.{AdminOrOwner, Staff, AdminOrSelfClient}`.

## Comandos típicos

```bash
# Build (0 errores, 0 warnings; Release usa -warnaserror)
dotnet build

# Tests
dotnet test

# Infra local (Postgres 5433, Seq 5341, RabbitMQ 15672 user/pass agendia)
cd deploy && docker compose up -d

# Migracion nueva (OJO: ruta completa del tool + --context)
"C:/Users/marco/.dotnet/tools/dotnet-ef.exe" migrations add NombreMigracion \
  --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api \
  --output-dir Migrations --context AgendiaDbContext

# Quitar la ultima migracion (antes de pushear)
"C:/Users/marco/.dotnet/tools/dotnet-ef.exe" migrations remove \
  --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api --context AgendiaDbContext

# Lanzar la API (en Development arranca + auto-migra)
dotnet run --project src/MRC.Agendia.Api

# Secretos de dev (Jwt:Key compartido con Harmony; ServiceClients para M2M)
cd src/MRC.Agendia.Api && dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
```

## Workflow al implementar una issue

1. `git checkout master && git pull origin master`
2. Rama `<num>-<slug-corto>` (ej. `248-fase7-docs-limpieza`). **Nunca** `claude/xxx`.
3. Implementar según convenciones.
4. `dotnet build` sin warnings. Tras crear ficheros, `dotnet format` (el `.editorconfig` exige
   CRLF; los nuevos suelen salir con LF).
5. `dotnet test` sin regresiones.
6. Commit en español sin tildes, **sin trailer de atribución a Claude**:
   `<tipo>: <descripcion>\n\nCloses #<num>\n\n<detalle>` (tipos: feat/fix/refactor/chore/test/docs).
7. Push + `gh pr create --base master` (sin footer "Generated with Claude Code").
8. Merge: bug/refactor/chore/docs → revisar con agentes → `gh pr merge <num> --admin --merge
   --delete-branch`; feature → esperar al humano.

## Cosas que NO hacer sin permiso explícito

- ❌ Mergear a `master` una **feature** sin revisión humana (bugs/refactors/chores sí, con `--admin`).
- ❌ `git push --force`/`--force-with-lease`, `git reset --hard`, `git clean -f` (bloqueados).
- ❌ `dotnet ef database drop` / borrar volúmenes de Docker (`docker volume rm`, `compose down -v`).
- ❌ Cambiar la arquitectura (capas, MediatR, provider) sin discutirlo.
- ❌ Reintroducir auth de usuario/Identity/tablas `AspNet*` (eso es Harmony).
- ❌ Volver a enviar email/push desde Agendia (eso es el consumidor de eventos).
- ❌ Añadir a `Update*Dto` campos `BusinessId`/`OwnerUserId`/`UserId` de un recurso scoped.
- ❌ Concatenar `Roles.X + "," + Roles.Y` en `[Authorize]` (usa `RolePolicies`).
- ❌ Refactor de rutas singular/plural sin coordinar con el front.
- ❌ Modificar `appsettings.json` con valores reales (solo placeholders).
- ❌ Subir la pila EF por encima de 9.0.0 sin resolver el conflicto de HealthChecks.UI/`-warnaserror`.

## Backlog / fuera de scope

- **#185** — cablear proveedor real de push (FCM): descartado en este modelo (las notificaciones
  las entrega el consumidor de eventos, no Agendia). El broker real de `IEventTransport` está por
  decidir.
- **#172** (pagos/depósito) y **#173** (Verifactu): discovery/futuro, no construir.
- **Cloud secret manager** para producción: aparcado hasta decidir cloud (`project_prod_secrets.md`).
- **Refactor "Resource"** (salas/equipos abstractos): hoy `Employee + MaxConcurrentAppointments`
  cubre los casos; solo si surge necesidad.

## Si te quedas sin contexto / dudas

- **Propiedad de datos / límites del servicio:** [`docs/bounded-context.md`](docs/bounded-context.md).
- **Contrato del token de Harmony:** [`docs/harmony-token-contract.md`](docs/harmony-token-contract.md).
- **Eventos de integración:** [`docs/events-contract.md`](docs/events-contract.md).
- **Sistema de horarios:** `src/MRC.Agendia.Infrastructure/Services/ScheduleResolver.cs`.
- **Auth por recurso:** `src/MRC.Agendia.Infrastructure/Authorization/ResourceAuthorizationService.cs`.
- **Validación de citas:** `src/MRC.Agendia.Application/Appointments/AppointmentSchedulingValidator.cs`.
- **Disponibilidad:** `src/MRC.Agendia.Application/Availability/AvailabilityService.cs`.
- **Mapeo de excepciones:** `src/MRC.Agendia.Api/Middleware/ExceptionHandlingMiddleware.cs`.
- **Outbox/eventos:** `src/MRC.Agendia.Infrastructure/Messaging/`.
- **Soft delete + audit:** `src/MRC.Agendia.Infrastructure/Persistence/AuditableSaveChangesInterceptor.cs`.

Si una decisión de diseño no es obvia, **pregúntale al usuario antes de codificar.**
