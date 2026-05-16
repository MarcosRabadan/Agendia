---
description: Implementa una issue de GitHub de principio a fin (rama, código, build, push, PR)
---

# /issue $ARGUMENTS

Implementa la issue de GitHub indicada (número o `#número`). Sigue este flujo de manera autónoma:

## 1. Contexto

- Lee la issue completa con `gh issue view $ARGUMENTS --repo MarcosRabadan/Agendia` y extrae:
  - Título
  - Body con criterios de aceptación
  - Labels (sobre todo `priority/*` y `area/*`)

## 2. Planificación

- Identifica qué archivos hay que tocar.
- Si hay decisiones de diseño no obvias, **pregunta al usuario** antes de seguir.
- Si la issue es muy grande (>500 líneas estimadas) propón partirla en varias ramas y pide confirmación.

## 3. Setup de rama

- `git checkout master`
- `git pull origin master`
- `git checkout -b <num>-<slug>` (usa palabras del título, en minúscula, separadas por guión, máx 5)

## 4. Implementar

Sigue todas las convenciones de `CLAUDE.md` (one-class-per-file, naming, async/await, validación auth en handlers, etc.).

## 5. Verificar

- `dotnet build` debe terminar con **0 errores, 0 warnings**.
- Si tocas el modelo de datos, crea la migración correspondiente y verifica que `dotnet ef migrations add` tampoco genera warnings.
- Si hay tests para el área afectada, córrelos.

## 6. Commit

Mensaje en español sin tildes, formato:

```
<tipo>: <descripcion corta>

Closes #<num>

<detalle del cambio>

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
```

Tipos válidos: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`.

## 7. Push y PR

- `git push -u origin <rama>`
- `gh pr create --base master --head <rama> --title "..." --body "..."`
- Body del PR debe incluir: contexto, cambios, verificación, test plan.

## 8. NO mergear

Espera siempre a review humana. Master tiene branch protection.

## 9. Reportar

Al final, muestra el link del PR y un resumen ejecutivo (3-5 bullets) de qué se hizo.
