# Catálogo de códigos de error de la API

Todas las respuestas de error siguen la forma:

```json
{ "code": "...", "message": "...", "traceId": "...", "errors": { } }
```

- `code`: identificador estable y legible por máquina. El frontend debe ramificar por `code`, no por `message`.
- `message`: texto en español orientado al usuario/dev.
- `traceId`: correlación de la petición (ver `X-Correlation-Id`).
- `errors`: solo en validaciones (`VALIDATION_ERROR`), mapa `campo -> [mensajes]`.

El mapeo vive en `ExceptionHandlingMiddleware`. Las excepciones tipadas heredan de
`DomainException` (en `MRC.Agendia.Domain.Exceptions`) y llevan su propio `Code`.

## Genéricos / transversales

| Code | HTTP | Cuándo |
|---|---|---|
| `VALIDATION_ERROR` | 400 | FluentValidation falló (incluye `errors`). |
| `BAD_REQUEST` | 400 | `InvalidOperationException`/`ArgumentException` sin tipar (reglas varias). |
| `UNAUTHENTICATED` | 401 | Credenciales inválidas, cuenta bloqueada/desactivada, refresh token inválido, email sin confirmar. |
| `FORBIDDEN` | 403 | Autenticado pero sin permiso sobre el recurso (cross-tenant). |
| `NOT_FOUND` | 404 | Recurso no encontrado sin tipar (fallback heredado). |
| `INTERNAL_ERROR` | 500 | Excepción no controlada. |

## Dominio — recursos no encontrados (404)

| Code | Excepción |
|---|---|
| `BUSINESS_NOT_FOUND` | `BusinessNotFoundException` |
| `CLIENT_NOT_FOUND` | `ClientNotFoundException` |
| `EMPLOYEE_NOT_FOUND` | `EmployeeNotFoundException` |
| `SERVICE_NOT_FOUND` | `ServiceNotFoundException` |
| `APPOINTMENT_NOT_FOUND` | `AppointmentNotFoundException` |
| `APPOINTMENT_SERIES_NOT_FOUND` | `AppointmentSeriesNotFoundException` |
| `WAITLIST_ENTRY_NOT_FOUND` | `WaitlistEntryNotFoundException` |
| `SCHEDULE_TEMPLATE_NOT_FOUND` | `ScheduleTemplateNotFoundException` |
| `SCHEDULE_OVERRIDE_NOT_FOUND` | `ScheduleOverrideNotFoundException` |
| `HOLIDAY_NOT_FOUND` | `HolidayNotFoundException` |

## Dominio — reglas de negocio (400)

| Code | Excepción | Cuándo |
|---|---|---|
| `DUPLICATE_EMAIL` | `DuplicateEmailException` | Ya existe una cuenta con ese email. |
| `SCHEDULE_TEMPLATES_OVERLAP` | `TemplatesOverlapException` | Plantillas de horario con fechas solapadas. |
| `SCHEDULE_OVERRIDE_CONFLICT` | `ScheduleOverrideConflictException` | Ya existe una excepción de horario para esa fecha en el negocio. |
| `SCHEDULE_YEAR_ALREADY_EXISTS` | `ScheduleAlreadyExistsForYearException` | Se intenta generar el horario de un año que el negocio ya tiene configurado sin confirmar el reemplazo. Reenviar con `replaceExisting: true` para rehacerlo. |
| `APPOINTMENT_OUTSIDE_SCHEDULE` | `AppointmentOutsideScheduleException` | La cita cae en día cerrado o fuera de las franjas abiertas. |
| `APPOINTMENT_CONFLICT` | `AppointmentConflictException` | Se supera la capacidad (`MaxConcurrentAppointments`) del empleado. |
| `INVALID_APPOINTMENT_TIME` | `InvalidAppointmentTimeException` | Fechas de la cita ausentes, invertidas o en el pasado. |
| `INVALID_APPOINTMENT_STATUS_TRANSITION` | `InvalidAppointmentStatusTransitionException` | Se intenta cambiar el estado de una cita que ya está en un estado final (Completed/NoShow/Cancelled). |
| `EMPLOYEE_INACTIVE` | `EmployeeInactiveException` | El empleado seleccionado está inactivo. |
| `SERVICE_EMPLOYEE_MISMATCH` | `ServiceEmployeeMismatchException` | El servicio y el empleado pertenecen a negocios distintos. |
| `APPOINTMENT_DURATION_MISMATCH` | `AppointmentDurationMismatchException` | La duración de la cita no coincide con la del servicio. |
| `CANCELLATION_WINDOW_ELAPSED` | `CancellationWindowElapsedException` | Un cliente intenta cancelar/reprogramar su cita pasada la ventana de antelación del negocio (solo self-service; el personal no la sufre). |
| `SLOT_HAS_CAPACITY` | `SlotHasCapacityException` | Al apuntarse a la lista de espera, la franja todavía tiene hueco (reserva directa). |
| `DUPLICATE_WAITLIST_ENTRY` | `DuplicateWaitlistEntryException` | El cliente ya está en la lista de espera de esa franja. |

## Cómo añadir un código nuevo

1. Crea la excepción en `MRC.Agendia.Domain.Exceptions` heredando de `NotFoundException` (→404) o de `DomainException` (→400), con su `Code`.
2. Lánzala desde el servicio/validador correspondiente (no uses las builtin).
3. El middleware ya la mapea por su tipo base; añade aquí la fila del catálogo.
