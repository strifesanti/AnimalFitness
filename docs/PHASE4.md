# Fase 4 — Seguimiento de progreso

**Estado:** completada (2026-11-13).

## Objetivo

Que alumnos, entrenadores y administradores registren y consulten la evolución física: mediciones corporales, fotos (por URL), valoraciones, objetivos y gráficos de tendencia.

## Entregables

| Entregable | Estado |
|---|---|
| Dominio: `BodyMeasurement` (con `ComputeBmi()`), `ProgressPhoto`, `FitnessAssessment` | Hecho |
| Permisos `ProgressRead` / `ProgressManage` (Owner, Trainer, Athlete) | Hecho |
| Migración `Phase4Progress` | Hecho |
| Aplicación: `IProgressService` + DTOs (`BodyMeasurementDto` incluye `bmi`) | Hecho |
| Infraestructura: `ProgressService` | Hecho |
| API `/api/v1/t/{slug}/progress/...` | Hecho |
| Objetivos reutilizando `AthleteGoal` (crear, completar, cancelar) | Hecho |
| Frontend `/t/{slug}/progress` (mediciones, gráficos SVG, fotos, objetivos, valoraciones) | Hecho |
| Navegación: «Mi progreso» (alumno) y «Progreso» (Owner/Trainer) | Hecho |
| Pruebas unitarias (`Tracking/`) e integración (`Progress/`) | Hecho |

## Reglas de acceso

- **Alumno:** solo su propio perfil. Si pasa otro `athleteProfileId` recibe 403; si no tiene perfil, las listas salen vacías y, al registrar, se crea su perfil.
- **Owner / Trainer:** deben indicar `athleteProfileId` (400 si falta, 404 si no existe en la organización).
- Los registros por `id` (PUT, complete/cancel) devuelven 404 si el alumno intenta tocar los de otro.

## Decisiones

- **IMC:** no se persiste; se calcula con el peso y la estatura de cada medición. Las mediciones sin ambos valores no aportan punto al IMC.
- **Gráficos:** el endpoint filtra por `from`/`to` y ordena en memoria (SQLite no ordena `DateTimeOffset`). El frontend dibuja con SVG, sin librerías.
- **Fotos:** solo URL; la subida de binarios queda para una fase posterior.
- **Borrado:** no se expone en esta fase (el dominio soporta soft-delete).

## Verificación

```powershell
dotnet test backend/AnimalFitness.sln --filter "Category!=SqlServer"
cd frontend; pnpm build
```

## Cómo probar

1. Aplica la migración (`dotnet ef database update`) y reinicia API y frontend.
2. Alumno: `/t/academia-norte/progress` → registra una medición con peso y estatura y revisa las gráficas.
3. Owner/Trainer: elige un alumno en el selector y crea objetivos o valoraciones.

## Siguiente

**Fase 5 — Avanzadas:** nutrición, asistencia, retos, rankings, chat y notificaciones.