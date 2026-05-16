# Contribuir a MRC.Agendia

Guía de cómo trabajar en este proyecto (tanto humanos como agentes).

## Setup inicial

```bash
git clone https://github.com/MarcosRabadan/Agendia.git
cd Agendia

# Configurar user-secrets (ver README.md sección "Configuración de secretos")
cd src/MRC.Agendia.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
dotnet user-secrets set "AdminSeed:Email" "admin@agendia.local"
dotnet user-secrets set "AdminSeed:Password" "Admin123!"
dotnet user-secrets set "AdminSeed:FullName" "Administrador"

cd ../..

# Aplicar migraciones
dotnet ef database update --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api

# Build
dotnet build
```

## Flujo de trabajo

### 1. Una issue → una rama → un PR

- **NO trabajes directo en `master`** (hay branch protection).
- **NO mezcles issues en una rama**. Cada PR debe poder revertirse de forma independiente.

### 2. Nombrar la rama

Formato: `<numero-issue>-<slug-corto>`.

Ejemplos:
- ✅ `42-rate-limiting`
- ✅ `48-pagination-clients`
- ✅ `tooling-and-claude-setup` (cuando no hay issue concreta asociada)
- ❌ `feature-auth-improvements` (sin número de issue)
- ❌ `marcos-trabajo` (no descriptivo)

### 3. Implementar

Lee `CLAUDE.md` antes. Convenciones críticas:

- **One-class-per-file**: cada `class`, `record`, `enum`, `interface` en su propio `.cs`.
- **`async`/`await`** en todo lo que toque BD o I/O.
- **Records** para DTOs.
- **Validación de auth en handlers**, no en controllers.
- **Sin secretos en `appsettings.json`** — usa user-secrets o env vars.

### 4. Verificar localmente antes de pushear

```bash
dotnet build           # 0 errores, 0 warnings
dotnet test            # todos los tests pasan
```

Si tocaste el modelo:

```bash
dotnet ef migrations add NombreMigracion --project src/MRC.Agendia.Infrastructure --startup-project src/MRC.Agendia.Api --output-dir Migrations
```

Comprueba el `.cs` generado de la migración antes de commitearlo.

### 5. Commit

Formato:

```
<tipo>: <descripcion corta en imperativo>

Closes #<num>

<detalle del cambio>
<por que se hace asi>
```

Tipos: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`.

Sin tildes (algunos terminales no las muestran bien). En español.

### 6. Pull Request

```bash
git push -u origin <rama>
gh pr create --base master --head <rama>
```

El body del PR debe incluir:

- **Contexto** (resumen del problema)
- **Cambios** (qué se hizo)
- **Verificación** (build, tests, migración)
- **Test plan** (checkboxes de cosas a probar manualmente)

### 7. Review

- El CI corre solo en cada push al PR.
- El revisor humano comprueba el código en GitHub.
- Si hay cambios solicitados, se commitean al mismo PR (no nueva rama).
- Cuando esté aprobado, el dueño del repo hace **squash merge** a master.

### 8. Limpiar

Después del merge:

```bash
git checkout master
git pull
git branch -d <rama>  # local
```

(La rama remota se borra automáticamente desde GitHub.)

## Migraciones EF Core

- **No hacer rebase/squash de commits que contengan migraciones** entre el push y el merge. Cambiar el timestamp confunde a EF.
- Si vas a deshacer una migración antes de pushear: `dotnet ef migrations remove`.
- **NUNCA** edites una migración ya aplicada en producción. Crea una nueva que corrija.

## Lo que NO debes hacer

- ❌ `git push --force` (bloqueado en settings)
- ❌ Mergear PRs sin review humana
- ❌ Borrar entidades del dominio sin confirmar
- ❌ Cambiar la arquitectura sin discutirla
- ❌ Subir secretos al repo

## Para agentes (Claude Code)

Lee `CLAUDE.md` al inicio de cada sesión. Está pensado para que arranques en frío con el contexto del proyecto. Si te falta info en él, propón añadirla.
