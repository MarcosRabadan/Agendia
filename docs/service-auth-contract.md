# Contrato de autenticación máquina-a-máquina (Agendia ← servicios de confianza)

Este documento describe el flujo **client-credentials (OAuth2 M2M)** que Agendia
expone para que otros microservicios (el primero, **SoundMate**) llamen a su API
servicio-a-servicio, sin usuario humano y sin la contraseña de nadie.

Es el complemento del [contrato de token de usuario](harmony-token-contract.md):
aquel cubre los tokens que **emite Harmony**; éste cubre el token de servicio que
**emite Agendia** a partir de un `clientId` + `clientSecret`.

## Endpoint

```
POST /api/auth/service-token
Content-Type: application/json

{ "clientId": "soundmate", "clientSecret": "<secreto>" }
```

Respuesta `200`:

```json
{
  "accessToken": "eyJhbGci...",
  "expiresAt": "2026-07-29T12:15:00Z",
  "tokenType": "Bearer"
}
```

- **Público** (no requiere Bearer previo), pero solo funciona con un `clientSecret` válido.
- **Sin `refreshToken`.** Cuando el `accessToken` caduque, el servicio vuelve a llamar
  a este mismo endpoint con su secreto y cachea el nuevo.
- `expiresAt` es un instante **UTC** (sufijo `Z`). Úsalo para re-pedir token **antes**
  de que caduque (p. ej. renueva al 80 % de su vida).
- `400 VALIDATION_ERROR` si falta `clientId` o `clientSecret`.
- `401 INVALID_SERVICE_CREDENTIALS` si el `clientId` no existe, el secreto no coincide
  o el cliente está deshabilitado (mensaje uniforme, no revela cuál de los tres).

Para consumir la API protegida, envía el token en cada llamada:

```
Authorization: Bearer <accessToken>
```

## Contenido del token de servicio

El JWT se firma con la **misma clave/issuer/audience** que valida `AuthenticationSetup`
(la clave simétrica `Jwt:Key`, HS256), así que lo aceptan los endpoints protegidos
existentes sin ningún cambio. Claims:

| Claim | Contenido |
|---|---|
| `sub` | El `clientId` (identifica al servicio). |
| `client_id` | El `clientId` (duplica `sub` explícitamente). |
| `token_use` | `service` (marca de token de máquina). |
| `role` | El rol configurado para el cliente (v1: `Admin`, ver más abajo). |
| `iss` / `aud` | `Jwt:Issuer` / `Jwt:Audience` de Agendia. |
| `exp` / `nbf` / `iat` | Vida del token (UTC). |
| `jti` | Identificador único del token. |

## Registro de clientes de servicio (configuración)

Los clientes de confianza viven en configuración, **sin migración de BD** (v1):

```jsonc
// appsettings.json — el HASH puede ir aquí; el secreto EN CLARO nunca se commitea
"ServiceAuth": {
  "TokenLifetimeMinutes": 15
},
"ServiceClients": [
  { "clientId": "soundmate", "clientSecretHash": "<hash>", "role": "Admin", "enabled": true }
]
```

- El secreto se guarda **hasheado** (PBKDF2-HMAC-SHA256 con sal, formato
  `pbkdf2-sha256$<iter>$<salt-b64>$<hash-b64>`). La verificación es en **tiempo constante**.
- El secreto en claro va en **user-secrets** (dev) o **variables de entorno** (prod),
  nunca en el repositorio.
- `enabled: false` deja al cliente sin poder obtener token (revocación sin borrar).

### Generar el `clientSecretHash`

El hash lo produce `ServiceClientSecretHasher.Hash(secret)`
(`src/MRC.Agendia.Infrastructure/ServiceAuth/`). Como cada hash lleva sal aleatoria,
hay que generarlo una vez a partir del secreto elegido y pegar el resultado en la
config. Cualquier vía que invoque ese método sirve; por ejemplo un test de un solo uso:

```csharp
// Escribe el hash a copiar en ServiceClients[].clientSecretHash
System.Console.WriteLine(
    MRC.Agendia.Infrastructure.ServiceAuth.ServiceClientSecretHasher.Hash("EL-SECRETO-ELEGIDO"));
```

En producción, además, sobrescribe `ServiceClients__0__ClientSecretHash` (y el resto de
índices) por variable de entorno si no quieres el hash en el fichero.

## Autorización: qué puede hacer el token (v1)

SoundMate opera **a través de varios negocios** (cada academia de SoundMate = un
negocio de Agendia), así que el token de servicio **no** puede quedar atado a un
negocio. En v1 se emite con el rol **`Admin`** (opción A de la issue #232):

- Pasa las `[Authorize(Roles = ...)]` existentes (Admin está en todas las combinaciones).
- `ResourceAuthorizationService` da bypass a `Admin` en todas sus comprobaciones.
- `CurrentBusinessScope` no restringe a `Admin` → acceso **transversal** a todos los negocios.

El token queda marcado como servicio (`token_use=service`, `client_id`) para auditoría
y para poder acotarlo en el futuro a un rol `Service` dedicado (opción B) sin cambiar el
flujo de emisión. Cambiar el `role` del cliente en config es el único punto a tocar.

## Seguridad

- Secreto **hasheado** en reposo; secreto real fuera del repo (user-secrets / env).
- Comparación del secreto en **tiempo constante** (`CryptographicOperations.FixedTimeEquals`).
- El token de servicio **no** lo puede obtener un usuario final: solo con el secreto.
- Cada intento (éxito y fallo) se **audita** (`SERVICE_TOKEN_ISSUED` / `SERVICE_TOKEN_DENIED`
  con el `client_id`) en `AuditLog`.
- **Fuera de alcance v1** (mejoras futuras): rate-limiting del endpoint, y "on-behalf-of"
  (que SoundMate indique el usuario final en cuyo nombre actúa). Hoy SoundMate valida la
  membresía del usuario en su lado antes de delegar.

## Qué necesita SoundMate para integrarse

| Dato | Valor |
|---|---|
| Endpoint de token | `POST {baseUrl}/api/auth/service-token` |
| Base URL de Agendia | *(por entorno; la entrega infraestructura)* |
| `clientId` | `soundmate` |
| `clientSecret` | *(se entrega por canal seguro; va en los user-secrets/env de SoundMate)* |
| Vida del token | `ServiceAuth:TokenLifetimeMinutes` (default 15 min) → cachear y renovar antes de `expiresAt` |
| Rol/permisos | `Admin` (acceso transversal a todos los negocios) |
| Uso | `Authorization: Bearer <accessToken>` en cada llamada |
