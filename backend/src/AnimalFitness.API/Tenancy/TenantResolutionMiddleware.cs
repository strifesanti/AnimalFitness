using System.Security.Claims;
using AnimalFitness.Application.Abstractions.Tenancy;
using AnimalFitness.Infrastructure.Tenancy;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Resuelve el candidato de organización, valida la membresía y establece el contexto de la petición.
/// El valor enviado por el cliente nunca se acepta sin esa validación.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly PathString[] SkipTenantPrefixes =
    [
        new("/health"),
        new("/openapi"),
        new("/api/v1/info"),
        new("/api/v1/auth"),
        new("/api/v1/platform"),
        new("/api/v1/organizations"),
        new("/api/v1/invitations"),
        new("/api/v1/join-requests"),
        new("/api/v1/profile"),
        // SignalR: la conexión no necesita organización; cada método del hub verifica la membresía.
        new("/hubs"),
    ];

    /// <summary>
    /// Procesa la petición.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext httpContext,
        CompositeTenantCandidateResolver candidateResolver,
        ITenantAccessService accessService,
        RequestContext requestContext)
    {
        var userId = TryGetUserId(httpContext.User);
        if (userId is not null)
        {
            requestContext.EstablishUser(userId.Value);
        }

        if (SkipTenantPrefixes.Any(prefix =>
                httpContext.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next(httpContext);
            return;
        }

        var candidate = candidateResolver.Resolve(httpContext);
        if (candidate is null)
        {
            await next(httpContext);
            return;
        }

        if (userId is null)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Debes iniciar sesión para acceder a esta organización.");
            return;
        }

        var outcome = await accessService.ResolveAsync(candidate.Slug, userId.Value, httpContext.RequestAborted);
        switch (outcome)
        {
            case TenantAccessOutcome.NotFound:
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "No se encontró la organización.");
                return;

            case TenantAccessOutcome.Suspended:
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "La organización está suspendida.");
                return;

            case TenantAccessOutcome.Forbidden forbidden:
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    forbidden.Reason);
                return;

            case TenantAccessOutcome.Granted granted:
                requestContext.Establish(granted.Access.TenantId, granted.Access.UserId, granted.Access.Role);
                httpContext.Response.Headers["X-Tenant-Id"] = granted.Access.TenantId.ToString("D");
                httpContext.Response.Headers["X-Tenant-Slug"] = granted.Access.Slug;
                await next(httpContext);
                return;

            default:
                throw new InvalidOperationException($"Resultado de acceso no contemplado: {outcome.GetType().Name}");
        }
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) && userId != Guid.Empty ? userId : null;
    }

    private static async Task WriteProblemAsync(HttpContext httpContext, int statusCode, string title)
    {
        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(
                title: title,
                statusCode: statusCode,
                instance: httpContext.Request.Path.Value,
                type: "https://tools.ietf.org/html/rfc9457")
            .ExecuteAsync(httpContext);
    }
}
