# Fase 1 — Cierre

**Estado:** completada (2026-11-13).

## Objetivo

Dejar un sistema base comercializable: autenticación, roles, multi-tenant, API y frontend ejecutables, con Docker Compose para desarrollo local.

## Entregables

| Entregable | Estado |
|---|---|
| Estructura del repositorio | Hecho |
| Backend .NET 10 (API, Application, Domain, Infrastructure, Tests) | Hecho |
| Frontend Next.js (login + dashboards por rol) | Hecho |
| SQL Server + EF Core + migración `InitialCreate` | Hecho |
| Autenticación cookies + CSRF + rate limiting | Hecho |
| Roles globales (`SuperAdmin`) y de organización (Owner/Trainer/Athlete) | Hecho |
| Aislamiento multi-tenant (filtros, interceptor, middleware) | Hecho |
| Pantalla de login | Hecho |
| Dashboard inicial por rol | Hecho |
| Docker Compose (sqlserver + api + frontend) | Hecho |
| README e instrucciones de instalación | Hecho |
| Datos demo solo en Development | Hecho |
| SuperAdmin vía secretos/configuración (sin credenciales hardcodeadas) | Hecho |

## Verificación ejecutada

```powershell
dotnet test backend/AnimalFitness.sln --filter "Category!=SqlServer"
cd frontend; pnpm build
```

- Backend: 36 unitarias + 32 de integración (sin Docker) en verde.
- Frontend: `pnpm build` correcto.
- Artefactos Docker: `docker-compose.yml`, Dockerfiles API/frontend, `.env.example` y workflow CI listos.
- `docker compose up --build`: en este entorno el motor Docker Desktop responde (`Server Version 29.9.3`) pero `docker pull` falla con HTTP 500 en el pipe `dockerDesktopLinuxEngine` (fallo del host, no del Compose). Cuando el pull funcione: `copy .env.example .env` y `docker compose up --build`.
- Prueba `Category=SqlServer` (Testcontainers): mismo requisito de Docker operativo.

## Cómo levantar

### Opción A — Docker Compose

```powershell
copy .env.example .env
docker compose up --build
```

- Web: http://localhost:3000  
- API: http://localhost:5080/health  
- SQL: localhost:1433  

### Opción B — Local

```powershell
# API
dotnet user-secrets set "ConnectionStrings:AnimalFitness" "..." --project backend/src/AnimalFitness.API
dotnet run --project backend/src/AnimalFitness.API --launch-profile http

# Frontend
cd frontend
pnpm dev
```

## Usuarios demo (Development)

| Correo | Contraseña | Destino |
|---|---|---|
| `superadmin@animalfitness.local` | valor de `Seed__SuperAdmin__Password` | `/platform` |
| `owner@demo.animalfitness.test` | `OwnerDemo1!` | `/t/academia-norte` |
| `trainer@demo.animalfitness.test` | `TrainerDemo1!` | `/t/academia-norte` |
| `athlete@demo.animalfitness.test` | `AthleteDemo1!` | `/t/academia-norte` |

## No incluido (Fases 2–6)

Gestión completa de organizaciones/alumnos, entrenamientos, seguimiento, nutrición, asistencia, retos, chat, pagos y reportes comerciales.

## Siguiente

**Fase 2 — Administración:** organizaciones, entrenadores, alumnos, solicitudes, invitaciones y perfiles.
