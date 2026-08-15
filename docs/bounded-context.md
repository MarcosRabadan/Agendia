# Contexto acotado de Agendia (propiedad de datos)

Agendia es el microservicio de **agenda**: reserva pura. Tras el epic #241 ya **no es
dueño de la identidad** (eso es Harmony) **ni del perfil/catálogo** del negocio (eso es
el servicio de gestión/catálogo). Este documento fija qué dato pertenece a quién, qué
identificadores cruzan el límite del servicio **sin FK**, y cómo se aprovisiona una
agenda desde fuera.

Complementa a:
- [`harmony-token-contract.md`](harmony-token-contract.md) — cómo llega y se valida la identidad.
- [`events-contract.md`](events-contract.md) — cómo salen los avisos (Agendia no notifica; publica eventos).

## Qué posee cada servicio

| Dato | Dueño | En Agendia |
|---|---|---|
| Usuario, credenciales, roles, email/teléfono/nombre de la persona | **Harmony** (identidad) | Solo el `sub` opaco, como referencia sin FK. |
| Perfil del negocio: nombre, dirección, contacto | **Gestión** | No. Agendia solo guarda estado de agenda del negocio. |
| Catálogo de servicio: nombre, descripción, **precio** | **Gestión / catálogo** | No. Agendia solo guarda la **duración** del servicio. |
| Perfil del empleado: nombre, contacto | **Gestión / Harmony** | No. Agendia solo guarda sus atributos de agenda. |
| Estado de agenda del negocio (`IsActive`, ventana de cancelación, idioma, estado por defecto) | **Agendia** | Sí. `Business`. |
| Recurso reservable y su capacidad (`IsActive`, `MaxConcurrentAppointments`) | **Agendia** | Sí. `Employee` (persona, sala, sillón…). |
| Duración del servicio (para maquetar disponibilidad y validar la cita) | **Agendia** | Sí. `Service.DurationMinutes`. |
| Horarios: plantillas, excepciones, festivos, vacaciones | **Agendia** | Sí. `ScheduleTemplate` / `ScheduleOverride` / `HolidayCalendar`. |
| Citas, series, lista de espera | **Agendia** | Sí. `Appointment` / `WaitlistEntry`. |
| Registro de auditoría de las operaciones de agenda | **Agendia** | Sí. `AuditLog`. |

> Regla mental: si un dato describe **quién es** alguien o **qué vende** el negocio, no es
> de Agendia. Si describe **cuándo** se puede reservar y **quién tiene qué cita**, sí.

## Identificadores que cruzan el límite (sin FK)

No hay claves foráneas entre servicios; se comparten identificadores opacos y cada lado
es responsable de su consistencia.

| Identificador | Tipo | Lo emite | Agendia lo guarda en |
|---|---|---|---|
| `sub` (usuario de Harmony) | `string` opaco | Harmony | `Business.OwnerUserId`, `Employee.UserId` (opcional), `Appointment.ClientUserId`, `WaitlistEntry.ClientUserId`, `AuditLog.UserId` |
| `Business.Id` / `Employee.Id` / `Service.Id` | `Guid` (UUIDv7) | **Agendia** | Los usa el servicio de gestión para colgar el perfil/precio de la entidad de agenda. |

Notas:
- El `sub` es **inmutable de por vida**: si Harmony lo cambia, el usuario pierde el acceso
  a todo lo suyo en Agendia (ver contrato de token). No es un GUID y no se convierte a uno.
- Las PK/FK **internas** de Agendia son `Guid` UUIDv7 (Fase 6). `AuditLog.Id` es la única
  excepción: `long` autoincremental (secuencia de log, no se referencia desde fuera).
- El cliente **no tiene entidad** en Agendia: una cita guarda su `sub` directamente en
  `ClientUserId`. No se aprovisiona ni se persiste su perfil.

## Contrato de aprovisionamiento

Agendia no crea usuarios ni perfiles. Alguien de fuera (Harmony/gestión, con rol `Admin`
o el owner del negocio) crea las entidades de **agenda** pasando el `sub` cuando aplica.

| Entidad | Endpoint | Campo de enlace | Autorización |
|---|---|---|---|
| Negocio (contenedor de agenda) | `POST /api/Business` | `OwnerUserId` (**obligatorio**) | `Admin` |
| Empleado (recurso reservable) | `POST /api/Employee` | `UserId` (**opcional**: una sala no tiene login) | `Admin` u owner del negocio |
| Servicio (proyección de agenda) | `POST /api/Service` | — (solo `BusinessId` + `DurationMinutes`) | `Admin` u owner del negocio |
| Cliente | — | — (la cita guarda el `sub` en `ClientUserId`) | No se aprovisiona |

**Ningún DTO de `Update` acepta `OwnerUserId`, `UserId` ni `BusinessId`.** Repuntar una
entidad existente a otro usuario o a otro negocio permitiría regalar —o robar— el acceso
con un DTO manipulado (mismo vector que la regla de no incluir `BusinessId` en los updates).

## Consecuencias de diseño

- Agendia **compone datos por id, no por join**: para pintar una cita con el nombre del
  cliente o el precio del servicio, el consumidor cruza el `sub`/`Id` con Harmony/gestión.
  Agendia nunca devuelve esos campos porque no los tiene.
- Los **eventos de integración** llevan solo ids + idioma; el consumidor resuelve el
  contacto por `clientUserId` (ver `events-contract.md`).
- Una divergencia de identificadores (un `sub` reasignado, un `Business.Id` borrado en
  gestión) **no la detecta una FK**: es responsabilidad de la coordinación entre servicios.
