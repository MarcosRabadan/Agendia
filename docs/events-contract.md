# Contrato de eventos de integración (Agendia → consumidor)

Desde la Fase 5 del epic #241, **Agendia ya no envía notificaciones** (email/push). En su
lugar **publica eventos de dominio** y un servicio consumidor (Notifications/SoundMate, antes
*Harmony*)
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

### Las fechas: dos tipos distintos, y no se parsean igual

Esto es lo más fácil de equivocar del contrato (#321). Un payload lleva **dos clases de
fecha** y solo una de ellas es un instante real:

| Campo | Qué es | Formato |
|---|---|---|
| `startDate`, `endDate`, `previousStartDate`, `previousEndDate`, `date`, `startTime` | **Hora de pared** del negocio | Sin zona: `"2026-09-01T09:00:00"` |
| `occurredOnUtc`, `holdUntil` | **Instante real** en UTC | Con `Z`: `"2026-08-13T16:20:00Z"` |

> ⚠️ **Las horas de pared NO son UTC.** `"2026-09-01T09:00:00"` significa *las nueve de la
> mañana en el reloj del negocio*, no las 09:00 UTC. Parsearla como UTC —o como hora local
> del proceso consumidor— hace que el aviso al alumno **anuncie una hora equivocada**, que
> es justo el dato que ese aviso existe para dar.

Por eso todo payload con fechas de pared incluye **`timeZone`**, el identificador IANA de la
zona del negocio:

```json
"timeZone": "Europe/Madrid"
```

Para convertir una hora de pared a instante real: interpretar el valor **en `timeZone`**.
En .NET, `TimeZoneInfo.ConvertTimeToUtc(startDate, TimeZoneInfo.FindSystemTimeZoneById(timeZone))`
con el `DateTime` en `Kind=Unspecified`.

> **Hoy la zona es única para toda la instalación** (`Scheduling:TimeZone`, por defecto
> `Europe/Madrid`), pero viaja en el payload a propósito: el consumidor no debe depender de
> la configuración de Agendia, y el día que un negocio tenga zona propia el campo ya está.

## Eventos

| `Type`                 | Cuándo                                                  |
|------------------------|--------------------------------------------------------|
| `AppointmentConfirmed` | Al crear una cita, y al **reasignarla a otro cliente** (para el que entra, #296). |
| `AppointmentCancelled` | Al pasar una cita a `Cancelled`, al **borrarla** estando viva (`DELETE`, #296) y al **reasignarla a otro cliente** (para el que sale, con su `clientUserId`). |
| `AppointmentRescheduled` | Al mover una cita a otro horario (no en una cancelación ni en un cambio de titular). |
| `AppointmentReminder`  | Job de recordatorio 24h (idempotente por `ReminderSentAt`). |
| `AppointmentDelayed`   | El personal avisa de un retraso, por cita afectada.   |
| `WaitlistSlotAvailable`| Se libera una franja que un cliente esperaba (FIFO). Lleva `holdUntil`: la franja queda **reservada para él** hasta ese instante (#268). |

> **Series:** las operaciones de serie emiten los mismos eventos **por ocurrencia**: crear una
> serie emite un `AppointmentConfirmed` por cita creada; cancelar una serie, un
> `AppointmentCancelled` por ocurrencia futura cancelada; mover una serie, un
> `AppointmentRescheduled` por ocurrencia movida. **Borrar una serie sigue sin emitir evento**,
> a diferencia ya de la cita individual, cuyo `DELETE` sí avisa desde #296: la decisión se tomó
> para el endpoint individual y la de serie está pendiente de decidir.

> **Un update, un solo evento.** El update de una cita elige **uno** según lo que cambió, por
> este orden: si pasa a `Cancelled` → `AppointmentCancelled`; si cambia de titular →
> `AppointmentCancelled` (para el anterior) + `AppointmentConfirmed` (para el nuevo, ya con la
> hora final); si solo cambia el horario → `AppointmentRescheduled`. Un cambio de titular que
> además mueve la cita **no** emite además un `AppointmentRescheduled`: los dos eventos de
> arriba ya llevan la hora definitiva.

### Payload de los eventos de cita

`AppointmentConfirmed` / `AppointmentCancelled` / `AppointmentReminder`:

```json
{
  "appointmentId": "0198f3a1-7c4e-7b2a-9f01-2c3d4e5f6a7b",
  "businessId": "0198f3a1-7c4e-7b2a-9f01-111111111111",
  "employeeId": "0198f3a1-7c4e-7b2a-9f01-222222222222",
  "clientUserId": "harmony-sub-abc",
  "serviceId": "0198f3a1-7c4e-7b2a-9f01-333333333333",
  "startDate": "2026-09-01T09:00:00",
  "endDate": "2026-09-01T09:30:00",
  "language": "es",
  "timeZone": "Europe/Madrid",
  "occurredOnUtc": "2026-08-13T16:20:00Z"
}
```

`startDate`/`endDate` son **hora de pared** de `timeZone`; `occurredOnUtc` es UTC. Ver
[Las fechas](#las-fechas-dos-tipos-distintos-y-no-se-parsean-igual).

Los identificadores de entidad (`appointmentId`, `businessId`, `employeeId`,
`serviceId`, `waitlistEntryId`) son **GUID (UUIDv7)** desde la Fase 6; `clientUserId`
es el `sub` opaco de Harmony (string, no GUID).

`AppointmentDelayed` añade `"delayMinutes": 15`.

`AppointmentRescheduled` añade `"previousStartDate"` y `"previousEndDate"` (el horario anterior);
`startDate`/`endDate` son el nuevo horario.

### Payload de `WaitlistSlotAvailable`

```json
{
  "waitlistEntryId": "0198f3a1-7c4e-7b2a-9f01-444444444444",
  "businessId": "0198f3a1-7c4e-7b2a-9f01-111111111111",
  "employeeId": "0198f3a1-7c4e-7b2a-9f01-222222222222",  // null = "cualquier empleado"
  "clientUserId": "harmony-sub-abc",
  "serviceId": "0198f3a1-7c4e-7b2a-9f01-333333333333",
  "date": "2026-09-01",
  "startTime": "16:00:00",
  "holdUntil": "2026-08-13T16:35:00Z",
  "language": "es",
  "timeZone": "Europe/Madrid",
  "occurredOnUtc": "2026-08-13T16:20:00Z"
}
```

**`holdUntil` (#268)** es la **reserva prioritaria**: hasta ese instante (UTC) la franja queda
bloqueada para ese cliente y la API rechaza a cualquier otro que intente reservarla
(400 `SLOT_ON_HOLD`). El mensaje al cliente debería decir de cuánto tiempo dispone. Si no
reserva a tiempo, el hold expira y Agendia emite un `WaitlistSlotAvailable` nuevo para el
siguiente de la cola.

## Notas de implementación (Agendia)

- Contratos de evento: `Domain/Events/*` (records inmutables, marcador `IIntegrationEvent`).
- Publicación: las entidades **registran eventos de dominio** (`Domain/Common/Entity.RaiseEvent`,
  vía `IHasDomainEvents`) al cambiar de estado; el **override de `SaveChanges` de
  `AgendiaDbContext`** los vuelca al outbox en la MISMA transacción que el cambio, y luego los
  limpia. No hay un publisher aparte.
- Outbox + dispatcher + transporte: `Infrastructure/Messaging/*`.
- Config opcional del dispatcher: `Outbox:PollIntervalSeconds` (10), `Outbox:BatchSize` (20).
- El idioma (`language`) viene de `Business.DefaultLanguage`.
