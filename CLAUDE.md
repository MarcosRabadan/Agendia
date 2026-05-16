# Claude project notes — MRC.Agendia

Este fichero se carga **automáticamente** al inicio de cada sesión de Claude Code.
Lee con atención antes de tocar código.

## Qué es este proyecto

API REST de gestión de citas para negocios (peluquerías, clínicas, talleres…).
Cubre alta de negocios, empleados, servicios, clientes y citas, con un sistema de
horarios potente (plantillas anuales, festivos, vacaciones, overrides por día,
turnos partidos).

- **Idioma del producto:** Español
- **Idioma del código y los commits:** Español neutro sin tildes en commits (`git` no muestra bien tildes en algunos terminales). Comentarios en código pueden ir en español o inglés indistintamente.
- **Idioma de la documentación visible al usuario** (Swagger summaries, mensajes de error): Español.

## Stack

- **.NET 9.0** + ASP.NET Core Web API
- **Clean Architecture + DDD** (capas: Api / Application / Domain / Infrastructure)
- **CQRS con MediatR** — cada caso de uso es un Command/Query + Handler
- **AutoMapper** — Entity ↔ DTO
- **Entity Framework Core 8.0.11** + SQL Server
- **ASP.NET Identity** + **JWT Bearer** para auth
- **Serilog + Seq** (`http://localhost:5341`) para logging
- **Swagger / OpenAPI** documentando todo

## Estructura de carpetas

```
src/
├── MRC.Agendia.Api/              ← Controllers, Program.cs, middleware
├── MRC.Agendia.Application/      ← Commands/Queries/Handlers, DTOs, Services, AutoMapper profiles
├── MRC.Agendia.Domain/           ← Entidades, enums, interfaces, domain services, constants
├── MRC.Agendia.Infrastructure/   ← EF Core, DbContext, repositorios, Identity, JWT impl
└── MRC.Agendia.Shared/           ← Utilidades transversales (poco usado)
tests/
└── MRC.Agendia.API.Tests/        ← Tests (esqueleto, todavía sin cobertura)
scripts/
└── create-github-issues.sh       ← Backlog bulk creation
```

## Flujo de una petición

```
Controller (Api)
   → MediatR.Send(Command/Query)
      → Handler
         → IResourceAuthorizationService.EnsureCan*Async(...)  ← Resource-based auth si aplica
         → Service (Application)
            → Repository (Infrastructure / EF Core)
               → SQL Server
```

## Convenciones — IMPORTANTES, no las rompas

1. **Una clase por archivo.** Cada `class`, `record`, `enum`, `interface` en su propio `.cs`.
2. **Records inmutables** para todos los DTOs.
3. **Naming consistente:**
   - `*Command` + `*CommandHandler` para escrituras
   - `*Query` + `*QueryHandler` para lecturas
   - `*Repository` + `I*Repository`
   - `*Service` + `I*Service`
   - `*Dto`, `Create*Dto`, `Update*Dto`
4. **`async`/`await`** en todos los accesos a BD. Nunca `.Result` ni `.Wait()`.
5. **Validar autorización en handlers**, no en controllers. Inyecta `IResourceAuthorizationService` y llama al `EnsureCan*Async` correspondiente ANTES de delegar al servicio.
6. **Migraciones EF**: cada cambio de modelo va con su migración. Comprueba que `dotnet ef migrations add` no genera warnings inesperados.
7. **Sin secretos en `appsettings.json`.** Usa `dotnet user-secrets` en dev y variables de entorno en producción.

## Roles del sistema (auth)

| Rol | Quién |
|-----|-------|
| `Admin` | Super-usuario del sistema (`MRC.Agendia.Domain.Constants.Roles.Admin`) |
| `BusinessOwner` | Dueño de un negocio |
| `Employee` | Trabajador de un negocio |
| `Client` | Cliente final |

Reglas de permisos en la matriz documentada en el PR #35.

## Comandos típicos

```bash
# Build de toda la solución
dotnet build

# Tests (cuando los haya)
dotnet test

# Crear nueva migración
dotnet ef migrations add NombreMigracion --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api --output-dir Migrations

# Aplicar migraciones a BD local
dotnet ef database update --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Quitar la última migración (antes de pushear)
dotnet ef migrations remove --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Configurar secretos (en dev)
cd src/MRC.Agendia.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
dotnet user-secrets set "AdminSeed:Email" "admin@agendia.local"
dotnet user-secrets set "AdminSeed:Password" "TuPasswordFuerte123!"
```

## Workflow recomendado al implementar una issue

1. **Cambiar a `master` y pullear:** `git checkout master && git pull origin master`
2. **Crear rama** con el número de la issue: `git checkout -b <num>-<slug-corto>`. Ejemplos: `42-rate-limiting`, `48-pagination`.
3. **Implementar** siguiendo las convenciones de arriba.
4. **Build limpio:** `dotnet build` SIN warnings ni errores.
5. **Commit** con mensaje en español, claro, sin tildes problemáticas. Formato:
   ```
   <tipo>: <descripción corta>

   Closes #<num>

   <detalle>
   ```
   Tipos: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`.
6. **Push** + abrir PR con `gh pr create --base master`.
7. **NO mergear automáticamente.** Esperar a review humana.

## Cosas que NO debes hacer sin permiso explícito

- ❌ **Mergear PRs a `master`.** Solo el humano hace eso (hay branch protection).
- ❌ **`git push --force`** o `--force-with-lease`. Bloqueado en `.claude/settings.json`.
- ❌ **`git reset --hard`** ni `git clean -f`. Pierde trabajo del usuario.
- ❌ **`dotnet ef database drop`.** Borra la BD del usuario.
- ❌ **Cambiar la arquitectura sin discutirlo primero** (mover capas, cambiar de MediatR a otra cosa, etc.).
- ❌ **Borrar entidades del dominio** sin confirmar — pueden tener datos en producción.
- ❌ **Modificar `appsettings.json` añadiendo valores reales de producción.** Solo placeholders.
- ❌ **Crear ramas largas con muchas issues mezcladas.** Una rama = una issue (o un grupo cohesivo pequeño).

## Decisiones de diseño tomadas (no las cuestiones sin razón fuerte)

- **No usamos `Microsoft.AspNetCore.Identity.SignInManager`** porque vive en el shared framework de ASP.NET. La validación de password usa `UserManager.CheckPasswordAsync` directamente.
- **`ApplicationUser` vive en Infrastructure**, no en Domain. Domain solo tiene `string UserId` en las entidades (Business.OwnerUserId, Employee.UserId, Client.UserId).
- **Authorization basada en recursos** se hace en handlers vía `IResourceAuthorizationService`, no con `IAuthorizationHandler<TRequirement>` de ASP.NET.
- **Refresh token con rotación**: cada uso del refresh genera uno nuevo y revoca el anterior.
- **One Business → many ScheduleTemplates** que no se solapan en fechas. La elección del horario efectivo es por fecha (no por flag `IsDefault` — el flag existe por razones históricas, no afecta resolución).

## Estado del backlog

El backlog vive en GitHub Issues. Hay un script en `scripts/create-github-issues.sh` que lo regenera si hace falta.

Issues abiertas priorizadas con labels `priority/critical`, `priority/high`, `priority/medium`, `priority/low`.

## Si te quedas sin contexto / dudas

- Para entender el sistema de horarios, lee `src/MRC.Agendia.Infrastructure/Services/ScheduleResolver.cs` — es el componente más complejo.
- Para entender la auth resource-based, lee `src/MRC.Agendia.Infrastructure/Authorization/ResourceAuthorizationService.cs`.
- Si una decisión de diseño no es obvia, **pregúntale al usuario antes de codificar**. Tu tiempo no es gratis, pero el suyo tampoco.
