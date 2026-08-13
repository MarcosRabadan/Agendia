# Contrato de eventos de integración (Agendia → consumidor)

Desde la Fase 5 del epic #241, **Agendia ya no envía notificaciones** (email/push). En su
lugar **publica eventos de dominio** y un servicio consumidor (Notifications/Harmony)
resuelve el contacto del destinatario (email/teléfono/nombre) por su `clientUserId` y
entrega el mensaje en el `language` indicado.

## Transporte

- Los eventos se escriben en una **tabla outbox** (`OutboxMessages`) en la MISMA
  transacción que la operación que los origina (no se pierden si el broker está caído).
- Un `OutboxDispatcherService` (background) los entrega a través de `IEventTransport`.
- **Hoy `IEventTransport` es `LoggingEventTransport` (log-only):** el broker del sistema
  (RabbitMQ / Azure Service Bus / Kafka) aún no está decidido. Cuando se elija, se
  sustituye ese único registro en `Infrastructure/DependencyInjection` por el adaptador
  real; nada más cambia (publisher, outbox y dispatcher son agnósticos).
- Entrega **at-least-once**: el consumidor debe ser idempotente (deduplicar por
  `appointmentId`/`waitlistEntryId` + tipo).

## Formato del mensaje

Cada fila del outbox es `{ Type, Payload, OccurredOnUtc }`:

- `Type`: nombre del evento (discriminador), p. ej. `AppointmentConfirmed`.
- `Payload`: JSON del evento (camelCase, opciones `JsonSerializerDefaults.Web`).

Agendia **no incluye email/nombre/teléfono** (no los posee): el consumidor los resuelve
por `clientUserId` (el `sub` de Harmony).

## Eventos

| `Type`                 | Cuándo                                                  |
|------------------------|--------------------------------------------------------|
| `AppointmentConfirmed` | Al crear una cita.                                     |
| `AppointmentCancelled` | Al pasar una cita a `Cancelled`.                       |
| `AppointmentReminder`  | Job de recordatorio 24h (idempotente por `ReminderSentAt`). |
| `AppointmentDelayed`   | El personal avisa de un retraso, por cita afectada.   |
| `WaitlistSlotAvailable`| Se libera una franja que un cliente esperaba (FIFO).  |

### Payload de los eventos de cita

`AppointmentConfirmed` / `AppointmentCancelled` / `AppointmentReminder`:

```json
{
  "appointmentId": 123,
  "businessId": 10,
  "employeeId": 2,
  "clientUserId": "harmony-sub-abc",
  "serviceId": 3,
  "startDate": "2026-09-01T09:00:00",
  "endDate": "2026-09-01T09:30:00",
  "language": "es",
  "occurredOnUtc": "2026-08-13T16:20:00Z"
}
```

`AppointmentDelayed` añade `"delayMinutes": 15`.

### Payload de `WaitlistSlotAvailable`

```json
{
  "waitlistEntryId": 77,
  "businessId": 10,
  "employeeId": 2,          // null = "cualquier empleado"
  "clientUserId": "harmony-sub-abc",
  "serviceId": 3,
  "date": "2026-09-01",
  "startTime": "16:00:00",
  "language": "es",
  "occurredOnUtc": "2026-08-13T16:20:00Z"
}
```

## Notas de implementación (Agendia)

- Contratos de evento: `Domain/Events/*` (records inmutables, marcador `IIntegrationEvent`).
- Puerto de publicación: `Application/Events/IEventPublisher` (enlista en el outbox, no hace Save).
- Outbox + dispatcher + transporte: `Infrastructure/Messaging/*`.
- Config opcional del dispatcher: `Outbox:PollIntervalSeconds` (10), `Outbox:BatchSize` (20).
- El idioma (`language`) viene de `Business.DefaultLanguage`.
