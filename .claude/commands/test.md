---
description: Añade tests al código modificado o a un archivo concreto
---

# /test $ARGUMENTS

Añade tests al código indicado en `$ARGUMENTS` (ruta de fichero o nombre de clase). Si no se indica nada, añade tests al cambio más reciente en `git diff`.

## Reglas

1. **Framework:** xUnit (es el estándar de .NET para tests).
2. **Mocking:** Moq o NSubstitute (revisa qué hay ya en `tests/MRC.Agendia.API.Tests` y mantén consistencia).
3. **Cobertura mínima por clase pública:**
   - Happy path
   - Validaciones que lanzan excepción
   - Casos edge documentados
4. **Naming:** `MethodName_StateUnderTest_ExpectedBehavior` o el patrón que ya use el proyecto.
5. **AAA pattern:** Arrange, Act, Assert.

## Para tests de Handlers

Mockear `IService`, `IResourceAuthorizationService` y comprobar:
- Que se llama al auth check con los args correctos
- Que delega correctamente al servicio
- Que se propaga el `CancellationToken`

## Para tests del ScheduleResolver (caso crítico)

Casos obligatorios:
- Sin plantilla vigente
- Plantilla vigente cubriendo el día
- Plantilla vigente con día no laborable
- Override `Closed`
- Override `NationalHoliday`
- Override `LocalHoliday`
- Override `CustomHours` (devuelve CustomSlots ordenados)
- Override prevalece sobre plantilla
- Turno partido en plantilla → 2 slots ordenados

## Verificación final

- `dotnet test` pasa todos los tests
- Los tests nuevos cubren el código objetivo

No abras PR — déjalo en working copy para que el usuario decida cuándo commitear (probablemente junto con el código que están testeando).
