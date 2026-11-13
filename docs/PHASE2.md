# Fase 2 — Administración

**Estado:** completada (2026-11-13).

## Objetivo

Gestión real de organizaciones, miembros, invitaciones, solicitudes de ingreso y perfiles (alumno/entrenador), sin botones simulados.

## Entregables

| Entregable | Estado |
|---|---|
| Permisos centralizados (`Permissions.*` + mapa por rol) | Hecho |
| SuperAdmin: listar/crear/activar/suspender organizaciones | Hecho |
| Owner: configuración de academia | Hecho |
| Miembros: listar, cambiar rol, suspender, reactivar, retirar | Hecho |
| Invitaciones (token hasheado, correo provisional, aceptar) | Hecho |
| Solicitudes de ingreso (enviar/aprobar/rechazar/cancelar) | Hecho |
| Perfiles `AthleteProfiles`, `TrainerProfiles`, `TrainerAthletes`, `AthleteGoals` | Hecho |
| FKs compuestas `(TenantId, Id)` en asignaciones y objetivos | Hecho |
| Migración `Phase2Administration` | Hecho |
| Frontend: organizaciones, entrenadores, alumnos, solicitudes, settings, perfil, aceptar invitación | Hecho |

## Verificación

```powershell
dotnet test backend/AnimalFitness.sln --filter "Category!=SqlServer"
cd frontend; pnpm build
```

## Cómo probar en local

1. API + frontend en marcha (como en Fase 1).
2. Owner: `/t/academia-norte` → Entrenadores, Alumnos, Solicitudes, Configuración.
3. SuperAdmin: `/platform/organizations`.
4. Invitación: crear en Solicitudes → token en log de la API → `/invitations/accept`.
5. Solicitud: usuario registrado sin membresía → `POST /api/v1/organizations/{slug}/join-requests` (o flujo UI futuro de directorio).

## Siguiente

**Fase 3 — Entrenamiento:** biblioteca de ejercicios, rutinas y sesiones.
