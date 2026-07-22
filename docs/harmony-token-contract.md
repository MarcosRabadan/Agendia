# Contrato de token: Harmony → Agendia

Agendia es un microservicio de Harmony. **No emite tokens ni guarda credenciales**:
Harmony es el servicio de identidad, registra a los usuarios en su propia base de
datos, firma los access tokens y llama a Agendia con ellos. Agendia solo los valida.

Este documento es el contrato entre ambos. Si Harmony se desvía de él, Agendia
responderá `401` (token rechazado) o, peor y más confuso, `403` en absolutamente
todos los endpoints: el token valida, la petición se autentica, y luego cada
comprobación de autorización falla porque no encontró la identidad del llamante.

## Firma

| Parámetro | Valor |
|---|---|
| Algoritmo | `HS256` (pineado; ningún otro se acepta, tampoco `none`) |
| Clave | Secreto simétrico **compartido**, idéntico en Harmony y en Agendia |
| `iss` | Debe coincidir con `Jwt:Issuer` de Agendia |
| `aud` | Debe coincidir con `Jwt:Audience` de Agendia |
| Margen de reloj | 1 minuto |

La clave se configura en Agendia como `Jwt:Key` (variable de entorno `Jwt__Key` en
producción, `dotnet user-secrets` en desarrollo). Debe tener **al menos 32 bytes**;
la aplicación no arranca si falta o es más corta.

> **Consecuencia de usar clave simétrica:** Agendia tiene la misma clave con la que
> se *firma*, así que técnicamente podría fabricar tokens válidos. Es aceptable
> porque ambos servicios son del mismo equipo y Agendia no se expone a internet,
> pero implica dos cosas: rotar la clave exige desplegar Harmony y Agendia de forma
> coordinada, y si algún día Agendia deja de ser de confianza (o aparecen más
> consumidores), hay que migrar a RS256 con JWKS.

## Claims requeridos

Harmony debe emitir los nombres **cortos** de JWT:

| Claim | Contenido | Obligatorio |
|---|---|---|
| `sub` | Identificador de usuario de Harmony. Opaco, estable e **inmutable**. | Sí |
| `role` | Rol del usuario. Puede repetirse para varios roles. | Sí, salvo endpoints sin rol |
| `exp` | Expiración. | Sí |

Valores válidos de `role` (deben coincidir **literalmente**, ver
`src/MRC.Agendia.Domain/Constants/Roles.cs`):

- `Admin`
- `BusinessOwner`
- `Employee`
- `Client`

### Por qué el mapeo de claims importa tanto

Agendia lee la identidad con `ClaimTypes.NameIdentifier` y los roles con
`ClaimTypes.Role` (las URIs largas de .NET). El mapeo de entrada de JwtBearer
traduce `sub` → `NameIdentifier` y `role` → `Role` automáticamente, y por eso
`AuthenticationSetup` fija `MapInboundClaims = true` **de forma explícita** en vez
de confiar en el default.

Si alguien lo pone a `false`, los tokens siguen validando pero
`ICurrentUserContext.UserId` pasa a ser `null` y **todo devuelve 403**. Es el fallo
más caro de diagnosticar de esta integración.

`HarmonyTokenContractTests` cubre esto de punta a punta. Si esos tests fallan, la
integración con Harmony está rota: no los relajes.

## El `sub` es una clave de negocio en Agendia

El `sub` no se usa solo para autenticar: se **persiste**. Agendia guarda ese valor en
`Business.OwnerUserId`, `Employee.UserId`, `Client.UserId`, `DeviceToken.UserId` y
`AuditLog.UserId`, y la autorización por recurso compara el `sub` del token contra
esas columnas.

Por tanto, **si Harmony cambia el `sub` de un usuario, ese usuario pierde el acceso a
todo lo suyo en Agendia**. Debe ser inmutable de por vida.

## Aprovisionamiento

Agendia no crea usuarios. Harmony los registra en su lado y luego llama a Agendia
para crear la entidad de negocio correspondiente, pasando el `sub` del usuario:

| Entidad | Endpoint | Campo | Autorización |
|---|---|---|---|
| Negocio | `POST /api/Business` | `OwnerUserId` (**obligatorio**) | `Admin` |
| Empleado | `POST /api/Employee` | `UserId` (opcional) | `Admin` u owner del negocio |
| Cliente | `POST /api/Client` | `UserId` (opcional) | `Admin` |

`UserId` es opcional en empleado y cliente porque no todos tienen cuenta: un empleado
puede ser un recurso sin login (una sala, un sillón) y un cliente puede ser un
registro de mostrador o de teléfono.

**Ningún DTO de `Update` acepta estos campos.** Es deliberado: poder repuntar una
entidad existente a otro usuario permitiría regalar —o robar— el acceso a un negocio
con un DTO manipulado. Es el mismo vector que la regla de no incluir `BusinessId` en
los DTOs de update.

## Lo que Agendia ya NO hace

Todo esto vive ahora en Harmony y **no debe reintroducirse** aquí:

- Registro, login, logout, refresh tokens
- Cambio de contraseña, recuperación, confirmación de email
- Almacenamiento de credenciales (no hay tablas `AspNet*`)
- Rate limiting de auth (throttlear es responsabilidad del borde público de Harmony)
