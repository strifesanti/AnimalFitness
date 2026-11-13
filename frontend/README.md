# AnimalFitness Frontend

Next.js (App Router) + TypeScript + Tailwind CSS 4.

## Requisitos

- Node.js LTS
- pnpm
- API en marcha (`dotnet run --project backend/src/AnimalFitness.API --launch-profile http`)

## Desarrollo

```powershell
cd frontend
pnpm install
pnpm dev
```

Abre [http://localhost:3000](http://localhost:3000).

Las peticiones `/api/*` se reescriben al backend (`API_ORIGIN`, por defecto `http://localhost:5080`).

## Pantallas de la Fase 1

- `/login` — inicio de sesión con CSRF y cookie
- `/platform` — dashboard SuperAdmin
- `/t/{slug}` — dashboard Owner / Trainer / Athlete

Los módulos futuros aparecen como “Próximamente” y no simulan funcionalidad.
