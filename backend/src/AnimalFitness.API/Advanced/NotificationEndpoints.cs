using AnimalFitness.API.Http;
using AnimalFitness.Application.Features.Communication;
using Microsoft.AspNetCore.Antiforgery;

namespace AnimalFitness.API.Advanced;

/// <summary>Endpoints de notificaciones in-app (Fase 5).</summary>
public static class NotificationEndpoints
{
    /// <summary>Registra rutas para listar y marcar notificaciones como leídas.</summary>
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/t/{slug}/notifications").WithTags("Notifications");

        group.MapGet("/", async (bool? unreadOnly, int? limit, INotificationService service, CancellationToken ct)
            => (await service.ListAsync(unreadOnly ?? false, limit, ct)).ToHttpResult());
        group.MapPost("/{id:guid}/read", (
            Guid id,
            INotificationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.MarkReadAsync(id, ct)));
        group.MapPost("/read-all", (
            INotificationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.MarkAllReadAsync(ct)));

        return endpoints;
    }
}
