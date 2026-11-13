# Seguridad

> Estado: Fase 1 completada (cookies, CSRF, rate limiting, aislamiento tenant). Se actualiza con cada control nuevo.

## Autenticación

- Cookies HttpOnly, `Secure` (fuera de Testing), `SameSite=Lax`, prefijo `__Host-` en entornos reales.
- CSRF: cookie legible `AnimalFitness.XSRF` + cabecera `X-XSRF-TOKEN`.
- Rate limiting en registro, login, confirmación y recuperación (20/min por IP).
- Hashing con Identity (PBKDF2), longitud mínima 10, bloqueo tras 5 intentos fallidos (15 min).
- `SecurityStamp` se renueva al restablecer la contraseña.
- Respuestas neutras en registro duplicado y en forgot-password.
- Correo: `LoggingEmailSender` es **provisional** (solo log). Sustituir antes de Staging/Production.
- SuperAdmin de Development solo se crea si existen `Seed:SuperAdmin:Email` y `Seed:SuperAdmin:Password` (user-secrets). Sin credenciales predeterminadas en el código.

## Autorización

- Permisos granulares por política (`Permissions.Athletes.Read`, etc.) definidos en un mapa central.
- El rol de organización se toma de `TenantMemberships` en el servidor.
- Comprobaciones de propiedad en la capa Application para recursos individuales.
- Los alumnos nunca pueden consultar información de otros alumnos.

## Multi-tenant

- El dominio asigna `TenantId` al crear la entidad y no ofrece un setter público.
- `TenantResolutionMiddleware` toma un candidato (ruta `/t/{slug}`, cabecera `X-Tenant` o subdominio) y solo establece el contexto tras verificar membresía activa y organización no suspendida. Sin esa validación responde 401, 403 o 404.
- Filtros globales de EF Core. `IgnoreQueryFilters` solo aparece en `TenantAccessService` y `IPlatformQueryService`; una prueba de código fuente lo vigila.
- Claves de caché, grupos de SignalR y rutas de archivos prefijados con el `TenantId` (pendiente en módulos futuros).

## Riesgos identificados

| Riesgo | Mitigación |
|---|---|
| Acceso horizontal (IDOR) entre usuarios o tenants | Validación de pertenencia, filtros globales, claves foráneas compuestas, pruebas de integración |
| Fuga entre tenants por `IgnoreQueryFilters` o SQL crudo | Uso restringido, pruebas de arquitectura, SQL siempre parametrizado |
| Escalada de privilegios por invitaciones | Token con hash, un solo uso, expiración y rol asignado en el servidor |
| CSRF | Antiforgery, `SameSite` y CORS cerrado |
| XSS que robe la sesión | Cookie HttpOnly, CSP estricta y escape por defecto de React |
| Fuerza bruta y enumeración | Rate limiting en autenticación y bloqueo de cuenta |
| Datos sensibles (salud, medidas, fotos) | Consentimiento explícito y versionado, URLs firmadas de corta duración, cifrado en reposo, auditoría de accesos y derechos de consulta, actualización y eliminación (Ley 1581 de 2012) |
| Subida de archivos maliciosos | Validación del tipo real, límite de tamaño, nombres generados por el servidor, almacenamiento privado (Fase 4) |
| Secretos en el repositorio | `.env.example`, user-secrets, Key Vault y escaneo de secretos en CI |
| Datos personales en logs | Logging estructurado con enmascaramiento |
| Exposición de errores internos | `ProblemDetails` sin el mensaje de la excepción fuera de Development. OpenAPI solo se publica en Development. El encabezado `Server` de Kestrel está desactivado. |

## Datos de desarrollo

- Los datos demo solo se cargan en `Development`.
- El SuperAdmin de desarrollo se crea con un comando explícito y sus credenciales vienen de user-secrets o variables de entorno.
- No existen credenciales predeterminadas en ningún entorno.

## Reporte de vulnerabilidades

_Pendiente: definir el canal de contacto antes de la primera publicación._
