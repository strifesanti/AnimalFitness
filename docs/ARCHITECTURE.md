# Arquitectura de AnimalFitness

> Estado: Fase 5 completada. Se actualiza al cerrar cada fase.

## Principios

- **Monolito modular.** Sin microservicios en la fase inicial. Cada módulo funcional tiene sus propios DTOs, servicios y endpoints, y se comunica con otros módulos a través de interfaces.
- **Multi-tenant con base de datos compartida.** Cada entidad de organización lleva `TenantId`. Ver [DATABASE.md](DATABASE.md).
- **El backend es la fuente de verdad.** Nunca se confía en permisos, roles ni `TenantId` enviados por el cliente.
- **Datos reales o provisionales identificados.** Las integraciones externas (correo, pagos, almacenamiento) se implementan detrás de una interfaz. Si la implementación es provisional, se marca de forma explícita.

## Capas del backend

| Proyecto | Responsabilidad |
|---|---|
| `AnimalFitness.API` | Endpoints, autenticación, middleware, rate limiting, SignalR |
| `AnimalFitness.Application` | Casos de uso por funcionalidad, DTOs, validaciones, autorización de negocio |
| `AnimalFitness.Domain` | Entidades, enums, reglas de negocio e interfaces base |
| `AnimalFitness.Infrastructure` | EF Core, Identity, filtros multi-tenant, migraciones, seeders, almacenamiento |

Dependencias permitidas: `API → Application → Domain` e `Infrastructure → Application/Domain`. `Domain` no depende de ningún otro proyecto.

## Tiempo real (SignalR)

- `ChatHub` (`/hubs/chat`) comparte las cookies de Identity con la API. No resuelve organización al conectar; cada método recibe el `slug`, valida la membresía y la participación en la conversación.
- Los mensajes se persisten siempre en `IChatService`; la API (y el hub) difunden después a los grupos `tenant:{tenantId}:conversation:{id}` con `ChatBroadcaster`. Los servicios de `Infrastructure` no conocen SignalR.
- Servicios de la Fase 5 (`Infrastructure/Advanced`) comparten `AthleteResolver` para decidir sobre qué alumno actúa cada petición. Detalle en [PHASE5.md](PHASE5.md).

## Frontend

- Next.js con App Router y rutas agrupadas por contexto: `(auth)`, `(platform)` para SuperAdmin y `(tenant)` para academias.
- La navegación se adapta al rol. El middleware de Next.js solo mejora la experiencia; la seguridad real está en la API.
- Las llamadas a la API pasan por un cliente HTTP tipado que envía la cookie de sesión y el token CSRF.

## Hoja de ruta por fases

| Fase | Contenido | Estado |
|---|---|---|
| 1 | Fundamentos: estructura, autenticación, multi-tenant, roles, migraciones, Docker | **Completada.** Ver [PHASE1.md](PHASE1.md). |
| 2 | Administración: organizaciones, entrenadores, alumnos, solicitudes, invitaciones | **Completada.** Ver [PHASE2.md](PHASE2.md). |
| 3 | Entrenamiento: biblioteca de ejercicios, rutinas, sesiones | **Completada.** Ver [PHASE3.md](PHASE3.md). |
| 4 | Seguimiento: peso, medidas, fotografías, gráficos, objetivos | **Completada.** Ver [PHASE4.md](PHASE4.md). |
| 5 | Avanzadas: nutrición, asistencia, retos, rankings, chat, notificaciones | **Completada.** Ver [PHASE5.md](PHASE5.md). |
| 6 | Comercialización: planes SaaS, membresías, pagos, facturación, reportes | Pendiente |
