---
description: Hace una review de un Pull Request abierto
---

# /review-pr $ARGUMENTS

Revisa el PR indicado y emite una review estructurada.

## Pasos

1. `gh pr view $ARGUMENTS --repo MarcosRabadan/Agendia` — leer cabecera y body
2. `gh pr diff $ARGUMENTS --repo MarcosRabadan/Agendia` — leer el diff completo
3. `gh pr checks $ARGUMENTS --repo MarcosRabadan/Agendia` — estado del CI

## Qué evaluar

### Correctness
- ¿Cumple los criterios de la issue?
- ¿La lógica tiene bugs evidentes?
- ¿Hay casos edge sin manejar?

### Arquitectura
- ¿Sigue las convenciones de `CLAUDE.md`?
- ¿Respeta Clean Architecture (capas, no dependencias inversas)?
- ¿Cumple one-class-per-file?

### Seguridad
- ¿Hay validación de autorización en los handlers nuevos?
- ¿Se filtran datos del usuario actual donde aplica?
- ¿Hay riesgo de SQL injection, XSS, secretos expuestos?

### Tests / Verificación
- ¿Hay tests donde tocaría?
- Si no, ¿qué deberían cubrir?
- ¿El CI está verde?

### Estilo y limpieza
- Naming consistente
- Comentarios donde aportan, no de relleno
- Sin código muerto

## Output

Estructura la review así:

```
# Review PR #<num>: <titulo>

## ✅ Bien hecho
- ...

## ⚠️ Bloqueantes (deben arreglarse antes de mergear)
- ...

## 💡 Sugerencias (no bloqueantes)
- ...

## 🧪 Casos no cubiertos
- ...

## Veredicto: [APPROVE / REQUEST CHANGES / COMMENT]
```

NO publiques la review como comentario en GitHub automáticamente — déjasela en pantalla al usuario para que decida.
