# Auditoría integral de MRC.Agendia — prompts listos para usar

> **Plan para ejecutar con el plan Max.** Objetivo: barrer el código de arriba a abajo
> buscando bugs, inconsistencias y mejoras, **proponer tests** y luego arreglar sin romper
> nada. Motor recomendado: **Opus 5** a `effort: xhigh` para los revisores, `high` para los
> verificadores. No lanzar los 8 slices a la vez; ir por bloques (1-4, luego 5-8),
> consolidando entre medias.

## Cómo funciona (dos fases)

- **Fase 1 — Análisis (SOLO LECTURA).** Los agentes buscan y **proponen** los tests, pero
  **no tocan código**. Nada se modifica hasta que tú elijas. Aísla los agentes (worktree o
  git solo-lectura) para que no pierdan cambios sin commitear.
- **Fase 2 — Implementación (después, uno a uno).** Por cada arreglo que elijas, un agente
  lo implementa **+ escribe los tests propuestos** y pasa la verja de verificación antes de
  abrir PR. Una tarea a la vez; el usuario revisa y mergea.

Estructura por slice: **1 revisor** (busca) → **1 verificador** (confirma cada hallazgo
contra el código, descarta falsos positivos). Consolido yo los hallazgos verificados en una
tarea/issues para que elijas qué arreglar.

---

## PREÁMBULO COMPARTIDO (antepón esto a CADA brief de revisor y verificador)

```
Eres un revisor de código senior de C#/.NET auditando MRC.Agendia, un microservicio de
RESERVAS PURAS (agenda) en .NET 9, Clean Architecture + DDD, CQRS con MediatR, EF Core 9 +
Npgsql + PostgreSQL, FluentValidation, y notificaciones por eventos de integración (outbox
transaccional). La identidad la posee Harmony (Agendia solo valida sus JWT); el perfil del
negocio y el precio del servicio los posee el servicio de gestion. Agendia solo guarda la
DURACION del servicio y el estado de agenda.

REGLAS DE ESTA AUDITORIA:
- SOLO ANALISIS. No modifiques ni escribas codigo. No abras PRs. No ejecutes comandos que
  cambien estado. Trabajas en una copia aislada de solo lectura.
- Reporta TODO lo que encuentres, incluido lo dudoso y lo de baja severidad. NO filtres por
  importancia: un paso posterior de verificacion lo hara. Tu objetivo aqui es COBERTURA.
- Por cada hallazgo da: (a) file:line, (b) categoria (correctness / concurrency / security /
  consistency / efficiency / test-coverage / design), (c) un ESCENARIO DE FALLO CONCRETO
  (inputs/estado -> resultado erroneo o excepcion), (d) confianza (alta/media/baja),
  (e) severidad (alta/media/baja), (f) TESTS PROPUESTOS que lo cubririan (describe
  arrange/act/assert; di si es unit con xUnit+NSubstitute+EF InMemory o integracion con
  Testcontainers-Postgres para constraints/concurrencia).
- No propongas abstracciones prematuras: 3 lineas repetidas no justifican una abstraccion.
- Mensajes de runtime (logs, excepciones, validacion) van en INGLES; marca cualquiera en
  espanol como inconsistencia.

INVARIANTES QUE DEBEN CUMPLIRSE (una violacion es un hallazgo):
1. Hora "ahora" en el flujo de citas -> IClock.BusinessNow, NUNCA DateTime.UtcNow. Las fechas
   de cita son hora de pared (zona Scheduling:TimeZone). UtcNow se reserva para instantes
   reales: audit, CreatedAt, OccurredOnUtc.
2. Crear/reprogramar cita pasa SIEMPRE por IBookingConcurrencyGuard (envuelve validar+insertar
   en pg_advisory_xact_lock por empleado+dia).
3. Ningun Update*Dto de recurso scoped incluye BusinessId, OwnerUserId ni UserId (vector
   cross-tenant/takeover). Excepcion: UpdateAppointment SI cambia ClientUserId/EmployeeId/
   ServiceId, y por eso el handler debe RE-AUTORIZAR el destino.
4. Autorizacion en los handlers via IResourceAuthorizationService.EnsureCan*Async ANTES de
   delegar; no en los controllers (salvo listados Admin-only).
5. async/await en todo acceso a BD; nunca .Result ni .Wait().
6. Query filters globales !IsDeleted; las lecturas que cargan padres usan IgnoreQueryFilters()
   + Where(!IsDeleted) para no descartar el hijo por un padre soft-deleted.
7. Fechas de pared (Appointment.StartDate/EndDate y similares) en columnas
   "timestamp without time zone"; timestamptz solo para instantes reales.
8. PK/FK son Guid UUIDv7 client-side (UuidV7ValueGenerator en el Add). AuditLog.Id es long.
9. CancellationToken propagado handlers -> services -> repos -> EF.
10. Combos de roles via RolePolicies (AdminOrOwner, Staff, AdminOrSelfClient), no strings.
11. AppointmentStatus.OccupiesCapacity() = Pending|Confirmed (para el calculo de capacidad).
12. Cap de fechas Domain/Constants/SchedulingLimits (2000..2100) en validadores de rango
    para evitar overflow de DateOnly.AddDays (debe dar 400, no 500).

BUGS YA CAZADOS (verifica que NO reaparecen y que no tienen PARIENTES en tu slice):
- #273: escribir DateTime con Kind=Unspecified en timestamptz PETA en Npgsql -> columnas de
  pared deben ser "timestamp without time zone".
- #275: carrera del Join en waitlist -> PostgresException 23505 sobre
  IX_WaitlistEntry_UniqueWaiting traducida a DuplicateWaitlistEntryException en UnitOfWork.Save.
- Doble reserva -> serializada por IBookingConcurrencyGuard.
- Indice unico filtrado de waitlist con NULLS NOT DISTINCT (Postgres trata NULL != NULL).

SALIDA: una lista de hallazgos ordenada por (severidad desc, confianza desc). Si el slice
esta limpio, dilo explicitamente. No inventes hallazgos para rellenar.
```

---

## FASE 1 — BRIEFS DE REVISOR (uno por slice)

> Antepón el PREÁMBULO. Cada brief indica ficheros y qué sondear específicamente.

### Slice 1 — Citas

```
SLICE: Citas (creacion, actualizacion, series, delay, estados).
FICHEROS NUCLEO:
- src/MRC.Agendia.Application/Appointments/AppointmentService.cs
- src/MRC.Agendia.Application/Appointments/RecurringAppointmentService.cs
- src/MRC.Agendia.Application/Appointments/AppointmentSchedulingValidator.cs
- src/MRC.Agendia.Application/Appointments/AppointmentDelayService.cs
- src/MRC.Agendia.Infrastructure/Persistence/BookingConcurrencyGuard.cs
- Domain/Entities/Appointment.cs (+ ExtraServices), Enums de estado, y sus handlers CQRS.
SONDEA ESPECIALMENTE:
- Toda alta y reprogramacion pasa por IBookingConcurrencyGuard (invariante 2). Busca rutas
  que validen+inserten fuera del guard.
- Uso de BusinessNow vs UtcNow en calculos de solape, ventana de cancelacion y delay (inv. 1).
- UpdateAppointment re-autoriza el destino cuando cambia ClientUserId/EmployeeId/ServiceId
  (inv. 3). Comprueba que valida "mismo negocio" y duracion == service.DurationMinutes.
- Transiciones de estado: un Client solo puede pasar a Cancelled; cambiar estado de una cita
  terminal (Completed/NoShow/Cancelled) debe dar 400 INVALID_APPOINTMENT_STATUS_TRANSITION.
- Series: materializa una Appointment por ocurrencia reusando validador+guard; "saltar y
  avisar" en choques; comparten SeriesId; emite eventos por ocurrencia. Busca fugas: una
  ocurrencia que rompe la transaccion de las demas, o eventos duplicados/faltantes.
- Capacidad: OccupiesCapacity() = Pending|Confirmed; capacidad no excedida vs
  Employee.MaxConcurrentAppointments. Negocio soft-deleted no debe permitir alta.
- Multiservicio: duracion total = principal + ExtraServices; stats cuenta solo el principal.
```

### Slice 2 — Horarios

```
SLICE: Horarios (resolucion por fecha, generacion anual, overrides, plantillas, cache).
FICHEROS NUCLEO:
- src/MRC.Agendia.Infrastructure/Services/ScheduleResolver.cs
- Application/Schedules/ScheduleGenerationService.cs y su ScheduleService (update campo a campo)
- Domain/Constants/SchedulingLimits.cs
- Decoradores de cache (IMemoryCache) de festivos/anio y plantillas/negocio.
- Domain/Entities/ScheduleTemplate.cs, ScheduleOverride.cs, HolidayCalendar.cs.
SONDEA ESPECIALMENTE:
- IScheduleResolver es la fuente de verdad de "que horario aplica el dia X" (Resolve puro +
  ResolveRange). Busca reimplementaciones divergentes en otros servicios.
- Tie-break de plantilla: IsDefault gana en resolver, repo Y decorador de cache. Verifica que
  los tres coinciden (una divergencia = bug sutil de horario).
- ScheduleOverride prevalece sobre plantilla; unico por fecha. La generacion deduplica overlaps
  (vacaciones que solapan, cierres repetidos) con un HashSet para no violar el indice unico
  IX_ScheduleOverride_BusinessId_Date (seria 500). Busca rutas de generacion sin dedupe.
- ScheduleService update: NUNCA debe mapear BusinessId (inv. 3); va campo a campo. Verifica
  que AutoMapper no reintroduce BusinessId por convencion.
- Cap de fechas SchedulingLimits en validadores de rango (inv. 12): DateOnly.AddDays fuera de
  2000..2100 debe dar 400, no overflow/500.
- Consistencia de cache: invalidacion tras generar/modificar; entradas obsoletas ("ghost days").
```

### Slice 3 — Disponibilidad + Waitlist

```
SLICE: Disponibilidad y lista de espera.
FICHEROS NUCLEO:
- src/MRC.Agendia.Application/Availability/AvailabilityService.cs
- src/MRC.Agendia.Application/Waitlist/WaitlistService.cs (+ handlers)
- Domain/Entities/WaitlistEntry.cs, el indice unico IX_WaitlistEntry_UniqueWaiting
  (NULLS NOT DISTINCT) en AgendiaDbContext.
- Domain/Exceptions/DuplicateWaitlistEntryException.cs, WaitlistEntryNotFoundException.cs
SONDEA ESPECIALMENTE:
- AvailabilityService calcula capacidad por slot y OMITE huecos pasados (< BusinessNow) (inv. 1).
  Busca comparaciones con UtcNow o huecos pasados que se cuelan.
- WaitlistEntry es Entity (NO AuditableEntity): su CreatedAt manual es OBLIGATORIO. Verifica
  que se asigna siempre.
- Indice unico con NULLS NOT DISTINCT: apuntarse a "cualquier empleado" (EmployeeId null) debe
  deduplicar. Busca rutas que asuman NULL != NULL.
- Carrera del Join (#275): 23505 sobre IX_WaitlistEntry_UniqueWaiting -> se traduce a
  DuplicateWaitlistEntryException en UnitOfWork.Save. Verifica que la traduccion cubre el
  constraint name exacto y que no hay otras rutas de insercion sin cubrir.
- Al liberarse un hueco: aviso FIFO (evento WaitlistSlotAvailable) tras RE-CHEQUEAR capacidad,
  serializado por el guard. Busca avisos sin re-chequeo o fuera del guard.
```

### Slice 4 — Mensajería / eventos

```
SLICE: Outbox transaccional, eventos de dominio en el agregado, dispatcher, reminder.
FICHEROS NUCLEO:
- src/MRC.Agendia.Infrastructure/Messaging/ (OutboxMessage, OutboxProcessor, OutboxOptions,
  OutboxDispatcherService, IEventTransport, LoggingEventTransport)
- AgendiaDbContext override de SaveChanges/SaveChangesAsync + EnlistDomainEvents()
- Domain/Common/Entity.cs (RaiseEvent/ClearDomainEvents), IHasDomainEvents, Domain/Events/*
- src/MRC.Agendia.Infrastructure/Notifications/ (ReminderProcessor, ReminderOptions,
  AppointmentReminderService)
SONDEA ESPECIALMENTE:
- Los eventos se vuelcan al outbox en la MISMA transaccion que el cambio (override de
  SaveChanges) y se limpian. Busca eventos que se pierden si SaveChanges lanza, o que se
  duplican si se llama dos veces.
- Entrega at-least-once -> el consumidor debe ser idempotente; verifica que el payload lleva
  solo ids + idioma (no PII) y OccurredOnUtc real.
- Outbox N-instancias: poll excluye venenosos (Attempts >= MaxAttempts, dead-letter), purga por
  retencion, y claim FOR UPDATE SKIP LOCKED. Busca condiciones de carrera entre instancias o
  mensajes que nunca se purgan.
- Reminder N-instancias: pg_try_advisory_lock (two-int, no colisiona con el guard de reservas).
  Verifica idempotencia por ReminderSentAt y crash-safety del save por item.
- Serializacion del payload (JsonSerializer options) estable y con el Type correcto.
```

### Slice 5 — Persistencia

```
SLICE: DbContext, interceptor de auditoria/soft-delete, RepositoryBase, UnitOfWork, migraciones.
FICHEROS NUCLEO:
- src/MRC.Agendia.Infrastructure/AgendiaDbContext.cs (query filters, tipos de columna, indices)
- src/MRC.Agendia.Infrastructure/Persistence/AuditableSaveChangesInterceptor.cs
- src/MRC.Agendia.Infrastructure/Persistence/UuidV7ValueGenerator.cs
- src/MRC.Agendia.Infrastructure/Repositories/RepositoryBase.cs y repos EF
- src/MRC.Agendia.Infrastructure/UnitOfWork.cs
- Migrations/ (revisar coherencia con el modelo)
SONDEA ESPECIALMENTE:
- Columnas de fechas de pared en "timestamp without time zone" (inv. 7, bug #273). Barre TODO
  el modelo, no solo Appointment: cualquier DateTime de pared nuevo (p. ej. futuros time-off).
- Query filters !IsDeleted globales; RepositoryBase preserva FindAsync/AsNoTracking/
  IgnoreQueryFilters. FindAsync aplica los filtros (decision aceptada). Busca lecturas que
  cargan padres sin IgnoreQueryFilters()+Where(!IsDeleted) (inv. 6).
- Interceptor: rellena audit fields y convierte Delete en soft delete; sin cascada, conserva
  historial. Busca entidades que deberian ser auditables y no lo son, o cascadas accidentales.
- UnitOfWork.Save: traduccion de 23505 (waitlist) por constraint name; busca otros constraints
  unicos que deberian traducirse a excepciones de dominio y no lo hacen (hoy darian 500).
- UuidV7ValueGenerator: se aplica en el Add; AuditLog.Id sigue siendo long. Busca PKs que se
  generan server-side por error.
- Indices unicos con columnas NULL: NULLS NOT DISTINCT donde haga falta.
```

### Slice 6 — Autorización

```
SLICE: Autorizacion por recurso, scope por negocio, auth M2M/JWT.
FICHEROS NUCLEO:
- src/MRC.Agendia.Infrastructure/Authorization/ResourceAuthorizationService.cs
- ICurrentBusinessScope y el filtro global por negocio
- src/MRC.Agendia.Infrastructure/ServiceAuth/ (ConfigurationServiceClientAuthenticator,
  JwtServiceTokenIssuer, ServiceClientSecretHasher PBKDF2)
- src/MRC.Agendia.Api/Services/CurrentUserContext.cs, AuthenticationSetup
SONDEA ESPECIALMENTE:
- EnsureCan* cubre owner/employee/client/admin correctamente. Sondea especialmente
  EnsureCanManageAppointmentAsync y EnsureCanCreateAppointmentAsync: un Client solo crea citas
  para su propio sub; un Employee/Owner solo dentro de su negocio.
- R7 (404-vs-403): para Owner/Employee el filtro global de scope hace que apuntar a otro negocio
  de 404 en vez de 403. Verifica que sigue NEGANDO correctamente (solo cambia el status).
- Update de recurso scoped no permite mover a otro negocio/usuario (inv. 3). Intenta construir
  un DTO malicioso mentalmente y ve si el codigo lo para.
- M2M: verificacion en TIEMPO CONSTANTE (PBKDF2 dummy anti-enumeracion). Busca early-returns que
  filtren si el client_id existe. 401 INVALID_SERVICE_CREDENTIALS con mensaje uniforme.
- Validacion del JWT de Harmony: iss/aud/clave; token ausente/invalido -> 401 sin cuerpo.
```

### Slice 7 — API transversal

```
SLICE: Middleware, pipeline, validadores FluentValidation, routing, manejo de errores.
FICHEROS NUCLEO:
- src/MRC.Agendia.Api/Middleware/ExceptionHandlingMiddleware.cs, CorrelationIdMiddleware.cs
- Api/Configuration/PipelineExtensions, CorsSetup, HealthChecksSetup, SwaggerSetup
- Todos los *Validator de FluentValidation (Application/*)
- Los Controllers (routing singular/plural, [Authorize]/RolePolicies)
SONDEA ESPECIALMENTE:
- ExceptionHandlingMiddleware mapea CADA tipo de excepcion a su status. Busca excepciones de
  dominio nuevas que caen al 500 generico por no estar mapeadas, y que docs/error-codes.md este
  al dia. Respuesta { code, message, traceId, [errors] }, traceId == correlation id.
- Validadores: un Validator por Command/Query; ids nullable con NotEqual(Guid.Empty); rangos con
  el cap de SchedulingLimits. Busca Commands sin validador y validaciones que deberian ser 400 y
  hoy revientan a 500.
- Mensajes de validacion/excepcion/log en INGLES (barrido #274). Marca los que sigan en espanol.
- RolePolicies en [Authorize] (inv. 10); busca concatenacion de strings de roles.
- Orden del pipeline (ForwardedHeaders/Cors/RateLimiter/Exception/Auth) y skips en Testing.
- CancellationToken llega desde el controller (inv. 9).
```

### Slice 8 — CRUD por feature (pasada rápida)

```
SLICE: CRUD de Business / Employee / Service (aprovisionamiento) y sus mappings.
FICHEROS NUCLEO:
- Application/Business/, Employees/, Services/ (Commands/Queries/Handlers/Validators/DTOs)
- Mappings/ (AutoMapper Profiles)
SONDEA ESPECIALMENTE:
- Aprovisionamiento: POST /api/Business (Admin, OwnerUserId obligatorio), POST /api/Employee
  (Admin u owner, UserId opcional), POST /api/Service (BusinessId + DurationMinutes). NINGUN
  Update acepta OwnerUserId/UserId/BusinessId (inv. 3); si AutoMapper mapea, debe haber .Ignore().
- Coherencia de DTOs publicos (*PublicDto) con el bounded context: Agendia NO expone
  nombre/precio del servicio ni perfil del negocio (esos son de gestion). Marca cualquier fuga.
- Soft-delete + restore (POST /api/{recurso}/{id}/restore, Admin). Autorizacion en handlers.
- Records inmutables para DTOs/Commands/Queries; una clase por fichero.
```

---

## FASE 1 — BRIEF DE VERIFICADOR (uno por slice, tras el revisor)

> Antepón el PREÁMBULO. Dale la lista de hallazgos del revisor de ese slice.

```
Eres el verificador. Recibes los hallazgos del revisor para este slice. Para CADA hallazgo:
1. Abre el file:line y comprueba el hallazgo contra el codigo REAL. No confies en el resumen
   del revisor; leelo tu.
2. Reproduce mentalmente el escenario de fallo con inputs concretos. Marca el veredicto:
   CONFIRMADO (el fallo ocurre), PLAUSIBLE (podria, depende de algo no visible aqui) o
   DESCARTADO (falso positivo; explica por que, citando el codigo/invariante que lo impide).
3. Revisa que el escenario de fallo sea REAL y no este ya cubierto por una invariante, un
   guard, un query filter o una validacion que el revisor paso por alto.
4. Ajusta severidad y confianza si procede.
Devuelve solo los CONFIRMADOS y PLAUSIBLES, ordenados por severidad, cada uno con: file:line,
resumen en una frase, escenario de fallo, veredicto, y los tests propuestos (revisados).
Si un hallazgo del revisor era un falso positivo, dilo con una linea de por que. NO anadas
hallazgos nuevos: tu trabajo es verificar, no volver a auditar.
```

---

## FASE 2 — BRIEF DE IMPLEMENTACIÓN (uno por arreglo elegido, tras consolidar)

> Esto SÍ toca código, un arreglo a la vez, con la verja de "no romper nada".

```
Implementa el siguiente arreglo en MRC.Agendia. Sigue el CLAUDE.md (convenciones, DDD,
codigo limpio, reutilizar codigo sin abstracciones prematuras).

HALLAZGO A ARREGLAR:
<pega aqui el hallazgo verificado: file:line, escenario de fallo, y el fix acordado>

TESTS A ESCRIBIR (del analisis):
<pega aqui los tests propuestos por el revisor/verificador>

PROCESO:
1. git checkout master && git pull origin master
2. Rama <num>-<slug-corto> desde master (nunca claude/xxx).
3. Implementa el fix minimo que resuelve el escenario. No arregles de paso cosas no pedidas ni
   anadas abstracciones/validaciones para casos que no pueden ocurrir.
4. Escribe los tests: unit con xUnit+NSubstitute+EF InMemory, o integracion con
   WebApplicationFactory + Testcontainers-Postgres si el fallo depende de un constraint o de
   concurrencia real. Que los tests FALLEN sin el fix y PASEN con el.
5. Mensajes de runtime (logs/excepciones/validacion) en INGLES. Si tocas una excepcion nueva,
   actualiza docs/error-codes.md.
6. VERJA DE NO-ROMPER-NADA (obligatoria antes de PR):
   - dotnet build -c Release -warnaserror  ->  0 errores, 0 warnings.
   - dotnet test  ->  todo verde, sin regresiones.
   - dotnet format tras crear ficheros (el .editorconfig exige CRLF; los nuevos salen con LF).
   Si algo falla, arreglalo antes de continuar; reporta la salida real, no digas "verde" si no.
7. Commit en espanol sin tildes, sin trailer de atribucion a Claude:
   <tipo>: <descripcion>\n\nCloses #<num>\n\n<detalle>  (tipos: fix/refactor/chore/test/feat).
8. Push + abre PR contra master con descripcion profunda (que cambia, por que, escenario que
   arregla, tests anadidos). NO auto-mergear: el usuario revisa y mergea.
```

---

## Notas de ejecución (cupo Max)

- Lanza por bloques: slices **1-4** (motor: citas, horarios, disponibilidad/waitlist, eventos),
  consolido, y luego **5-8** (persistencia, auth, API, CRUD). Así, si el cupo aprieta, tienes
  resultados parciales útiles y no medio análisis a la mitad.
- Revisor a `xhigh`, verificador a `high`. Un revisor + un verificador por slice (16 agentes en
  total, en dos tandas de ~8).
- Los slices comparten costuras (p. ej. citas ↔ guard ↔ persistencia): si un hallazgo cruza
  slices, lo anoto al consolidar para no duplicarlo.
- La Fase 2 se hace **después** de que elijas, un PR cada vez, con la verja de verificación.
