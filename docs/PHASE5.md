# Fase 5 — Funcionalidades avanzadas

**Estado:** completada (2026-11-13).

## Objetivo

Sumar nutrición, asistencia, retos con clasificación, chat en tiempo real y notificaciones in-app, siempre acotados a la organización activa.

## Entregables

| Entregable | Estado |
|---|---|
| Dominio: `Nutrition*`, `FoodEntry`, `WaterLog`, `AttendanceRecord`, `Challenge*`, `Achievement`, `Conversation*`, `ChatMessage`, `AppNotification` | Hecho |
| Configuraciones EF, filtros de organización y migración `Phase5Advanced` | Hecho |
| Aplicación: `INutritionService`, `IAttendanceService`, `IChallengeService`, `IChatService`, `INotificationService` + DTOs | Hecho |
| Infraestructura: servicios en `Infrastructure/Advanced` y `AthleteResolver` compartido | Hecho |
| API `/api/v1/t/{slug}/nutrition`, `/attendance`, `/challenges`, `/achievements`, `/chat`, `/notifications` | Hecho |
| SignalR: `ChatHub` en `/hubs/chat` + difusión desde la API | Hecho |
| Frontend: `/t/{slug}/nutrition`, `/attendance`, `/challenges`, `/chat`, `/notifications` y campana en el encabezado | Hecho |
| Pruebas unitarias (`Advanced/`) e integración (`Advanced/Phase5FlowTests`) | Hecho |

## Reglas de acceso

| Módulo | Alumno | Trainer / Owner |
|---|---|---|
| Nutrición | Lee y gestiona **lo suyo** (objetivos, comidas, agua). Otro `athleteProfileId` → 403. | Indican `athleteProfileId` (400 si falta). |
| Asistencia | Solo **lee** la suya y sus estadísticas (no tiene `AttendanceManage`). | Registran (upsert) y consultan toda la organización; sin `athleteProfileId` ven a todos. |
| Retos | Ve los retos no borrador, se une y ve la clasificación y sus logros. | Crean, activan, finalizan, cancelan, suman puntos y otorgan logros. |
| Chat | Solo conversaciones en las que participa. | Igual: nadie lee conversaciones ajenas (ni el Owner). |
| Notificaciones | Solo las propias. | Solo las propias. |

## Decisiones

- **Alumno objetivo:** `AthleteResolver` centraliza la regla (propio perfil vs. `athleteProfileId`) para nutrición, asistencia y retos, igual que en Progreso.
- **Fechas:** los días (`DateOnly`) se comparan en UTC cuando no se indica fecha. Los instantes (`DateTimeOffset`) se ordenan en memoria por la limitación de SQLite.
- **Resumen nutricional:** objetivo vigente = el de `EffectiveFrom` más reciente que no supere el día consultado. Devuelve consumido, restante (nunca negativo) y porcentajes (puede superar 100).
- **Asistencia:** un registro por alumno y día (índice único); `POST` y `PUT /attendance` hacen upsert. `presentPercent` cuenta solo *Presente*; `attendedPercent` suma también *Tarde*.
- **Retos:** solo se puede unir uno a un reto *Activo* (409 si ya participa). Al **finalizar**, quien lidera la clasificación (con puntos > 0; los empates comparten) recibe un logro «Ganador: …» y una notificación. La clasificación ordena por puntos desc., desempata por inscripción y los empatados comparten posición.
- **Chat:** conversación *Directa* = exactamente 2 personas (si ya existe se reutiliza); *Grupo* = título + 1 a 50 personas. Todos deben ser miembros activos. Los adjuntos son solo URL `http(s)`. Cada mensaje nuevo crea como máximo una notificación sin leer por conversación y destinatario.
- **Notificaciones:** `INotificationService.CreateAsync` es el auxiliar para otros servicios (no comprueba permisos, guarda los cambios pendientes). `linkPath` es relativo a la organización (`/chat?c=…`, `/challenges`); el frontend antepone `/t/{slug}`.
- **Borrado:** objetivos, comidas y registros de agua se pueden quitar (borrado lógico vía interceptor).

## SignalR

- Hub `ChatHub` en `/hubs/chat`, `[Authorize]` con las **mismas cookies de Identity** (el navegador las envía en la negociación y en el WebSocket).
- `/hubs` está en `SkipTenantPrefixes` del middleware de organización: la conexión no fija tenant (sí se registra el usuario). Cada método recibe el `slug`, valida la membresía con `ITenantAccessService`, establece el contexto y comprueba que la persona participa en la conversación antes de tocar grupos o datos.
- Grupos: `tenant:{tenantId}:conversation:{conversationId}` (la organización forma parte del nombre).
- Métodos: `JoinConversation(slug, conversationId)`, `LeaveConversation(slug, conversationId)` y `SendMessage(slug, conversationId, body, attachmentUrl)`.
- Evento recibido: `messageReceived` con un `ChatMessageDto`. `POST /chat/conversations/{id}/messages` guarda y también difunde por `IHubContext`, de modo que API y hub se comportan igual.
- Frontend: `@microsoft/signalr` con reconexión automática; las conversaciones visibles se vuelven a unir tras reconectar. Si no hay conexión en vivo, la pantalla sondea cada 8 s. `next.config.ts` reescribe `/hubs/*` hacia la API; si el proxy de Next.js no admite WebSocket, SignalR cae a SSE/long polling. Para conectar directo a la API usa `NEXT_PUBLIC_HUB_ORIGIN` (requiere el mismo sitio para que la cookie `SameSite=Lax` se envíe).

## Límites conocidos

- Los mensajes y no leídos de una conversación se cargan y ordenan en memoria (SQLite no ordena `DateTimeOffset`). Con mucho histórico habrá que paginar por cursor y mover el orden a SQL Server.
- Quien deja una conversación conserva su suscripción de grupo hasta desconectarse (aún no hay endpoint para abandonar conversaciones).
- Sin subida de archivos al chat (solo URL) y sin presencia («escribiendo…»).
- Los retos con métrica `WorkoutsCompleted` / `AttendanceDays` aún no puntúan solos: los puntos se registran a mano.

## Verificación

```powershell
dotnet test backend/AnimalFitness.sln --filter "FullyQualifiedName!~SqlServerMigrationTests"
cd frontend; pnpm build
```

## Cómo probar

1. Aplica la migración (`dotnet ef database update`) y reinicia API y frontend.
2. Alumno: `/t/academia-norte/nutrition` → define objetivos, registra comidas y agua.
3. Owner/Trainer: `/attendance` → marca asistencia; `/challenges` → crea, activa y suma puntos.
4. Chat: abre `/chat` con dos sesiones (dos navegadores) y comprueba los mensajes en vivo.

## Siguiente

**Fase 6 — Comercialización:** planes SaaS, membresías, pagos, facturación y reportes.
