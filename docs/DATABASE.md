# Base de datos

> Estado: Fase 4 completada. Migraciones hasta `Phase4Progress` (mediciones corporales, fotos de progreso y valoraciones; los objetivos reutilizan `AthleteGoals`).

## Motor

Microsoft SQL Server 2022, gestionado con Entity Framework Core Migrations.

## Modelo multi-tenant

- Base de datos compartida. Toda entidad que pertenezca a una organización lleva `TenantId`.
- Los usuarios son globales (`AspNetUsers`). Su pertenencia a cada organización vive en `TenantMemberships`, por lo que un usuario puede estar en varias academias sin duplicarse.
- Los roles de organización (Owner, Trainer, Athlete) se guardan en `TenantMemberships`, no en `AspNetUserRoles`. El único rol global de Identity es `SuperAdmin`.
- Claves foráneas compuestas `(TenantId, Id)` entre entidades del mismo tenant, para que la base de datos impida referencias cruzadas. Se aplican cuando aparezca la primera relación entre entidades de organización (Fase 2).

## Convenciones

| Tema | Convención |
|---|---|
| Claves primarias | `uniqueidentifier` (Guid secuencial) |
| Auditoría | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, llenados por interceptor |
| Borrado | Lógico (`IsDeleted`, `DeletedAt`) en entidades de negocio |
| Concurrencia | `RowVersion` |
| Fechas | `datetimeoffset` en UTC. La presentación convierte a la zona horaria del usuario |
| Dinero | `decimal(18,2)` con código de moneda (predeterminado `COP`) |
| Nombres | Singular en C#, plural en tablas |

## Entidades de la Fase 1

El modelo ya está en `AnimalFitness.Domain`. La migración y las claves foráneas llegan en el paso de infraestructura. Los valores de los enums son estables y se guardarán como `int`.

| Entidad | Ámbito | Contrato |
|---|---|---|
| `Tenant` | Global | Nombre, slug único, estado (`Trial`, `Active`, `Suspended`), contacto, zona horaria y moneda. No tiene `TenantId`. |
| `User` | Global | Correo, nombre, teléfono, actividad, confirmación de correo y último acceso. No tiene `TenantId`. La contraseña no forma parte del dominio: la añadirá el adaptador de Identity. |
| `TenantMembership` | Tenant | `TenantId` fijo, `UserId`, rol (`Owner`, `Trainer`, `Athlete`) y estado (`Active`, `Suspended`, `Removed`). |
| `TenantSettings` | Tenant | Una fila por organización: color, cultura, moneda y un objeto JSON. |
| `Invitation` | Tenant | Correo, rol, hash del token, vencimiento y estado (`Pending`, `Accepted`, `Expired`, `Cancelled`). No guarda el token en claro. |
| `JoinRequest` | Tenant | Persona, estado (`Pending`, `Approved`, `Rejected`, `Cancelled`) y motivo cuando se rechaza. |
| `ConsentRecord` | Mixto | Titular, tipo de autorización, versión del texto, alta y revocación. `TenantId` es nulo si la autorización es de la plataforma. |
| `AuditLog` | Mixto | Hecho inmutable. `TenantId` es nulo en hechos de plataforma. No implementa `ITenantEntity`. |

`SuperAdmin` vive en `PlatformRoles` y no es un valor de `MembershipRole`.

### Reglas que ya impone el dominio

- El `TenantId` se asigna al crear la entidad y no tiene setter público.
- Una misma persona puede tener membresías en varias organizaciones.
- Una membresía `Removed` no se reactiva.
- Una invitación vencida no se acepta ni cambia de estado por ese intento.
- Una solicitud de ingreso resuelta no se vuelve a decidir.
- Un consentimiento revocado se conserva.
- Los slugs `www`, `api`, `admin`, `app`, `mail`, `static` y `animalfitness` están reservados.

## Esquema físico de la Fase 1

Migración: `InitialCreate` (`backend/src/AnimalFitness.Infrastructure/Persistence/Migrations`).
Script idempotente para despliegues: `database/scripts/001_initial_idempotent.sql`.

| Tabla | Notas |
|---|---|
| `Tenants` | Slug único. Restricción CHECK sobre `Status`. |
| `UserProfiles` | Perfil de dominio (`User`). Su `Id` es también la clave de `AspNetUsers`, así que cada persona tiene una sola cuenta global. Correo único. |
| `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc. | Identity. Guardan las credenciales. |
| `TenantMemberships` | Índice único `(TenantId, UserId)` **filtrado** a membresías no retiradas y no borradas. Una persona puede volver a vincularse después de ser retirada. CHECK sobre `Role` y `Status`. |
| `OrganizationSettings` | Tabla `TenantSettings`. Índice único por `TenantId`. |
| `Invitations` | Hash del token único. CHECK sobre `Role` y `Status`. |
| `JoinRequests` | Índice único filtrado: una sola solicitud pendiente por persona y organización. CHECK sobre `Status`. |
| `ConsentRecords` | Sin borrado lógico: se revoca, no se elimina. |
| `AuditLogs` | Sin claves foráneas, para que el historial sobreviva a cualquier cambio en las tablas de negocio. |

Todas las tablas de entidades auditables tienen `RowVersion` (`rowversion` en SQL Server) para control de concurrencia.

### Por qué `UserProfiles` y no `Users`

`Users` es el nombre de la tabla de Identity (`IdentityDbContext`). El perfil de dominio usa `UserProfiles` para no mezclar ambos conceptos.

### Correo duplicado: regla de sincronización

El correo vive en `AspNetUsers` (login) y en `UserProfiles` (dominio). Cualquier cambio de correo debe actualizar ambas tablas en la misma transacción, mediante un servicio de aplicación. Ninguna otra ruta puede cambiar uno solo de los dos.

### Claves foráneas compuestas `(TenantId, Id)`

Todavía no se usan en la Fase 1, porque ninguna entidad de organización referencia a otra de la misma organización. Se añadirán cuando aparezca esa relación (por ejemplo, alumno → entrenador en la Fase 2).

### Filtros y reglas de escritura

- Filtro global en `TenantMemberships`, `OrganizationSettings`, `Invitations` y `JoinRequests`: `TenantId` igual a la organización activa **y** no borrado lógicamente. Sin organización activa, el filtro no devuelve filas (falla cerrado).
- `TenantAuditInterceptor` impide crear o mover registros fuera de la organización activa, convierte el borrado en borrado lógico y protege la auditoría. Los registros de `AuditLogs` son inmutables.
- `IgnoreQueryFilters()` solo se usa en `TenantAccessService` (validar membresía antes de fijar el contexto) y en `IPlatformQueryService` (SuperAdmin / arranque de sesión). Una prueba unitaria (`IgnoreQueryFiltersGuardTests`) falla si aparece en otro archivo de producción.

El resto de entidades de la sección 6 del plan se crea en la fase en que se use.

## Esquema físico de la Fase 5

Migración: `Phase5Advanced`. Todas las tablas son de organización (`TenantId`, filtro global, borrado lógico y `RowVersion`).

| Tabla | Notas |
|---|---|
| `NutritionTargets` | Objetivos diarios por alumno. Índice `(TenantId, AthleteProfileId, EffectiveFrom)`. Rangos validados en el dominio. |
| `FoodEntries` | Comidas por alumno y día. Índice `(TenantId, AthleteProfileId, EntryDate)`. CHECK de `MealType`. |
| `WaterLogs` | Registros de agua. Mismo índice que las comidas. |
| `AttendanceRecords` | **Único** `(TenantId, AthleteProfileId, AttendanceDate)`: un registro por alumno y día. CHECK de estado. |
| `Challenges` | Índice `(TenantId, Status, StartsAt)`. CHECK sobre tipo, métrica y estado. |
| `ChallengeParticipants` | **Único** `(TenantId, ChallengeId, AthleteProfileId)`. Guarda los puntos acumulados. |
| `ChallengeActivities` | Historial de puntos por participante. |
| `Achievements` | Logros por alumno, con reto de origen opcional. |
| `Conversations` | `Direct` o `Group`; `LastMessageAt`. |
| `ConversationParticipants` | **Único** `(TenantId, ConversationId, UserId)`; `LastReadAt` para los no leídos. |
| `ChatMessages` | Índice `(TenantId, ConversationId, SentAt)`. Adjuntos solo por URL. |
| `AppNotifications` | Índice `(TenantId, UserId, IsRead, RaisedAt)`. `LinkPath` relativo a la organización. |

Los días (`EntryDate`, `AttendanceDate`, `EffectiveFrom`) son `DateOnly`; los instantes, `DateTimeOffset` (se ordenan en memoria con SQLite).
