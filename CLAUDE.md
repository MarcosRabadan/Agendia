# Claude project notes — MRC.Agendia

Este fichero se carga **automáticamente** al inicio de cada sesión de Claude Code.
Lee con atención antes de tocar código.

> 📌 **Última actualización: 2026-05-29 (6ª sesión).** **Auditoría exhaustiva multi-agente (11 revisores por área + verificadores adversariales) → 12 issues creadas (#187–#198) y 7 fixes MERGEADOS a master** (cada uno: build limpio + suite verde + verificado + `--admin`). **~248 tests verdes.** Antes de la auditoría, el usuario mergeó **#183 (push núcleo, #51 → CERRADA;** envío FCM real queda en **#185)** y **#186 (#55 caching + #58 filtro global por negocio).**
> - **Estado de los 12 de la auditoría:** ✅ **CERRADAS:** #187, #188, #189, #190 (HIGH) · #191, #192 (MEDIUM). 🟡 **#193 PARCIAL** (mergeada la parte de capacidad; faltan 2 partes de concurrencia). ⬜ **ABIERTAS:** #193(resto), #194, #195, #196, #197, #198.
> - **#187 — Registro transaccional (HIGH, PR #199):** `UserRegistrationService` envuelve crear-usuario+rol+entidad+sesión en una transacción relacional (`RunInTransactionAsync`, gateada por `IsRelational()`; InMemory ejecuta directo). Antes `UserManager.CreateAsync` commiteaba el usuario y un fallo posterior dejaba cuentas **huérfanas**.
> - **#188 — Dedup de overrides en generación (HIGH, PR #201):** `ScheduleGenerationService.BuildAsync` deduplica TODAS las fechas con un `HashSet claimedDates` sembrado con los overrides existentes del año (skip+warning). Antes: vacaciones solapadas / re-ejecutar / cierre repetido → 2 overrides mismo `(BusinessId,Date)` → viola el índice único → **HTTP 500 (solo en SQL real; EF InMemory no impone el índice → la suite verde no lo cazaba, ver #198).**
> - **#189 — Waitlist no pierde avisos (HIGH, PR #200):** `NotifyForFreedAppointmentAsync` **envía y solo entonces marca `Notified`** (antes marcaba antes de enviar y tragaba el fallo → aviso perdido sin reintento).
> - **#190 — Deps (HIGH, PR #202/#203):** EF Core/Identity/Extensions/InMemory **8.0.x → 9.0.0** (alinea net9; resuelve transitivamente la vuln **High de System.Text.Json**). Fix de infra de tests: EF9 obliga a quitar también `IDbContextOptionsConfiguration<AgendiaDbContext>` del SQL Server en `CustomWebApplicationFactory`. **AutoMapper (GHSA-rvv3-g6hj-g44x, High) y KubernetesClient (Moderate) ACEPTADOS+documentados** (sin arreglo gratuito / solo-Dev) y **suprimidos puntualmente con `NuGetAuditSuppress` en `Directory.Build.props`** (decisión del owner; rationale completo en el comentario de `Directory.Build.props` y en la memoria `project_accepted_audit_decisions.md`). El advisory de AutoMapper (DoS por recursión ~25k niveles) **no es alcanzable** aquí: JSON `MaxDepth` 64 + profiles estáticos.
> - **#191 — Máquina de estados de cita (MEDIUM, PR #204):** `AppointmentStatus.IsTerminal()` + guard en `UpdateAsync`: cambiar el estado de una cita ya `Completed/NoShow/Cancelled` lanza `InvalidAppointmentStatusTransitionException` (400, código nuevo `INVALID_APPOINTMENT_STATUS_TRANSITION`). Un cambio sin tocar el estado (notas/reprogramar) sigue permitido.
> - **#192 — Idempotencia de recordatorios (MEDIUM, PR #205):** `AppointmentReminderService` hace `SaveChanges` **por cita** (no tras el bucle) → un crash a mitad ya no reenvía. Scale-out multi-instancia documentado como hardening futuro (RowVersion/claim atómico; single-instance hoy).
> - **#193 — Waitlist re-chequea capacidad (PARCIAL, PR #206):** el trigger re-consulta `GetSlotCapacityAsync` y solo avisa si capacidad > 0 (evita el falso "hay hueco" con "cualquier empleado"/`MaxConcurrent>1`). **Quedan abiertas** las partes de concurrencia (serializar el trigger, índice único de join).
> - **NO construir sin tu decisión (backlog abierto):** **#194** (audit-log de serie skip-only + colisión `MoveSeries`), **#195** (bundle auth/crypto: **hashear refresh tokens** —migración—, colación del token, JWT `ValidAlgorithms`, ConfirmEmail IsActive, entropía `Jwt:Key`), **#196** (bundle SMTP: timeout, fail-fast user/pass, componer fuera del try), **#197** (bundle API/persistencia: `OperationCanceled`→499, sanear correlation-id, `:int` en rutas, `CreatedAt.IsModified=false`, **FK WaitlistEntry** —migración—, tie-break de plantilla, solape de franjas), **#198** (META: capa de integración contra **SQL Server real** —Testcontainers/LocalDB— porque EF InMemory no impone constraints; + DataProtection efímero para CI).
> - **Sin migraciones nuevas esta sesión** (los fixes fueron código/config). **Codex refutado:** los "tests de integración rotos" que reportó eran artefacto de su sandbox (sin permisos para `DataProtection-Keys`); en esta máquina la suite está **verde**.

> 📌 **Última actualización: 2026-05-29 (5ª sesión).** **Backlog de features prácticamente cerrado.** #167/#168/#169/#174 + **#171 + #170** MERGEADAS a master; **#51 (push) en PR #183** (núcleo, pendiente de tu review); **#172 (pagos) y #173 (Verifactu) a futuro — NO construir**. **232 tests verdes** (156 unit + 75 integration + 1 placeholder); build limpio.
> - **#171 — Cancelación/reprogramación self-service (PR #181, MERGEADA):** `Business.CancellationWindowHours` (`int?`, null = sin restricción) + migración `AddBusinessCancellationWindowHours`. Un Client no puede cancelar/reprogramar/borrar su cita dentro de la ventana del negocio (medida contra el inicio **actual** de la cita con `IClock.BusinessNow`); el personal nunca la sufre. Regla pura `AppointmentCancellationPolicy`; reutiliza el disparador de lista de espera (#167). Nuevo código `CANCELLATION_WINDOW_ELAPSED` (400). Validador 1..8760h; expuesto en `BusinessDto`/`BusinessPublicDto`.
> - **#170 — Reserva multiservicio (PR #182, MERGEADA, enfoque ADITIVO):** `Appointment.ServiceId` sigue siendo el **principal** + colección `ExtraServices` (entidad `AppointmentExtraService`, no soft-deletable; migración `AddAppointmentExtraServices`). Duración total = suma; disponibilidad (`extraServiceIds` opcional) y validador la usan. **Aditivo a propósito para NO romper el contrato del front** (una cita de 1 servicio se comporta igual que antes). `AppointmentDto.ExtraServiceIds` (vía `ForCtorParam`, por ser record). Rechaza extras duplicados o iguales al principal. **Límites v1:** stats (#169) cuenta ingresos solo del servicio **principal** (extras no suman); series (#174) y lista de espera (#167) siguen single-service; no se expone `TotalPrice` en servidor.
> - **#51 — Notificaciones push, NÚCLEO agnóstico (PR #183, ABIERTA — pendiente de tu review):** `DeviceToken` + enum `DevicePlatform` + `IDeviceTokenRepository` + migración `AddDeviceTokens`; `IPushSender` (espeja `IEmailSender`) + `LoggingPushSender`; CQRS register/remove + `POST`/`DELETE /api/notifications/device-tokens` (autenticado, gestionas solo los tuyos); `NotificationService` envía push **best-effort** junto al email. **AÚN NO envía push real** (sin proveedor): cablear FCM en `PushSetup` (credenciales por entorno + fail-fast en prod, como `EmailSetup`); no verificable sin un proyecto real. Feature → **tu revisión**; **NO cierra #51** (queda el envío real). Detalle en "Notificaciones".
>
> ⚠️ **Incidente menor de la 5ª sesión:** un agente revisor con acceso a git (sin aislar) ejecutó `git checkout master` + `pull` en el árbol compartido y, al limpiar, **descartó tu cambio local sin commitear de `appsettings.Development.json`** (no quedó en stash). Es config de dev; vuelve a aplicar tus valores locales. Para próximas revisiones, aislar los agentes (worktree) o darles git solo-lectura.

> 📌 **Última actualización: 2026-05-29 (4ª sesión).** **4 features nuevas implementadas y revisadas con multi-agente:** 3 MERGEADAS a master (#174, #168, #169) y **#167 en PR #179** pendiente de review/merge. Build limpio; suite verde.
> - **#174 — Reservas recurrentes / serie de citas (PR #175, MERGEADA):** el personal crea en masa una serie ("todos los viernes a las 16h hasta una fecha") y la gestiona por `SeriesId` (cancelar/reprogramar/borrar). Materializa una `Appointment` por ocurrencia reutilizando validador + booking guard; "saltar y avisar" en choques. Detalle en "Lo que SÍ funciona" + nota de diseño en "Citas y disponibilidad".
> - **#168 — Alerta de retraso (PR #176, MERGEADA):** `POST /api/businesses/{id}/notify-delay` (Staff) avisa solo a los clientes con cita futura del mismo tramo (respeta turno partido vía `IScheduleResolver`); email best-effort, no reescribe horas.
> - **#169 — Panel de estadísticas (PR #177, MERGEADA):** `GET /api/businesses/{id}/stats?from=&to=` (Owner/Admin); proyección server-side filtrada por negocio+rango + `BusinessStatsCalculator` puro (agrega en memoria para evitar GroupBy frágiles en SQL Server).
> - **#167 — Lista de espera para huecos completos (PR #179, MERGEADA).** Construida en la rama `167-lista-de-espera` (sincronizada con master): `WaitlistEntry` + enum `WaitlistStatus` + migración `AddWaitlist`; `IWaitlistRepository`/`WaitlistRepository`; `WaitlistService` (apuntarse/baja/listar + trigger FIFO `NotifyForFreedAppointmentAsync`, enganchado **best-effort** en `AppointmentService.UpdateAsync`→Cancelled y `DeleteAsync` solo si la cita ocupaba hueco); `INotificationService.SendWaitlistAvailabilityAsync`; `IAvailabilityService.GetSlotCapacityAsync` (reutilizado para validar "franja completa" al apuntarse); CQRS + endpoints `POST` / `GET me` / `DELETE /api/waitlist` (rol Client). **196 tests verdes; build limpio (warning corregido).** Feature nueva → **tu revisión**, no auto-merge.

> 📌 **Última actualización mayor: 2026-05-23 (3ª sesión).** **Re-auditoría multi-agente completa** (10 revisores en paralelo + 4 verificadores + re-review por PR) y cierre de **TODO el punch list** de la auditoría. 14 PRs mergeados a master: #139, #141, #143, #145, #147, #149, #151, #153, #155, #157, #159, #161, #163, #165. **146/146 tests verdes** (1 placeholder + 97 unit + 48 integration). Resumen por área:
> - **Rendimiento:** N+1 del calendario eliminado (nuevo `IScheduleResolver.ResolveRange`), conteo de capacidad en servidor (`CountOverlappingForEmployeeAsync`), `AsNoTracking` en lecturas puras, `Include` innecesarios fuera, índice `IX_Appointment_StartDate` (migración `AddAppointmentStartDateIndex`).
> - **Concurrencia (BIZ-02):** doble-reserva por carrera cerrada con un lock de aplicación de SQL Server (`sp_getapplock`) vía `IBookingConcurrencyGuard`, keyed por empleado+día.
> - **Zona horaria (BIZ-01):** "ahora" coherente con la hora de pared de las citas: `IClock.BusinessNow` (zona configurable `Scheduling:TimeZone`, default `Europe/Madrid`).
> - **Bugs de citas:** marcar cita pasada Completed/NoShow (SVC-01), recordatorio ignora participantes soft-deleted (BIZ-03), override duplicado → 400 (BIZ-05), festivo `Year==Date.Year` (APP-02), disponibilidad sin huecos pasados (BIZ-06), listado por rango con **solape** (BIZ-04).
> - **Seguridad:** refresh reuse-detection (SEC-01), rate-limit en reset/confirm (SEC-03), `BusinessId` fuera de `UpdateServiceDto` (SEC-04), logout solo del propio token (SEC-05), transiciones de estado por rol — Client solo `Cancelled` (SVC-02), registro sin sesión antes de confirmar email (IDN-02), forgot-password en tiempo constante (SEC-02), `/health` sin detalle a anónimos en prod (API-01).
> - **Limpieza:** código muerto eliminado, validators compartidos, enums con valor explícito (DOM-01), fail-fast en crash de arranque (API-03).
>
> _2ª sesión (histórico):_ #52 (soft delete + audit fields) + auditoría inicial (#125/#127/#128/#129/#133/#135).
>
> ⚠️ **POLÍTICA DE TRABAJO (vigente):** las **correcciones de bugs y refactors** se hacen en **automático de principio a fin (incluido mergear a master con `gh pr merge --admin`)**; las **features nuevas** sí pasan por PR para revisión humana. Lee **"Política de trabajo y autonomía"** ANTES de nada. **Backlog (5ª sesión):** features cerradas salvo el **push real** (#51, núcleo en PR #183); abiertas solo low-priority **#55** y **#58** (ambas desaconsejadas sin métricas/incidente) y **#172/#173 a futuro**. El punch list de la auditoría está **CERRADO** (decisiones aceptadas en la memoria `project_accepted_audit_decisions.md`).

## Política de trabajo y autonomía (LEE ESTO PRIMERO)

Instrucciones explícitas del usuario (2026-05-23, sustituyen al workflow clásico de "una PR cada vez, solo el humano mergea"):

- **Bugs y refactorizaciones → AUTOMÁTICO de principio a fin.** Crear issue/tareas, implementar, **revisar con varios agentes (varias veces)**, build/test/format verdes, y **mergear a master uno mismo** con `"/c/Program Files/GitHub CLI/gh.exe" pr merge <num> --admin --merge --delete-branch` (el `--admin` salta la branch protection; la gh CLI está autenticada como el owner). NO esperar revisión humana.
- **Features nuevas (funcionalidad nueva) → PR para el humano.** Crear la PR contra master y **NO** auto-mergear; el usuario la revisa.
- **Analizar las decisiones con agentes varias veces** aunque tarde más, porque al usuario le preocupa introducir bugs. Antes de auto-mergear un bug/refactor, lanzar agentes verificadores que confirmen que el cambio es correcto, no rompe flujos y no añade código innecesario.
- **Re-auditar el código periódicamente** con muchos agentes (reviewers + verificadores en varias rondas) y corregir en automático lo que aparezca (bug/refactor). Patrón usado: 4-6 reviewers por área + 2-3 verificadores que auditan los hallazgos de los reviewers (descartan falsos positivos).
- **Reutilizar código** siempre que se pueda y **no dejar código innecesario** (pero sin abstracciones prematuras: 3 líneas repetidas no justifican una abstracción).
- **Checkpoints que SÍ se conservan (preguntar al usuario):** (1) **decisiones** de producto/negocio, (2) **confirmar creación de issues**, (3) **crear PR de features nuevas**.
- **`docs/error-codes.md` se mantiene al día** con cada excepción nueva.
- **Decisiones de auditoría ya aceptadas** (no re-levantar en futuras re-auditorías): `FindAsync` SÍ aplica los query filters; soft-delete sin cascada + conservar historial; Schedule/Holiday NO soft-deletable; sin validators base genéricos; los GET de horarios cross-tenant (`ScheduleController`) son aceptables (cualquier autenticado puede ver el calendario); modelo de dominio anémico no se rehace.

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
- **Entity Framework Core 9.0.x** + SQL Server (LocalDB en Dev) — alineado con net9 en la 6ª sesión (antes 8.0.11)
- **ASP.NET Identity** + **JWT Bearer** + Refresh tokens con rotación
- **FluentValidation** con `ValidationBehavior` de MediatR (corre antes del handler)
- **Rate limiting** built-in de ASP.NET en endpoints de auth
- **Serilog + Seq** (`http://localhost:5341`) para logging
- **Swagger / OpenAPI** documentando todo
- **xUnit + NSubstitute + EF InMemory** en tests unitarios
- **xUnit + WebApplicationFactory<Program>** en tests integration

## Estado actual del proyecto (2026-05-23)

> Snapshot tras una sesión muy larga de cierre de backlog. La aplicación está **funcional, testeada y razonablemente pulida** para un MVP de un solo dev.

### Lo que SÍ funciona end-to-end

- ✅ **Auth completa**: registro de Client (público), registro de Owner (público desde #82), registro de Employee (Owner crea sus empleados), login, refresh con rotación, logout, change password, `GET /me`. Lockout tras 5 fallos. Rate limit en `/api/auth/*`.
- ✅ **Reset password + confirmación de email** (#57): `forgot-password` (anti-enumeración, siempre 204), `reset-password`, `confirm-email`. El registro envía email de confirmación. Token de reset 1h, confirmación 24h (configurables). Flag `Auth:RequireConfirmedEmail` (default false) bloquea login sin confirmar. Email vía `IEmailSender` (SMTP genérico en prod, Logging en Dev/Test).
- ✅ **CRUD completo** de Business, Client, Employee, Service, Appointment, Holiday.
- ✅ **Sistema de horarios** (Schedule): templates anuales, overrides por día, festivos, vacaciones, turnos partidos.
- ✅ **Disponibilidad** (`GET /api/businesses/{id}/availability`): calcula huecos libres con capacidad por empleado.
- ✅ **Panel de estadísticas del negocio (#169):** `GET /api/businesses/{id}/stats?from=&to=` (Owner/Admin) devuelve reservas por mes y semana (ISO), servicios más/menos usados (con ingresos), no-shows y cancelaciones (conteo + %), e ingresos por hora y por día de la semana. Solo lectura. La query trae una **proyección filtrada por negocio+rango** (server-side, `AsNoTracking`) y un **calculador puro** (`BusinessStatsCalculator`) agrega en memoria (evita `GroupBy` por día-de-semana/semana que traducen mal en SQL Server). "Reserva" = Pending+Confirmed+Completed; "ingreso" = solo Completed × precio **actual** del servicio (sin snapshot histórico, v1).
- ✅ **Capacidad por empleado** (`Employee.MaxConcurrentAppointments`): clase grupal, peluquera con tinte, etc.
- ✅ **Reservas recurrentes / serie de citas (#174):** el personal crea en masa una serie ("todos los viernes a las 16h hasta una fecha") con `POST /api/Appointment/series`; reutiliza el validador, la capacidad y el guard anti-doble-reserva **por ocurrencia**. Las que chocan (festivo, lleno, pasado) se **omiten y se informan** (no aborta). Gestión por `SeriesId`: cancelar (`/series/{id}/cancel`), reprogramar (`/series/{id}/move`) y borrar (`DELETE /series/{id}`). Patrones: semanal (varios días), quincenal (`Interval`) y mensual por día del mes. Staff-only.
- ✅ **Alerta de retraso en tiempo real (#168):** el personal avisa con `POST /api/businesses/{id}/notify-delay` (minutos + alcance negocio/empleado + límite opcional) a los clientes con cita **futura del mismo tramo** (usa `IScheduleResolver`; un retraso de mañana NO llega a la tarde por el descanso del turno partido). Solo cuenta `BusinessNow` en adelante, excluye soft-deleted/inactivos (BIZ-03), email best-effort. Staff-only. No reescribe las horas de las citas (v1).
- ✅ **Validación de citas**: `AppointmentSchedulingValidator` chequea horario, conflictos, capacidad, etc.
- ✅ **Auto-creación de Employee al registrar Owner**: un autónomo opera al instante sin pasos extra.
- ✅ **`BusinessPublicDto`** para `GET /api/business` anónimo (sin email, filtra inactivos).
- ✅ **Logout-all** (#59): `POST /api/auth/logout-all` revoca todos los refresh tokens; change/reset password también los revocan.
- ✅ **Preview de schedule** (#54): `POST /api/businesses/{id}/schedules/preview` devuelve el calendario anual resultante de un `generate` **sin persistir** (fusiona request + horario existente).
- ✅ **Notificaciones por email** (#51 parcial): `INotificationService` (confirmación al crear cita, cancelación al pasar a Cancelled, recordatorio 24h via `AppointmentReminderService`). Reutiliza `IEmailSender`. **Push/FCM pendiente** (#51 sigue abierta).
- ✅ **Audit log** (#56): `IAuditLogger` registra login ok/fallido, cambios de password, alta de usuarios, cambios de horario/plantilla y cambios de estado de cita. `GET /api/admin/audit-logs` (Admin) con filtros.
- ✅ **Soft delete + audit fields** (#52, HECHO): `IAuditable` (CreatedAt/UpdatedAt/CreatedBy/UpdatedBy) + `ISoftDelete` (IsDeleted/DeletedAt) via clase base `AuditableEntity` (Domain/Common) en **Client, Employee, Appointment, Business, Service**. `AuditableSaveChangesInterceptor` rellena los audit fields y convierte los `Delete` en soft delete; global query filters `!IsDeleted`; `POST /api/{recurso}/{id}/restore` (Admin) por entidad. **Política: sin cascada, se conserva el historial.** Schedule/Holiday NO son soft-deletable (config técnica).
- ✅ **Zona horaria del negocio (BIZ-01, 3ª sesión):** las fechas de cita son hora de pared (`DateTime` sin Kind); `IClock.BusinessNow` (impl `BusinessClock`) da "ahora" en `Scheduling:TimeZone` (default `Europe/Madrid`), **independiente de la zona del servidor**. Lo usan el check de "pasado" del validador y la ventana del recordatorio. Modelo de **una sola zona** (multi-zona por negocio descartado salvo necesidad).
- ✅ **Anti doble-reserva (BIZ-02, 3ª sesión):** `IBookingConcurrencyGuard` serializa la sección crítica validar+insertar de `Create` y del reschedule de `Update` con `sp_getapplock` de SQL Server, keyed por empleado+día (en InMemory ejecuta directo). Cierra la carrera check-then-act que permitía superar `MaxConcurrentAppointments`.
- ✅ **Transiciones de estado de cita por rol (SVC-02, 3ª sesión):** un Client solo puede poner `Cancelled`; `Confirmed/Completed/NoShow` son del personal (Admin/Owner/Employee), si no → 403. Un cambio que NO toca el estado (notas, reprogramar) sigue permitido.
- ✅ **Disponibilidad sin huecos pasados (BIZ-06, 3ª sesión):** `AvailabilityService` omite los huecos cuyo inicio es anterior a `BusinessNow` (el mismo límite que el validador).
- ✅ **Endurecimientos de auth (3ª sesión):** refresh **reuse-detection** (re-usar un token rotado revoca toda la familia), rate-limit en `reset-password`/`confirm-email`, `logout` solo revoca el token del propio usuario, y con `RequireConfirmedEmail` ON el registro **no concede sesión** (no auto-login). `forgot-password` responde en **tiempo constante** (envío best-effort en su propio scope) → sin timing oracle.

### Robustez

- ✅ **146/146 tests verdes** (1 API.Tests placeholder + 97 unit + 48 integration).
- ✅ **`RepositoryBase<T>`** (Infrastructure/Repositories) centraliza el CRUD plano (GetById/Add/Update/Delete); los 8 repos de dominio lo heredan (#128).
- ✅ **Resource-based authorization** en handlers via `IResourceAuthorizationService` con 11 métodos `EnsureCan*Async`.
- ✅ **Códigos de error de dominio descriptivos** (#60): jerarquía `DomainException`/`NotFoundException` + concretas, mapeadas a `code` específico (ver `docs/error-codes.md`).
- ✅ **CancellationToken propagado de extremo a extremo** (#49): handlers → services → repos → EF.
- ✅ **Correlation ID** (#61): `X-Correlation-Id` en request/response + Serilog; es el `traceId` de los errores.
- ✅ **Health checks ricos** (#62): `/health`, `/health/ready` (SQL+Seq), `/health/live`, `/health-ui`.
- ✅ **Auto-migrate en Development** (`Database.MigrateAsync()` al arrancar) — fresh clone funciona con un solo `dotnet run`.
- ✅ **Fail-fast en producción** si faltan `Jwt:Key`, `Cors:AllowedOrigins` o `Email:Smtp:Host/From`.
- ✅ **UseForwardedHeaders** activo solo en producción (IP real tras proxy).
- ✅ **Concurrencia de reserva** serializada por empleado+día con `sp_getapplock` (3ª sesión, BIZ-02) — ver "Decisiones de diseño › Concurrencia de reserva".
- ✅ **Hora de pared coherente** vía `IClock`/`BusinessClock` (3ª sesión, BIZ-01) — ver "Decisiones de diseño › Zona horaria".
- ✅ **`/health` minimal fuera de Development** (3ª sesión, API-01): `/health` y `/health/ready` solo devuelven el estado global a anónimos en prod; el dashboard `/health-ui` queda solo en Development.

### Lo que NO está hecho todavía

- ❌ **Push notifications (FCM)** — #51 sigue abierta solo por esto: falta Firebase Admin SDK + persistir device tokens (necesita decidir cloud). El email ya está.
- ❌ Envío SMTP real verificado contra un relay (Mailtrap/SES). En Dev/Test el email se loguea; el envío real (`SmtpEmailSender`) está implementado pero sin probar contra un servidor.
- ❌ Tests unitarios de CRUD básico de los handlers de Business/Client/Employee/Service. (En la 3ª sesión SÍ se añadieron tests de `AppointmentService`, `AppointmentSchedulingValidator`, `AvailabilityService` y `BookingConcurrencyGuard`; el resto de CRUD sigue cubierto solo por integration/cross-tenant.)
- ❌ **#55** caching y **#58** global query filter — desaconsejadas sin métricas/incidente (siguen abiertas).
- ❌ Cloud secret manager (aparcado, sin cloud decidido).

## Estructura de carpetas

```
src/
├── MRC.Agendia.Api/
│   ├── Configuration/         ← Wiring (LoggingSetup, AuthenticationSetup, RateLimitingSetup, SwaggerSetup, CorsSetup, EmailSetup, HealthChecksSetup, PipelineExtensions) + ShortLivedTokenProvider (token reset 1h)
│   ├── Controllers/           ← Auth, Business, Client, Employee, Service, Appointment, Schedule, Holiday, Availability, AuditLog (api/admin/audit-logs)
│   ├── Middleware/            ← ExceptionHandlingMiddleware (excepciones→JSON) + CorrelationIdMiddleware (X-Correlation-Id)
│   ├── Services/              ← CurrentUserContext (UserId, IpAddress, roles), CurrentUserService
│   └── Program.cs             ← Orquesta ConfigureSerilog/AddInfrastructure/AddApplication/AddAppHealthChecks/AddIdentityAndJwt + AddEmailSender + auto-migrate (Dev) + seed + pipeline
├── MRC.Agendia.Application/
│   ├── Auth/                  ← IAuthService (login/refresh/logout/logoutAll/changePw/forgotPw/resetPw/confirmEmail/getCurrent) + IUserRegistrationService + Commands + DTOs + Validators
│   ├── Auditing/             ← IAuditLogger + AuditLogDto + Queries (GetAuditLogs) (#56)
│   ├── Authorization/         ← ICurrentUserContext (con IpAddress), IResourceAuthorizationService
│   ├── Availability/          ← Endpoint clave: huecos libres para reservar (omite huecos pasados, BIZ-06)
│   ├── Appointments/          ← IAppointmentSchedulingValidator (reglas de cita válida) + IBookingConcurrencyGuard (anti doble-reserva, 3ª sesión)
│   ├── Behaviors/             ← ValidationBehavior (MediatR pipeline)
│   ├── Business/Clients/Employees/Services/Schedules/Holidays/   ← CRUD CQRS por feature (Schedules incluye IScheduleGenerationService + preview)
│   ├── Common/                ← PagedResult<T> + Email/IEmailSender (abstracción de envío de correo) + IClock (hora de pared del negocio, 3ª sesión)
│   ├── Notifications/         ← INotificationService (emails de cita) (#51)
│   ├── Mappings/              ← Profiles de AutoMapper
│   └── DependencyInjection.cs ← AddApplication() — auto-discovery de MediatR/AutoMapper/FluentValidation + servicios
├── MRC.Agendia.Domain/
│   ├── Common/                ← IAuditable + ISoftDelete + AuditableEntity (clase base de las 5 entidades soft-deletable) (#52/#128)
│   ├── Constants/             ← Roles.cs + RolePolicies.cs + PaginationConstants.cs + AuditActions.cs
│   ├── Entities/              ← Business, Client, Employee (MaxConcurrentAppointments), Service, Appointment (con ReminderSentAt) — las 5 heredan AuditableEntity; ScheduleTemplate, ScheduleOverride, HolidayCalendar, AuditLog...
│   ├── Enums/                 ← AppointmentStatus (+ AppointmentStatusExtensions.OccupiesCapacity), ScheduleOverrideType, HolidayScope...
│   ├── Exceptions/            ← AuthenticationException (→401) + DomainException/NotFoundException + concretas (...NotFound, TemplatesOverlap, ScheduleOverrideConflict, AppointmentOutsideSchedule, AppointmentConflict, DuplicateEmail)
│   ├── Interfaces/            ← I*Repository (+ IAuditLogRepository)
│   └── Services/              ← IScheduleResolver (domain service: Resolve puro + ResolveRange por rango, en memoria)
├── MRC.Agendia.Infrastructure/
│   ├── Auditing/             ← AuditLogger (impl, best-effort) (#56)
│   ├── Authorization/         ← ResourceAuthorizationService (impl)
│   ├── Email/                 ← SmtpEmailSender (prod) + LoggingEmailSender (Dev/Test)
│   ├── Identity/              ← ApplicationUser, JwtTokenService, AuthService, UserRegistrationService, AuthEmailService, AuthResponseFactory, DbInitializer, RefreshTokenCleanupService
│   ├── Migrations/
│   ├── Notifications/         ← NotificationService + AppointmentReminderService (BackgroundService, recordatorio 24h) (#51)
│   ├── Persistence/           ← AuditableSaveChangesInterceptor (audit fields + Delete→soft delete) (#52) + BookingConcurrencyGuard (sp_getapplock, 3ª sesión)
│   ├── Repositories/          ← RepositoryBase<T> (CRUD plano compartido, #128) + repos EF (+ AuditLogRepository)
│   ├── Services/              ← ScheduleResolver (impl)
│   ├── Time/                  ← BusinessClock (impl de IClock; zona Scheduling:TimeZone, default Europe/Madrid) (3ª sesión)
│   ├── AgendiaDbContext.cs    ← IdentityDbContext + DbSets (incluye AuditLogs) + índice IX_Appointment_StartDate
│   └── DependencyInjection.cs ← AddInfrastructure(config) — DbContext + repos + auth + notifications + audit + hosted services
docs/
└── error-codes.md            ← Catálogo de códigos de error de la API (#60)
tests/
├── MRC.Agendia.API.Tests/        ← Placeholder UnitTest1.cs vacío. No tocar.
├── MRC.Agendia.Tests.Unit/       ← 97 tests con xUnit + NSubstitute + EF InMemory
│   ├── TestDoubles/              ← FakeCurrentUserContext
│   ├── Domain/                   ← AppointmentStatusExtensionsTests (OccupiesCapacity)
│   ├── Application/              ← Appointments/AppointmentServiceTests (SVC-01 + guard + SVC-02), Appointments/AppointmentSchedulingValidatorTests (past-check con IClock), Availability/AvailabilityServiceTests (BIZ-06) — 3ª sesión
│   └── Infrastructure/           ← Authorization/ResourceAuthorizationServiceTests, Services/ScheduleResolverTests, Notifications/NotificationServiceTests, Auditing/AuditLoggerTests, Persistence/AuditableSaveChangesInterceptorTests, Repositories/AppointmentRepositoryTests (query filters + solape de rango), Time/BusinessClockTests (3ª sesión)
└── MRC.Agendia.Tests.Integration/← 48 tests con xUnit + WebApplicationFactory<Program> + InMemory (+ Xunit.SkippableFact para el test LocalDB)
    ├── Infrastructure/           ← CustomWebApplicationFactory (con AuditableSaveChangesInterceptor) + FakeEmailSender (con WaitForAsync) + RequireConfirmedEmailWebApplicationFactory
    ├── Auth/                     ← AuthFlow (incl. reuse-detection + logout de token ajeno), OwnerRegistration, PasswordReset, EmailConfirmation, RequireConfirmedEmail (incl. registro sin sesión), LogoutAll, AuditLogEndpoint
    ├── Business/                 ← BusinessPublicEndpointTests
    ├── Common/                   ← CorrelationIdTests, ErrorCodesTests
    ├── SoftDelete/               ← SoftDeleteIntegrationTests (delete oculta, restore admin, audit fields, disponibilidad de negocio borrado)
    ├── Concurrency/              ← BookingConcurrencyGuardLocalDbTests ([SkippableFact]: sp_getapplock serializa; se omite si no hay LocalDB) — 3ª sesión
    ├── Employees/Services/Schedules/ ← Cross-tenant (incl. UpdateEmployee no mueve de tenant) + SchedulePreview
    └── Health/                   ← HealthCheckEndpointsTests
scripts/
└── create-github-issues.sh       ← Backlog bulk creation
```

## Flujo de una petición

```
Request
   ↓
[Pipeline en Api/Configuration/PipelineExtensions.cs]
   ↓
1. ForwardedHeaders     (solo si Environment != Development && != Testing)
2. CorrelationIdMiddleware (lee/genera X-Correlation-Id, lo fija como TraceIdentifier)
3. Swagger UI           (solo si Environment == Development)
4. HttpsRedirection     (skip en Testing)
5. CORS
6. RateLimiter          (skip en Testing)
7. ExceptionHandlingMiddleware
8. Authentication (JWT Bearer)
9. Authorization
10. MapControllers + MapHealthChecks (/health, /health/ready, /health/live; cuerpo detallado solo en Development, minimal fuera) + MapHealthChecksUI (/health-ui, solo en Development)
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
9. **DTOs de Update**: NO incluir `BusinessId` si el recurso pertenece a un Business. AutoMapper de `UpdateXDto → X` debe usar `.ForMember(BusinessId, opt => opt.Ignore())` para evitar cross-tenant takeover (ver #91/#92, y #125 para Employee — se olvidó y se parcheó). Para máxima seguridad: en `ScheduleService` se hace asignación campo a campo (sin AutoMapper) en Update. **OJO con FKs editables (Appointment):** `UpdateAppointment` SÍ puede cambiar `ClientId/EmployeeId/ServiceId`, así que el handler re-autoriza el destino con `EnsureCanCreateAppointmentAsync(dto.ClientId, dto.EmployeeId)` además de la cita existente (mass-assignment, #133). **Desde la 3ª sesión NINGÚN Update DTO lleva `BusinessId`** (se quitó de `UpdateServiceDto`, el último que lo tenía — SEC-04).
10. **Migraciones EF**: cada cambio de modelo va con su migración. Comprueba que `dotnet ef migrations add` no genera warnings inesperados. En Development las migraciones se aplican **automáticamente** al arrancar (issue #85, PR #86).
11. **Sin secretos en `appsettings.json`.** Usa `dotnet user-secrets` en dev y variables de entorno en producción. En Dev el connection string apunta a `(localdb)\MSSQLLocalDB` por defecto (#97).
12. **Merge a master (política 2026-05-23):** para **bugs y refactors** SÍ mergeo yo con `gh pr merge --admin` (ver "Política de trabajo y autonomía"). Para **features nuevas**, PR y espera revisión humana. NUNCA `--admin` en una feature nueva sin que el humano la apruebe.
13. **Routing — 3 patrones (NO se refactorizan, ver #102).** El proyecto convive con 3 patrones de ruta. Al crear un controller nuevo, elige según el tipo de recurso. La capitalización del archivo es la que sale en Swagger UI y en logs.

    | Patrón | Atributo | Ejemplos actuales | Cuándo usarlo |
    |---|---|---|---|
    | **Top-level singular** | `[Route("api/[controller]")]` | `/api/Business`, `/api/Client`, `/api/Service`, `/api/Employee`, `/api/Appointment`, `/api/Holiday` | Recurso de primer nivel. Heredado de `dotnet new webapi`: `[controller]` se expande al nombre del controller en PascalCase. Para un recurso nuevo (ej. `Invoice`, `Notification`) → `[Route("api/[controller]")]`. |
    | **Sub-recurso anidado plural** | `[Route("api/businesses/{businessId}/<recurso>")]` | `ScheduleController` (`/api/businesses/{id}/schedules`), `AvailabilityController` (`/api/businesses/{id}/availability`) | El recurso **pertenece a** un Business y no tiene sentido sin él. La URL refleja la jerarquía REST. Para un sub-recurso nuevo (ej. sucursales) → `[Route("api/businesses/{businessId}/branches")]`. |
    | **Operación / agrupador lowercase** | `[Route("api/<nombre>")]` literal en minúscula | `AuthController` (`/api/auth/login`, `/api/auth/register/owner`...) | No es un recurso CRUD sino un agrupador de operaciones. Para un grupo nuevo (ej. pagos) → `[Route("api/payment")]`. |

    - **NO pluralizar los top-level** (`/api/Clients`): el frontend móvil ya está acoplado a `/api/Client`, `/api/Service`, etc. Cambiarlo requiere versionado + deprecation. Lo mismo para kebab/snake_case.
    - ASP.NET routing es **case-insensitive**: `/api/client` y `/api/Client` resuelven al mismo endpoint. La convención del archivo solo afecta a lo que se muestra en Swagger/logs.

14. **Hora "ahora" en el flujo de citas → `IClock.BusinessNow`, NUNCA `DateTime.UtcNow`** (BIZ-01, 3ª sesión). Las fechas de cita son hora de pared; el check de "pasado", el recordatorio y la disponibilidad comparan contra `IClock.BusinessNow` (zona `Scheduling:TimeZone`). `UtcNow` se reserva para instantes reales (tokens, audit, `CreatedAt`).
15. **Crear/reprogramar citas pasa por `IBookingConcurrencyGuard`** (BIZ-02, 3ª sesión). Si añades un flujo que inserte o mueva una cita, envuelve validar+insertar con `ExecuteSerializedAsync(employeeId, date, ...)` para no reintroducir la carrera de doble-reserva.

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
- **Endurecimientos de auth (3ª sesión):**
  - **Refresh reuse-detection (SEC-01):** al re-presentar un refresh token ya rotado (`RevokedAt != null && ReplacedByToken != null`), `RefreshAsync` revoca TODA la familia de sesiones del usuario (replay de token robado). Un token revocado por logout (sin `ReplacedByToken`) cae en la rama normal de "revocado".
  - **`forgot-password` en tiempo constante (SEC-02):** el lookup+token+envío corre best-effort en su propio scope de DI (`Task.Run` + `IServiceScopeFactory`) y el endpoint devuelve de inmediato → la latencia no revela si el email existe, y siempre 204 (también evita el 500-solo-cuentas-existentes ante fallo SMTP).
  - **`logout` con pertenencia (SEC-05):** `LogoutCommand` lleva el `UserId` del caller (`User.GetUserId()`); `LogoutAsync` solo revoca si `stored.UserId == userId`.
  - **Sin sesión antes de confirmar (IDN-02):** con `Auth:RequireConfirmedEmail` ON, `RegisterClient/Owner` NO auto-loguean (devuelven la cuenta sin tokens vía `IAuthResponseFactory.CreateWithoutSessionAsync`). Con OFF (default) sin cambios.

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
- **`AvailabilityService`** calcula `Capacity` por slot = suma de `(MaxConcurrent - overlapping)` por empleado libre. El front pinta `slot.capacity` directamente. **Omite los huecos cuyo inicio es anterior a `IClock.BusinessNow`** (BIZ-06, 3ª sesión) — el mismo límite que rechaza el validador.
- **Marcar cita pasada (SVC-01, 3ª sesión):** `UpdateAsync` solo re-valida el scheduling cuando cambia un campo de reserva (StartDate/EndDate/Client/Employee/Service). Un cambio de solo estado/notas NO se re-valida, así que se puede marcar `Completed`/`NoShow`/`Cancelled` una cita pasada (y funciona aunque un participante se haya soft-deleteado después; el handler ya autoriza origen+destino, así que no se abre agujero).
- **Transiciones de estado por rol (SVC-02, 3ª sesión):** en `UpdateAsync`, si el caller NO es personal (Admin/BusinessOwner/Employee) y cambia `Status` a algo distinto de `Cancelled` → `UnauthorizedAccessException` (403). Comprobación **aditiva** a la autorización de pertenencia del handler.
- **Listado por rango = solape (BIZ-04, 3ª sesión):** `AppointmentRepository.GetByBusinessIdAndDateRangeAsync` usa solape (`Start<to && End>from`), no contención, para no perder citas que cruzan los bordes del rango. `GetAppointmentsByDateRangeQueryValidator` acota el rango a ≤366 días (sustituye al antiguo `ValidateRangeQuery`). Inocuo para `AvailabilityService` (pide el día completo y re-filtra por slot).
- **Reservas recurrentes (serie de citas, #174, feature):** `IRecurringAppointmentService` materializa **una `Appointment` por ocurrencia** (no una regla virtual) para reaprovechar `IAppointmentSchedulingValidator` + `IBookingConcurrencyGuard` (por empleado+día) tal cual el alta individual. Las fechas las genera `RecurrenceExpander` (puro, testeable): semanal multi-día, quincenal (`Interval`) y mensual por día del mes (los meses sin ese día se omiten y se informan). La ventana se acota a ≤366 días y se recorta a `IClock.BusinessNow` (no genera pasado). **"Saltar y avisar":** solo se capturan las excepciones **de fecha** (`AppointmentOutsideSchedule`/`AppointmentConflict`/`InvalidAppointmentTime`) como ocurrencia omitida; las de request (no encontrado, empleado inactivo, mismatch) abortan toda la petición. Las citas comparten `Guid? SeriesId` (migración `AddAppointmentSeriesId` + índice filtrado). Cancelar/mover solo tocan las **futuras y activas**; borrar es soft-delete de toda la serie. Endpoints Staff-only; autorización por serie vía `IResourceAuthorizationService.EnsureCanManageAppointmentSeriesAsync` (resuelve el negocio desde una cita de la serie). En creación masiva **NO** se envía email por cita (evita spam); el recordatorio de 24h sí sale por cada ocurrencia.

### Zona horaria (BIZ-01, 3ª sesión)

- Las fechas de cita son **hora de pared** (`DateTime` sin Kind, como llegan del JSON). Compararlas contra `DateTime.UtcNow` daba un desfase por el offset del negocio (en España se podían reservar citas hasta ~2h en el pasado; el recordatorio se desfasaba).
- **`IClock.BusinessNow`** (Application/Common; impl `BusinessClock` en Infrastructure/Time) convierte `UtcNow` a la zona configurada `Scheduling:TimeZone` (default `Europe/Madrid`), así "ahora" es **independiente de la zona del servidor**. Fail-fast si la zona no existe.
- Lo usan: el check de "pasado" del `AppointmentSchedulingValidator`, la ventana de `AppointmentReminderService` y el recorte de huecos pasados de `AvailabilityService`. `ReminderSentAt` sigue en UTC (solo es un flag de idempotencia).
- **NO se tocan** los usos legítimos de `UtcNow` (tokens JWT/refresh, audit `Timestamp`, `CreatedAt`).
- **Modelo de una sola zona** (Opción A). El multi-zona por negocio (`Business.TimeZoneId` + almacenar instantes UTC) se descartó por ahora: sería una feature con migración + cambio de contrato. Reabrir solo si hay negocios en zonas distintas.

### Concurrencia de reserva (BIZ-02, 3ª sesión)

- La reserva era check-then-act (validar solapes → insertar) sin atomicidad: dos peticiones concurrentes para el mismo empleado+franja podían superar `MaxConcurrentAppointments` (doble-reserva del último hueco).
- **`IBookingConcurrencyGuard`** (Application/Appointments; impl `BookingConcurrencyGuard` en Infrastructure/Persistence) envuelve la sección crítica validar+insertar en un **lock de aplicación de SQL Server** (`sp_getapplock`, `@LockOwner='Transaction'`) keyed por `booking:{employeeId}:{date}`, dentro de una transacción (auto-release en commit). Solo serializa el MISMO empleado+día (sin contención global).
- **Comprueba `IsSqlServer()` en runtime**: en SQL Server hace lock+transacción; en InMemory (tests) ejecuta la acción directa, así la suite no cambia. No hay retries de EF habilitados → la transacción manual es segura.
- Lo usan `AppointmentService.CreateAsync` y el reschedule de `UpdateAsync`. Notificaciones y audit-log quedan **fuera** del lock.
- Verificado con un test LocalDB (`[SkippableFact]`): dos llamadas concurrentes con la misma clave **se serializan** (se omite si no hay LocalDB).

### Horarios

- **One Business → many ScheduleTemplates** que no se solapan en fechas. La elección del horario efectivo es por fecha (no por flag `IsDefault` — existe pero no afecta resolución).
- **ScheduleOverride** prevalece sobre la plantilla para un día concreto.
- **`IScheduleResolver`** es la fuente de verdad para "qué horario aplica el día X" — úsalo siempre, no reimplementes. Vive en `Domain/Services/`, impl en `Infrastructure/Services/`.
- **`ScheduleService.UpdateTemplateAsync` / `UpdateOverrideAsync` usan asignación campo a campo, NO AutoMapper.** Es defensa en profundidad — los DTOs no tienen BusinessId y el servicio no lo tocaría aunque viniera. Patrón que el resto de services podrían adoptar (issue futuro si se decide).
- **SRP de Schedule (#100):** la generación masiva (el "wizard" de configuración inicial) se extrajo a **`IScheduleGenerationService`** (`GenerateScheduleAsync` + `ValidateTemplatesDoNotOverlap`). `ScheduleService` se queda con CRUD de templates/overrides + `GetEffectiveSchedule` + `GetCalendar`. El `GenerateScheduleCommandHandler` inyecta el servicio nuevo.
- **Preview de schedule (#54):** `IScheduleGenerationService.PreviewScheduleAsync` comparte `BuildAsync` con `GenerateScheduleAsync` (construye templates+overrides en memoria, sin persistir), **fusiona con el horario existente** del negocio y resuelve el calendario del **año completo**. Usa el método **puro `IScheduleResolver.Resolve(templates, overrides, date)`** (en memoria, sin BD) — `ScheduleResolver` comparte el mapeo `BuildEffectiveSchedule` entre los métodos async (BD) y el puro. Endpoint Staff-only `POST /api/businesses/{id}/schedules/preview`.

### Errors (#60 — códigos de dominio descriptivos)

- **Jerarquía de excepciones de dominio** en `Domain.Exceptions`: `DomainException` (abstracta, con `Code`) y `NotFoundException` (abstracta). Las concretas llevan su `Code` (ej. `BusinessNotFoundException` → `BUSINESS_NOT_FOUND`, `DuplicateEmailException` → `DUPLICATE_EMAIL`, `TemplatesOverlapException`, `ScheduleOverrideConflictException` → `SCHEDULE_OVERRIDE_CONFLICT` (override duplicado por fecha, BIZ-05/3ª sesión), `AppointmentOutsideScheduleException`, `AppointmentConflictException`). **NO heredan de las builtin** (a propósito).
- **`ExceptionHandlingMiddleware`** mapea (en este orden):
  - `AuthenticationException` → **401** `UNAUTHENTICATED`
  - `NotFoundException` → **404** con su `Code`
  - `DomainException` → **400** con su `Code`
  - Legacy fallback: `KeyNotFoundException`→404 `NOT_FOUND`, `UnauthorizedAccessException`→403 `FORBIDDEN`, `InvalidOperationException`/`ArgumentException`→400 `BAD_REQUEST`
  - `FluentValidation.ValidationException` → 400 `VALIDATION_ERROR` con `errors: { Field: ["msg"] }`
  - Otros → 500 `INTERNAL_ERROR`
- Respuesta JSON: `{ code, message, traceId, [errors] }`. El `traceId` == correlation ID (#61).
- **Catálogo completo en `docs/error-codes.md`.** Para una excepción nueva: heredar de `NotFoundException`(404) o `DomainException`(400) con su `Code`, lanzarla desde el service/validator, y añadir la fila al catálogo.
- **Mensajes en español** (los ve el dev/front); solo el `code` es estable para ramificar en el front.

### Rate limiting (en `/api/auth/*`)

- `auth-login`: 5 / IP / minuto
- `auth-refresh`: 10 / IP / minuto
- `auth-register`: 3 / IP / hora
- **`reset-password` y `confirm-email`** usan `auth-login` (5/min) desde la 3ª sesión (antes estaban sin límite, SEC-03).
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

### Notificaciones de cita por email (#51, parcial)

- **`INotificationService`** (Application) + **`NotificationService`** (Infrastructure): carga la cita con detalles y envía email **al cliente** vía `IEmailSender`. Plantillas HTML en español. **Best-effort**: cualquier fallo se loguea y NUNCA rompe el flujo de reserva.
- **Triggers** (en `AppointmentService`): confirmación en `CreateAsync`; cancelación al pasar el estado a `Cancelled` en `UpdateAsync`.
- **Recordatorio 24h**: `AppointmentReminderService` (BackgroundService) escanea citas que empiezan en las próximas `Notifications:ReminderWindowHours` (24) cada `Notifications:ReminderIntervalMinutes` (60). **Idempotente** vía `Appointment.ReminderSentAt`. La ventana usa `IClock.BusinessNow` (BIZ-01) y **excluye** citas con cliente/empleado/negocio soft-deleted o empleado inactivo (BIZ-03, 3ª sesión).
- **Push (FCM) NO está hecho** — #51 sigue abierta solo por eso (necesita Firebase + device tokens).

### Audit log (#56)

- **`IAuditLogger`** (Application) + **`AuditLogger`** (Infrastructure, best-effort): escribe `AuditLog` rellenando `UserId` e `IpAddress` desde `ICurrentUserContext`. **Llamar SIEMPRE después de persistir** la operación (hace `SaveChanges` sobre el contexto compartido).
- **Eventos** (hooks en services): `LOGIN_SUCCESS`/`LOGIN_FAILED`, `PASSWORD_CHANGED`/`PASSWORD_RESET` (AuthService), `USER_CREATED` (UserRegistrationService), `SCHEDULE_TEMPLATE_*`/`SCHEDULE_OVERRIDE_*` (ScheduleService), `APPOINTMENT_STATUS_CHANGED` (AppointmentService). Códigos en `AuditActions`.
- **`GET /api/admin/audit-logs`** (solo Admin) con filtros (userId, action, entityType, from, to) + paginación.

### Soft delete + audit fields (#52, HECHO)

- **Alcance: 5 entidades de negocio** — Client, Employee, Appointment, Business, Service. Heredan `AuditableEntity` (Domain/Common) que implementa `IAuditable` (CreatedAt/UpdatedAt/CreatedBy/UpdatedBy) + `ISoftDelete` (IsDeleted/DeletedAt). Schedule/Holiday NO (config técnica/global).
- **`AuditableSaveChangesInterceptor`** (Infrastructure/Persistence): rellena audit fields desde `ICurrentUserContext` (Added→Created, Modified→Updated) y **convierte `Delete` en soft delete** (IsDeleted=true). En jobs/seed `CreatedBy` queda null (no hay usuario).
- **Global query filters** `!IsDeleted` en las 5 entidades (`AgendiaDbContext.OnModelCreating`) + índice en `IsDeleted`. `CreatedAt` con `HasDefaultValueSql("GETUTCDATE()")` para backfill.
- **Política: SIN cascada, se conserva el historial.** Borrar un padre NO borra hijos. Por eso las **lecturas de cita que cargan padres usan `IgnoreQueryFilters()` + `Where(!a.IsDeleted)`** (`AppointmentRepository.GetByIdWithDetailsAsync/GetPagedAsync/GetPagedByClientIdAsync/GetByBusinessIdAndDateRangeAsync`): si no, el `Include` de una navegación requerida soft-deleted hace INNER JOIN y **descarta la cita** (rompía notificaciones y el conteo de capacidad → doble-reserva; arreglado en #127/#133).
- **Restore**: `GetByIdIncludingDeletedAsync` (con `IgnoreQueryFilters`) en los 5 repos + `RestoreAsync` en los 5 services (idempotente) + `POST /api/{recurso}/{id}/restore` `[Authorize(Roles=Admin)]`.
- **OJO `FindAsync`**: en EF Core 8 **SÍ aplica los query filters** (verificado). `GetByIdAsync` (que usa FindAsync) devuelve null para entidades soft-deleted — correcto.

### Reuso / DRY (#128) y código muerto (#135)

- **`RepositoryBase<T>`** (Infrastructure/Repositories): CRUD plano compartido (`GetByIdAsync` via FindAsync, `AddAsync`, `Update`, `Delete`). Los 8 repos de dominio lo heredan; cada uno conserva sus queries específicas. `AuditLogRepository` queda aparte (solo Add + query). **Preservar la semántica exacta** (FindAsync, AsNoTracking, IgnoreQueryFilters) al tocarlo.
- **`AuditableEntity`** clase base: evita repetir los 6 campos audit/soft-delete x5 entidades. No cambia el esquema EF (no hay DbSet de la base).
- Eliminado código muerto: `GetAllAsync` no paginado (solo lo usaba Holiday) y `ScheduleOverrideRepository.DeleteRange` (#135).

### Integridad de citas (#133)

- **`AppointmentStatus.OccupiesCapacity()`** (Domain/Enums): predicado compartido `Pending|Confirmed` que usan **AvailabilityService Y AppointmentSchedulingValidator** para contar ocupación. Antes divergían (el validador incluía `Completed`).
- **`AppointmentSchedulingValidator`** valida también que el **negocio no esté soft-deleted** (carga `IBusinessRepository.GetByIdAsync`) — coherente con AvailabilityService.
- **`UpdateAppointment`**: el handler re-autoriza el destino (`EnsureCanCreateAppointmentAsync`) + `ReminderSentAt` se resetea al reprogramar (StartDate cambia) para reenviar el recordatorio.

### Correlation ID (#61)

- `CorrelationIdMiddleware` (al inicio del pipeline) lee/genera `X-Correlation-Id`, lo fija como `HttpContext.TraceIdentifier` (→ el `traceId` de los errores coincide), lo empuja a Serilog (`LogContext.PushProperty`) y lo devuelve en la response.

### Health checks (#62)

- `HealthChecksSetup`: SQL Server (crítico, tag `ready`) + Seq (`Degraded`, solo avisa). Endpoints `/health` (todo), `/health/ready` (deps), `/health/live` (proceso), `/health-ui` (dashboard). Seq URL en `HealthChecks:SeqUrl`. Paquetes: `AspNetCore.HealthChecks.SqlServer/.Uris/.UI.InMemory.Storage` (9.0.0).
- **Endurecido (API-01, 3ª sesión):** el informe detallado (`WriteHealthCheckUIResponse`) y el dashboard `/health-ui` solo se exponen en **Development**. Fuera de Development, `/health` y `/health/ready` devuelven un cuerpo **minimal** (solo `{"status":"..."}`), sin nombres/duraciones de dependencias; los probes siguen usando el status code (200/503). `/health/live` no cambia. (El dashboard polla `/health/ready`, que necesita el JSON detallado → por eso ambos van solo en Development.)

### CancellationToken (#49)

- Propagado handlers → services → repos → EF en **todas** las firmas async (`CancellationToken cancellationToken = default`). Métodos `void` (Update/Delete) y el `Resolve` puro NO lo llevan. `UserManager` de Identity no expone overloads con ct: ahí solo se propaga a repos/stores.

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

# Tests (146/146 verdes a fecha de hoy)
dotnet test

# Migración nueva. OJO: 'dotnet ef' NO está en el PATH del bash de Claude
# (usar "C:/Users/marco/.dotnet/tools/dotnet-ef.exe") y hay 2 DbContext
# (AgendiaDbContext + el de HealthChecks UI) -> SIEMPRE pasar --context AgendiaDbContext.
dotnet ef migrations add NombreMigracion --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api --output-dir Migrations --context AgendiaDbContext

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
5. **Tests:** `dotnet test` SIN regresiones (146/146 actual). Tras crear ficheros nuevos, `dotnet format` (el `.editorconfig` exige CRLF; los ficheros nuevos suelen salir con LF).
6. **Commit** en español, sin tildes (SIN trailer de atribución a Claude):
   ```
   <tipo>: <descripción corta>

   Closes #<num>

   <detalle>
   ```
   Tipos: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`.
7. **Push** + abrir PR con `gh pr create --base master` (sin footer "Generated with Claude Code").
8. **Merge según el tipo (política 2026-05-23):**
   - **Bug/refactor/chore/docs:** revisar con agentes (varias veces) → si verde y verificado, `gh pr merge <num> --admin --merge --delete-branch` y `git checkout master && git pull`.
   - **Feature nueva:** NO mergear; esperar revisión humana.
9. Tras mergear un bug/refactor, seguir con lo siguiente. Para features, **una PR cada vez** (esperar al humano).

## Cosas que NO debes hacer sin permiso explícito

- ❌ **Mergear a `master` una FEATURE nueva** sin revisión humana. (Bugs/refactors SÍ se auto-mergean con `--admin`, ver "Política de trabajo y autonomía".)
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

## Estado del backlog (2026-05-29, 5ª sesión)

> **5ª sesión:** #167, #171 y #170 MERGEADAS (PRs #179/#181/#182); **#51 push núcleo en PR #183** (sin proveedor real). #172/#173 a futuro (no construir). Decisión de producto registrada: **#170 se implementó ADITIVO** (ServiceId principal + colección de extras) para **no romper el contrato del front**. Patrón confirmado: feature nueva → PR → el **humano mergea**.
>
> **4ª sesión:** se abrió un backlog de **features nuevas** (#167–#173; #174 se creó nueva en esa sesión). Patrón: feature nueva → issue → implementar → PR → el **humano mergea** (no auto-merge de features).
>
> _3ª sesión:_ re-auditoría completa, **TODO el punch list cerrado** (#139…#165); decisiones aceptadas en `project_accepted_audit_decisions.md`. _2ª sesión:_ **#52** + auditoría inicial (#125/#127/#128/#129/#133/#135).

### Features nuevas (backlog de producto)

| # | Título | Estado |
|---|---|---|
| [#174](https://github.com/MarcosRabadan/Agendia/issues/174) | Reservas recurrentes (serie en masa) | ✅ **MERGEADA** (PR #175) |
| [#168](https://github.com/MarcosRabadan/Agendia/issues/168) | Alerta de retraso en tiempo real | ✅ **MERGEADA** (PR #176) |
| [#169](https://github.com/MarcosRabadan/Agendia/issues/169) | Panel de estadísticas del negocio | ✅ **MERGEADA** (PR #177) |
| [#167](https://github.com/MarcosRabadan/Agendia/issues/167) | Lista de espera para huecos completos | ✅ **MERGEADA** (PR #179) |
| [#171](https://github.com/MarcosRabadan/Agendia/issues/171) | Cancelación/reprogramación self-service (política de antelación) | ✅ **MERGEADA** (PR #181) |
| [#170](https://github.com/MarcosRabadan/Agendia/issues/170) | Reserva multiservicio (varios servicios en una cita) | ✅ **MERGEADA** (PR #182, enfoque **aditivo**: ServiceId principal + colección de extras; no rompe el front). |
| [#172](https://github.com/MarcosRabadan/Agendia/issues/172) | Pagos online y depósito anti-no-show | ⏸️ **DISCOVERY** (la propia issue dice "no lista para implementar": pasarela, política anti-no-show, asesoría legal/fiscal). |
| [#173](https://github.com/MarcosRabadan/Agendia/issues/173) | Facturación electrónica / Verifactu | ⏸️ **FUTURO, no construir** (integrar proveedor). |

### Otras issues abiertas (anteriores)

| # | Título | Estado / Notas |
|---|---|---|
| [#51](https://github.com/MarcosRabadan/Agendia/issues/51) | Notificaciones (email + push) | ✅ **CERRADA** (6ª sesión). Núcleo email+push mergeado (PR #183). Envío **FCM real** → issue de seguimiento **#185** (abierta, no verificable sin Firebase). |
| [#55](https://github.com/MarcosRabadan/Agendia/issues/55) | Caching de festivos y plantillas | ✅ **MERGEADA** (PR #186). Decoradores `IMemoryCache` (festivos/año + plantillas/negocio). |
| [#58](https://github.com/MarcosRabadan/Agendia/issues/58) | Global query filter por BusinessId | ✅ **MERGEADA** (PR #186). `ICurrentBusinessScope` + filtro global en Business/Employee/Service (Owner/Employee ven solo lo suyo; Admin/anónimo/Client sin restricción; lecturas públicas con `IgnoreQueryFilters`). |

### Auditoría 6ª sesión — issues abiertas (#185, #187–#198)

| # | Título | Estado |
|---|---|---|
| [#185](https://github.com/MarcosRabadan/Agendia/issues/185) | Cablear proveedor real de push (FCM) en `PushSetup` | Abierta (seguimiento de #51; no verificable sin Firebase). |
| [#193](https://github.com/MarcosRabadan/Agendia/issues/193) | Waitlist: re-chequeo de capacidad **(hecho)** + serializar trigger + índice único de join | 🟡 Parcial: capacidad mergeada (PR #206); concurrencia abierta (baja-prob single-instance). |
| [#194](https://github.com/MarcosRabadan/Agendia/issues/194) | Series: audit-log en skip-only + colisión `MoveSeries` | Abierta (MEDIUM). |
| [#195](https://github.com/MarcosRabadan/Agendia/issues/195) | Endurecimiento auth/crypto (bundle LOW) | Abierta. Incluye **hashear refresh tokens** (migración). |
| [#196](https://github.com/MarcosRabadan/Agendia/issues/196) | Fiabilidad notificaciones/SMTP (bundle LOW) | Abierta. Pequeño y limpio. |
| [#197](https://github.com/MarcosRabadan/Agendia/issues/197) | Pulido API/persistencia (bundle LOW) | Abierta. Incluye **FK WaitlistEntry** (migración). |
| [#198](https://github.com/MarcosRabadan/Agendia/issues/198) | Fidelidad de tests: integración contra SQL Server real | Abierta (META). Por qué la suite verde no cazó el 500 de #188. |

### Pendientes "fuera de scope" (no en el backlog)

- **Cloud secret manager** (Azure Key Vault / AWS Secrets Manager / GCP). Aparcado hasta decidir cloud. Memoria persistida en `project_prod_secrets.md`.
- **Refactor `Resource` para salas/equipos** (generalizar Employee → Resource abstracto). Marcado "solo si surge necesidad". Hoy Employee+capacity cubre los casos reales.

## Cómo retomar el desarrollo (recomendación honesta)

El punch list de la auditoría está **cerrado** (incluidos los 2 riesgos reales: zona horaria y concurrencia). Lo natural ahora son features o pasos de pre-producción, no más arreglos. Por orden:

1. **Revisar/mergear #51 (PR #183)** si está OK — es el **núcleo** de push (device tokens + `IPushSender` + integración best-effort en notificaciones), **sin** proveedor real. Para **completar #51**: cablear FCM (u otro) en `PushSetup` con credenciales por entorno + fail-fast en prod (como `EmailSetup`); el envío real no es verificable sin un proyecto Firebase (igual que el SMTP).
2. **Antes de producción real**: verificar el **envío SMTP real** (`SmtpEmailSender`) contra un relay (Mailtrap/SES). La lógica está hecha y testeada con un fake; el envío real no se ha probado contra un servidor.
3. **Seguimientos opcionales de #170 (multiservicio):** sumar los servicios extra al **ingreso del panel de stats (#169)** y exponer `TotalPrice`/duración total en el read DTO (v1 cuenta solo el servicio principal). Series (#174) y lista de espera (#167) siguen single-service por diseño.
4. **Re-auditar periódicamente** con agentes (reviewers por área + verificadores). Antes de reportar, revisar `project_accepted_audit_decisions.md` para no re-levantar lo ya aceptado. **Aislar los agentes revisores (worktree) o darles git solo-lectura** — en la 5ª sesión un revisor con git tocó el árbol compartido.
5. **NO atacar #55 (caching) ni #58 (global query filter)** sin métricas / sin incidente.
6. Deuda conocida sin issue: **tests unitarios de los CRUD handlers** de Business/Client/Employee/Service.

## Si te quedas sin contexto / dudas

- **Sistema de horarios complejo:** lee `src/MRC.Agendia.Infrastructure/Services/ScheduleResolver.cs`.
- **Auth resource-based:** lee `src/MRC.Agendia.Infrastructure/Authorization/ResourceAuthorizationService.cs`.
- **Validación de citas:** lee `src/MRC.Agendia.Application/Appointments/AppointmentSchedulingValidator.cs`.
- **Algoritmo de disponibilidad:** lee `src/MRC.Agendia.Application/Availability/AvailabilityService.cs`.
- **Cómo se mapean excepciones:** `src/MRC.Agendia.Api/Middleware/ExceptionHandlingMiddleware.cs`.
- **Soft delete + audit:** `src/MRC.Agendia.Infrastructure/Persistence/AuditableSaveChangesInterceptor.cs` + los query filters/índices en `AgendiaDbContext.OnModelCreating` + `Domain/Common/AuditableEntity.cs`.
- **Reuso de repos:** `src/MRC.Agendia.Infrastructure/Repositories/RepositoryBase.cs`.
- **Cómo se construye un test de integration:** mira `tests/MRC.Agendia.Tests.Integration/Infrastructure/CustomWebApplicationFactory.cs` + cualquier `*IntegrationTests.cs`. Hay helpers para registrar Owner y obtener su `BusinessDto`.
- Si una decisión de diseño no es obvia, **pregúntale al usuario antes de codificar**.

## Cosas pendientes / out of scope conocidas

- Refactor "Resource" para soportar salas/equipos abstractos (vs. el actual `Employee` con `MaxConcurrentAppointments`). Hoy `Employee + capacity` cubre los casos reales planteados.
- Cloud secret manager para producción. Aparcado por decisión de cloud.
- Tests unitarios de los CRUD handlers de Business/Client/Employee/Service. (Appointment, disponibilidad, validador de citas y guard de concurrencia ya tienen tests desde la 3ª sesión; el resto sigue cubierto vía integration.) Deuda conocida, sin issue.
- Verificación del envío SMTP real contra un relay (la lógica de #57 está hecha y testeada con un fake; el `SmtpEmailSender` no se ha probado contra un servidor real).
