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
| `SCHEDULE_TEMPLATE_NOT_FOUND` | `ScheduleTemplateNotFoundException` |
| `SCHEDULE_OVERRIDE_NOT_FOUND` | `ScheduleOverrideNotFoundException` |
| `HOLIDAY_NOT_FOUND` | `HolidayNotFoundException` |

## Dominio — reglas de negocio (400)

| Code | Excepción | Cuándo |
|---|---|---|
| `DUPLICATE_EMAIL` | `DuplicateEmailException` | Ya existe una cuenta con ese email. |
| `SCHEDULE_TEMPLATES_OVERLAP` | `TemplatesOverlapException` | Plantillas de horario con fechas solapadas. |
| `APPOINTMENT_OUTSIDE_SCHEDULE` | `AppointmentOutsideScheduleException` | La cita cae en día cerrado o fuera de las franjas abiertas. |
| `APPOINTMENT_CONFLICT` | `AppointmentConflictException` | Se supera la capacidad (`MaxConcurrentAppointments`) del empleado. |

## Cómo añadir un código nuevo

1. Crea la excepción en `MRC.Agendia.Domain.Exceptions` heredando de `NotFoundException` (→404) o de `DomainException` (→400), con su `Code`.
2. Lánzala desde el servicio/validador correspondiente (no uses las builtin).
3. El middleware ya la mapea por su tipo base; añade aquí la fila del catálogo.
