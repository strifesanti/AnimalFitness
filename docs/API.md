# API

> Estado: Fase 5 completada (nutrición, asistencia, retos, chat y notificaciones). OpenAPI en Development.

## Convenciones

- **Base:** `/api/v1/...`
- **Estilo:** Minimal APIs agrupadas por módulo.
- **Formato:** JSON en `camelCase`. Los DTOs son independientes de las entidades.
- **Errores:** `ProblemDetails` (RFC 9457). Sin stack traces fuera de Development.
- **Autenticación:** cookie HttpOnly Identity (`__Host-AnimalFitness.Auth` fuera de Testing).
- **CSRF:** primero `GET /api/v1/auth/csrf`; luego envía el token en `X-XSRF-TOKEN` en `POST`/`PUT`/`PATCH`/`DELETE` de autenticación.
- **Tenant:** prioridad ruta (`/t/{slug}` o `/api/v1/t/{slug}`) → cabecera `X-Tenant` → subdominio (`TenantResolution:BaseDomain`). El valor es solo un candidato; el middleware valida membresía y estado. Tras conceder el acceso, responde con `X-Tenant-Id` y `X-Tenant-Slug`.
- **Paginación:** `page` y `pageSize`, con respuesta `{ items, page, pageSize, totalCount }`.

## Implementado

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/health` | Estado del proceso. No comprueba la base de datos todavía. |
| GET | `/api/v1/info` | Nombre y versión del servicio. |
| GET | `/openapi/v1.json` | Documento OpenAPI. Solo se publica en Development. |
| GET | `/api/v1/tenancy/context` | Contexto actual (tenant, usuario, rol). Sin candidato no fija organización. |
| GET | `/api/v1/t/{slug}/context` | Igual, resolviendo la organización por la ruta. Requiere sesión y membresía. |
| GET | `/api/v1/auth/csrf` | Emite cookie CSRF y devuelve `{ token }`. |
| POST | `/api/v1/auth/register` | Registro + sesión. Requiere CSRF. Respuesta neutra si el correo ya existe. |
| POST | `/api/v1/auth/login` | Inicio de sesión. Requiere CSRF. |
| POST | `/api/v1/auth/logout` | Cierre de sesión. Requiere CSRF. |
| GET | `/api/v1/auth/me` | Usuario, membresías y tenant activo. |
| POST | `/api/v1/auth/confirm-email` | Confirmación de correo. Requiere CSRF. |
| POST | `/api/v1/auth/forgot-password` | Recuperación. Siempre responde igual. Requiere CSRF. |
| POST | `/api/v1/auth/reset-password` | Restablecimiento. Requiere CSRF. |

Las respuestas de error usan `application/problem+json`. Los endpoints de autenticación mutables tienen rate limiting (20/min por IP).

## Administración (Fase 2)

Respuestas exitosas tipadas: `{ value, message }`. Mutaciones requieren CSRF.

| Método | Ruta | Quién |
|---|---|---|
| GET/POST | `/api/v1/platform/tenants` | SuperAdmin |
| POST | `/api/v1/platform/tenants/{id}/activate\|suspend` | SuperAdmin |
| GET | `/api/v1/organizations` | Autenticado (directorio) |
| POST | `/api/v1/organizations/{slug}/join-requests` | Autenticado sin membresía |
| POST | `/api/v1/invitations/accept` | Autenticado (mismo correo) |
| GET/PUT | `/api/v1/t/{slug}/organization` | Owner |
| PUT | `/api/v1/t/{slug}/organization/settings` | Owner |
| GET/POST… | `/api/v1/t/{slug}/members` | Owner/Trainer según permiso |
| GET/POST… | `/api/v1/t/{slug}/invitations` | Owner/Trainer |
| GET/POST… | `/api/v1/t/{slug}/join-requests` | Owner/Trainer |
| GET/PUT… | `/api/v1/t/{slug}/athletes` | Owner/Trainer/Athlete |
| GET/PUT… | `/api/v1/t/{slug}/trainers` | Owner/Trainer |
| PUT | `/api/v1/profile/me` | Autenticado |

## Entrenamiento (Fase 3)

| Prefijo | Contenido |
|---|---|
| `/api/v1/t/{slug}/exercises` | Categorías, biblioteca, medios |
| `/api/v1/t/{slug}/workouts` | Planes, días, prescripciones, publicar, nueva versión, asignar |
| `/api/v1/t/{slug}/sessions` | Iniciar/completar/cancelar y registrar series |

Los planes publicados no se editan in-place: se crea una **nueva versión** para no alterar sesiones históricas.

## Progreso (Fase 4)

Prefijo `/api/v1/t/{slug}/progress`. Alumno: solo su propio perfil. Owner/Trainer: pasan `athleteProfileId` (query en GET, cuerpo en POST). Mutaciones requieren CSRF. Permisos `Progress.Read` / `Progress.Manage`.

| Método | Ruta | Descripción |
|---|---|---|
| GET/POST | `/measurements` | Listar / registrar mediciones (`bmi` calculado en la respuesta) |
| PUT | `/measurements/{id}` | Actualizar medición |
| GET/POST | `/photos` | Listar / registrar foto (solo URL) |
| PUT | `/photos/{id}` | Actualizar foto |
| GET/POST | `/assessments` | Listar / registrar valoración |
| PUT | `/assessments/{id}` | Actualizar valoración |
| GET/POST | `/goals` | Listar / crear objetivo (`AthleteGoal`) |
| POST | `/goals/{id}/complete\|cancel` | Cerrar objetivo |
| GET | `/charts?athleteProfileId=&from=&to=` | Series `weightKg`, `bodyFatPercent`, `waistCm`, `bmi` como `{ at, value }[]` |

## Nutrición (Fase 5)

Prefijo `/api/v1/t/{slug}/nutrition`. Alumno: solo lo suyo. Owner/Trainer: `athleteProfileId` (query en GET, cuerpo en POST/PUT). `date` es `yyyy-MM-dd` (hoy UTC por defecto). Permisos `Nutrition.Read` / `Nutrition.Manage`.

| Método | Ruta | Descripción |
|---|---|---|
| GET/POST | `/targets` | Listar / crear objetivos diarios (calorías, macros, agua; vigencia `effectiveFrom`) |
| PUT/DELETE | `/targets/{id}` | Actualizar / eliminar objetivos |
| GET/POST | `/foods?date=` | Comidas del día / registrar comida (`mealType`: `Breakfast`, `Lunch`, `Dinner`, `Snack`, `Other`) |
| DELETE | `/foods/{id}` | Quitar comida |
| GET/POST | `/water?date=` | Registros de agua del día / registrar (`amountMl` 1–5000) |
| DELETE | `/water/{id}` | Quitar registro de agua |
| GET | `/summary?date=&athleteProfileId=` | `{ target, consumed, remaining, percentages, foods, water }` frente al objetivo vigente |

## Asistencia (Fase 5)

Prefijo `/api/v1/t/{slug}/attendance`. Permisos `Attendance.Read` (todos) / `Attendance.Manage` (solo Owner y Trainer). Rango máximo 366 días; por defecto, los últimos 30.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/?athleteProfileId=&from=&to=` | Registros. Alumno: los suyos. Staff sin `athleteProfileId`: toda la organización |
| POST / PUT | `/` | Upsert `{ athleteProfileId, date, status, notes }` (`Present`, `Absent`, `Excused`, `Late`); uno por alumno y día |
| GET | `/stats?athleteProfileId=&from=&to=` | `total`, conteos por estado, `presentPercent` y `attendedPercent` (presente + tarde) |

## Retos y logros (Fase 5)

Prefijo `/api/v1/t/{slug}/challenges` y `/api/v1/t/{slug}/achievements`. Permisos `Challenges.Read` (todos) / `Challenges.Manage` (Owner y Trainer). Los alumnos no ven retos en borrador.

| Método | Ruta | Descripción |
|---|---|---|
| GET/POST | `/challenges?status=` | Listar (con `participantCount` y `joined`) / crear (queda en `Draft`) |
| GET | `/challenges/{id}` | Detalle |
| POST | `/challenges/{id}/activate\|complete\|cancel` | Ciclo de vida. `complete` otorga un logro a quien lidera |
| POST | `/challenges/{id}/join` | Inscribirse (alumno: sin cuerpo; staff: `{ athleteProfileId }`). 409 si ya participa |
| GET/POST | `/challenges/{id}/activities` | Actividades / sumar puntos `{ athleteProfileId, points, note }` (solo staff, reto activo) |
| GET | `/challenges/{id}/leaderboard` | `{ rank, participantId, athleteProfileId, athleteName, points }[]` por puntos desc. |
| GET/POST | `/achievements?athleteProfileId=` | Listar logros / otorgar `{ athleteProfileId, title, description, challengeId }` (staff) |

## Chat y notificaciones (Fase 5)

Permisos `Chat.Use` y `Notifications.Read` (todos los roles). Solo se accede a las conversaciones y notificaciones propias.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/t/{slug}/chat/contacts` | Miembros activos con los que se puede conversar |
| GET/POST | `/api/v1/t/{slug}/chat/conversations` | Listar (con `unreadCount`) / crear `{ kind: Direct\|Group, title, participantUserIds }` |
| GET | `/api/v1/t/{slug}/chat/conversations/{id}/messages?limit=` | Últimos mensajes en orden cronológico (50 por defecto, máx. 200) |
| POST | `/api/v1/t/{slug}/chat/conversations/{id}/messages` | Enviar `{ body, attachmentUrl }`; guarda y difunde por SignalR |
| POST | `/api/v1/t/{slug}/chat/conversations/{id}/read` | Marcar como leída |
| GET | `/api/v1/t/{slug}/notifications?unreadOnly=&limit=` | `{ unreadCount, items }` |
| POST | `/api/v1/t/{slug}/notifications/{id}/read` | Marcar una como leída |
| POST | `/api/v1/t/{slug}/notifications/read-all` | Marcar todas como leídas |

### SignalR: `/hubs/chat`

Requiere la cookie de sesión (mismas cookies que la API). Métodos del servidor: `JoinConversation(slug, conversationId)`, `LeaveConversation(slug, conversationId)`, `SendMessage(slug, conversationId, body, attachmentUrl)`. Evento del cliente: `messageReceived(ChatMessageDto)`. Grupos: `tenant:{tenantId}:conversation:{conversationId}`.
