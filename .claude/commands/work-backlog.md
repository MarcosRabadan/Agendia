---
description: Implementa varias issues en cadena (PRs separados, sin mergear)
---

# /work-backlog $ARGUMENTS

Implementa varias issues del backlog en secuencia, una rama y PR por cada una.

Argumentos: lista de números de issue separados por espacios. Ejemplo: `/work-backlog 42 43 49`.

## Flujo

Para cada número en `$ARGUMENTS`:

1. Aplica el mismo flujo que `/issue <num>`.
2. Después de hacer push y PR de una issue, **vuelve a master** (`git checkout master && git pull`) antes de empezar la siguiente.
3. Cada PR es independiente. No mergees automáticamente.

## Reglas

- **Si una issue falla** (build no compila, requiere decisión humana), para todo el lote y reporta al usuario.
- **No mezcles issues en la misma rama** aunque parezcan relacionadas.
- **Después de cada PR**, muestra el link.
- **Al final del lote**, muestra un resumen con los N PRs abiertos.

## Tope de seguridad

- Máximo 5 issues en un lote. Si te piden más, divide en dos sesiones.
- Si el contexto de la sesión empieza a estar muy lleno, para tras la siguiente issue y pide al usuario que abra una sesión nueva.
