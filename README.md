# MRC.Agendia

[![CI](https://github.com/MarcosRabadan/Agendia/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/MarcosRabadan/Agendia/actions/workflows/ci.yml)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-private-lightgrey)](#)

API de gestión de citas para negocios. .NET 9.0 con Clean Architecture + DDD + CQRS.

> 📖 Si vas a trabajar en el proyecto (humano o agente), lee también:
> - **[CLAUDE.md](./CLAUDE.md)** — contexto técnico que se carga automáticamente al abrir Claude Code
> - **[CONTRIBUTING.md](./CONTRIBUTING.md)** — workflow de ramas, commits y PRs

## Stack

- .NET 9.0 + ASP.NET Core Web API
- Entity Framework Core 8.0 + SQL Server
- MediatR (CQRS) + AutoMapper
- ASP.NET Identity + JWT Bearer
- Serilog + Seq
- Swagger / OpenAPI

## Requisitos previos

- .NET 9 SDK
- SQL Server (local o LocalDB)
- (Opcional) [Seq](https://datalust.co/seq) en `http://localhost:5341` para visualizar logs

## Configuración de secretos

Los secretos sensibles **NO están en `appsettings.json`** por seguridad. Hay que configurarlos en local antes de arrancar la app.

### En desarrollo (user-secrets)

```bash
cd src/MRC.Agendia.Api

# Clave JWT (genera una aleatoria fuerte)
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"

# Admin inicial (se crea en el primer arranque)
dotnet user-secrets set "AdminSeed:Email" "admin@agendia.local"
dotnet user-secrets set "AdminSeed:Password" "TuPasswordFuerte123!"
dotnet user-secrets set "AdminSeed:FullName" "Administrador"
```

Los secretos quedan en `%APPDATA%/Microsoft/UserSecrets/<UserSecretsId>/secrets.json` (Windows) — **fuera del repositorio**.

### En producción (variables de entorno)

```
Jwt__Key=<clave aleatoria de al menos 32 chars>
AdminSeed__Email=admin@tu-dominio.com
AdminSeed__Password=<password fuerte>
AdminSeed__FullName=Administrador
ConnectionStrings__DefaultConnection=Server=...;Database=...;...
```

⚠️ Si `Jwt:Key` no está configurada o tiene menos de 32 caracteres, la app **no arranca** (fail-fast).

⚠️ Si `AdminSeed:Email` o `AdminSeed:Password` están vacíos, el seed del admin se omite y se registra un warning.

## CORS por entorno

La política CORS se configura desde la sección `Cors:AllowedOrigins`. La lista vive en `appsettings.<Entorno>.json` o en variables de entorno.

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5173",
    "https://app.tu-dominio.com"
  ]
}
```

Comportamiento:

- **Con orígenes definidos** → política restringida a esos hosts (`WithOrigins`).
- **Lista vacía en Development** → fallback a `AllowAnyOrigin` con un warning en logs. Sirve para arrancar sin fricción, pero conviene definir orígenes explícitos.
- **Lista vacía fuera de Development** → la app **no arranca** (fail-fast).

En producción, define los orígenes como variables de entorno (uno por índice):

```
Cors__AllowedOrigins__0=https://app.tu-dominio.com
Cors__AllowedOrigins__1=https://admin.tu-dominio.com
```

## Forwarded Headers (IP real tras proxy)

En producción la API normalmente vive detrás de un proxy o load balancer (nginx, Cloudflare, AWS ALB, Azure App Service, etc.). Sin configurar `ForwardedHeaders`, **todas** las peticiones llegan con la IP del proxy → el rate limiter del #42 mete a todo el mundo en la misma cuota y un único atacante consume el cupo global.

La API activa `UseForwardedHeaders` automáticamente cuando `ASPNETCORE_ENVIRONMENT` no es ni `Development` ni `Testing`. Lee `X-Forwarded-For` y `X-Forwarded-Proto` y sustituye la IP/esquema que ven el rate limiter, los logs y la autenticación.

**Por seguridad, ASP.NET solo confía en proxies loopback por defecto.** Si tu proxy está en otra máquina (Cloudflare, ALB), tienes que decírselo:

```json
"ForwardedHeaders": {
  "KnownProxies": ["203.0.113.1"],
  "KnownNetworks": ["10.0.0.0/8"]
}
```

O como variables de entorno:

```
ForwardedHeaders__KnownProxies__0=203.0.113.1
ForwardedHeaders__KnownNetworks__0=10.0.0.0/8
```

Si no añades ningún proxy y el tuyo no es loopback, ASP.NET **ignora** los headers y vuelves al problema original. Esto es **deseado**: evita que un cliente malicioso falsifique `X-Forwarded-For` desde fuera y se salte el rate limit.

## Arrancar la aplicación

```bash
dotnet run --project src/MRC.Agendia.Api
```

En **Development** las migraciones EF Core pendientes se aplican **automáticamente al arrancar** (puedes verlo en los logs: `aplicando N migracion(es) pendiente(s)...`). No hace falta ejecutar `dotnet ef database update` a mano.

Si quieres aplicar migraciones sin arrancar la API (p.ej. desde un script):

```bash
dotnet ef database update --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api
```

En **Producción** la auto-migración está desactivada por seguridad: las migraciones se aplican vía el pipeline de despliegue, no en el arranque de la app.

Swagger UI: `https://localhost:<puerto>/`

## Estructura del proyecto

```
src/
├── MRC.Agendia.Api/              ← Controllers, Program.cs, middleware
├── MRC.Agendia.Application/      ← Commands, Queries, Handlers, DTOs, Services
├── MRC.Agendia.Domain/           ← Entidades, enums, interfaces, domain services
├── MRC.Agendia.Infrastructure/   ← EF Core, repositorios, Identity, JWT
└── MRC.Agendia.Shared/           ← Utilidades transversales
```

## Auth flow

1. `POST /api/auth/register/client` (público) — registro de cliente
   `POST /api/auth/register/owner` (público) — registro de owner + su negocio
2. `POST /api/auth/login` (público) — devuelve `accessToken` (15 min) + `refreshToken` (7 días)
3. Llamadas autenticadas: header `Authorization: Bearer <accessToken>`
4. `POST /api/auth/refresh` cuando el access expira — rota el refresh token
5. `POST /api/auth/logout` revoca el refresh token

Roles: `Admin`, `BusinessOwner`, `Employee`, `Client`.

## Scripts útiles

- `scripts/create-github-issues.sh` — crea el backlog de issues en GitHub (requiere `gh` CLI)
