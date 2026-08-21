# Wiki de Agendia

El motor de reservas y disponibilidad de la plataforma: un **microservicio de agenda** para
academias de música y profesores particulares. Define cómo trabajas y Agendia calcula los huecos,
acepta reservas sin errores y avisa a todo el mundo.

La wiki se organiza **por versión**. Cada versión tiene **una sola página** con las dos
explicaciones dentro: una **funcional** (para cualquiera, con ejemplos) y una **técnica** (a fondo,
para el equipo de desarrollo).

## Versiones

| Versión | Estado | Contenido |
|---|---|---|
| [**v0.2.0**](v0.2.0.md) | Actual | Reservas puras: el epic #241, seis funciones nuevas y la auditoría |
| [v0.1.0](v0.1.0.md) | Histórica | Retrato del servicio **antes** del epic #241 |

> **Ojo con la v0.1.0.** No está solo incompleta: está **desfasada**. Describe servicios con precio y
> una entidad `Client` que ya no existen, y usa peluquerías y clínicas como ejemplos. Sigue siendo
> útil como retrato del "antes", pero para saber cómo funciona Agendia hoy, la buena es la v0.2.0.

## Documentos de referencia (repo)

- [`docs/bounded-context.md`](../bounded-context.md) — propiedad de datos: qué es de Agendia y qué de los servicios vecinos.
- [`docs/events-contract.md`](../events-contract.md) — contrato de los eventos de integración (Agendia → consumidor).
- [`docs/harmony-token-contract.md`](../harmony-token-contract.md) — contrato del token de usuario (Harmony → Agendia).
- [`docs/service-auth-contract.md`](../service-auth-contract.md) — contrato del token de servicio (máquina-a-máquina).
- [`docs/error-codes.md`](../error-codes.md) — catálogo de códigos de error de la API.
