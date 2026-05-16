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

## Arrancar la aplicación

```bash
# 1. Aplicar migraciones
dotnet ef database update --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# 2. Lanzar la API
dotnet run --project src/MRC.Agendia.Api
```

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
2. `POST /api/auth/login` (público) — devuelve `accessToken` (15 min) + `refreshToken` (7 días)
3. Llamadas autenticadas: header `Authorization: Bearer <accessToken>`
4. `POST /api/auth/refresh` cuando el access expira — rota el refresh token
5. `POST /api/auth/logout` revoca el refresh token

Roles: `Admin`, `BusinessOwner`, `Employee`, `Client`.

## Scripts útiles

- `scripts/create-github-issues.sh` — crea el backlog de issues en GitHub (requiere `gh` CLI)
