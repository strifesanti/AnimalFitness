# AnimalFitness

Plataforma SaaS multi-tenant para la gestión de entrenadores personales, gimnasios, academias deportivas y alumnos.

> **Estado:** Fase 5 (Nutrición, asistencia, retos, chat y notificaciones) **completada**. Ver [docs/PHASE5.md](docs/PHASE5.md).

## Stack

| Capa | Tecnologías |
|---|---|
| Frontend | Next.js 16 (App Router), React, TypeScript, Tailwind CSS 4, React Hook Form, Zod, Lucide |
| Backend | ASP.NET Core Web API (.NET 10), EF Core, Identity (cookies), SignalR (chat en tiempo real, `/hubs/chat`) |
| Base de datos | Microsoft SQL Server 2022 |
| Infraestructura | Docker Compose |

## Estructura

```
AnimalFitness/
├── frontend/          Next.js
├── backend/           Solución .NET (monolito modular)
├── database/scripts/  SQL idempotente de migraciones
├── docs/
├── docker-compose.yml
└── README.md
```

## Requisitos

- .NET 10 SDK (`global.json` fija 10.0.404)
- Node.js LTS + pnpm
- Docker Desktop (para Compose y la prueba SQL Server)

## Arranque rápido con Docker

```powershell
copy .env.example .env
docker compose up --build
```

| Servicio | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API salud | http://localhost:5080/health |
| SQL Server | `localhost,1433` (usuario `sa`) |

Si Docker no arranca por virtualización/WSL2 desactivados, usa el arranque local siguiente y activa la virtualización cuando puedas.

## Arranque local (sin Compose)

### 1. API

```powershell
cd backend/src/AnimalFitness.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AnimalFitness" "Server=localhost,1433;Database=AnimalFitness;User Id=sa;Password=AnimalFitness_Dev1!;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set "Seed:SuperAdmin:Email" "superadmin@animalfitness.local"
dotnet user-secrets set "Seed:SuperAdmin:Password" "SuperAdmin_Dev1!"
dotnet run --launch-profile http
```

### 2. Frontend

```powershell
cd frontend
pnpm install
pnpm dev
```

Abre http://localhost:3000/login. Las rutas `/api/*` se reenvían a `http://localhost:5080`.

## Usuarios de desarrollo

Solo se crean con `ASPNETCORE_ENVIRONMENT=Development` y `Seed:Enabled=true`.

| Correo | Contraseña | Rol |
|---|---|---|
| `superadmin@animalfitness.local` | `SuperAdmin_Dev1!` (o tu secreto) | SuperAdmin |
| `owner@demo.animalfitness.test` | `OwnerDemo1!` | Owner |
| `trainer@demo.animalfitness.test` | `TrainerDemo1!` | Trainer |
| `athlete@demo.animalfitness.test` | `AthleteDemo1!` | Athlete |
| `entrenador02@demo…` / `entrenador03@demo…` | `TrainerDemo1!` | Trainer (histórico) |
| `alumno01@demo…` … `alumno12@demo…` | `AthleteDemo1!` | Athlete (histórico, últimos ~6 meses) |

El roster histórico (mediciones, asistencia y sesiones) se carga en `academia-norte` de forma idempotente al arrancar en Development.

## Pruebas

```powershell
dotnet tool restore
dotnet test backend/AnimalFitness.sln --filter "Category!=SqlServer"
dotnet test backend/AnimalFitness.sln --filter "Category=SqlServer"   # requiere Docker
cd frontend
pnpm build
pnpm lint
```

## Documentación

- [Cierre Fase 1](docs/PHASE1.md)
- [Cierre Fase 2](docs/PHASE2.md)
- [Cierre Fase 3](docs/PHASE3.md)
- [Cierre Fase 4](docs/PHASE4.md)
- [Cierre Fase 5](docs/PHASE5.md)
- [Arquitectura](docs/ARCHITECTURE.md)
- [Base de datos](docs/DATABASE.md)
- [API](docs/API.md)
- [Seguridad](docs/SECURITY.md)
- [Frontend](frontend/README.md)

## Secretos

Nada sensible va al repositorio. Usa `.env` (desde `.env.example`) o *user-secrets*.

## Licencia

_Pendiente de definir._
