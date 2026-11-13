# Fase 3 — Entrenamiento

**Estado:** completada (2026-11-13).

## Objetivo

Biblioteca de ejercicios, planes/rutinas versionados, asignación a alumnos y sesiones con registro de series — sin sobrescribir el historial al editar un plan publicado.

## Entregables

| Entregable | Estado |
|---|---|
| Dominio: categorías, ejercicios, medios, planes, días, prescripciones | Hecho |
| Asignaciones plan→alumno | Hecho |
| Sesiones + `ExerciseLogs` | Hecho |
| Versionado (`new-version`) al editar publicados | Hecho |
| Permisos Exercises/Workouts/Sessions | Hecho |
| Migración `Phase3Training` | Hecho |
| API `/api/v1/t/{slug}/exercises|workouts|sessions` | Hecho |
| Frontend: biblioteca, rutinas, detalle, sesiones | Hecho |
| Seed demo (categoría, 3 ejercicios, plan publicado) | Hecho |

## Verificación

```powershell
dotnet test backend/AnimalFitness.sln --filter "Category!=SqlServer"
cd frontend; pnpm build
```

## Cómo probar

1. Reinicia API y frontend (BD nueva o migración aplicada).
2. Owner/Trainer: `/t/academia-norte/exercises` y `/workouts`.
3. Publica un plan, asígnalo al alumno demo, inicia sesión y registra series.
4. Al editar un plan publicado: **Nueva versión** (el historial de sesiones sigue apuntando a la versión anterior).

## Siguiente

**Fase 4 — Seguimiento:** peso, medidas, fotografías, gráficos y objetivos de progreso.
