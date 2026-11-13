using AnimalFitness.API.Http;
using AnimalFitness.API.Realtime;
using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Communication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Advanced;

/// <summary>Endpoints de chat (Fase 5). Los mensajes nuevos también se difunden por SignalR.</summary>
public static class ChatEndpoints
{
    /// <summary>Registra rutas de contactos, conversaciones y mensajes.</summary>
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/t/{slug}/chat").WithTags("Chat");

        group.MapGet("/contacts", async (IChatService service, CancellationToken ct)
            => (await service.ListContactsAsync(ct)).ToHttpResult());
        group.MapGet("/conversations", async (IChatService service, CancellationToken ct)
            => (await service.ListConversationsAsync(ct)).ToHttpResult());
        group.MapPost("/conversations", (
            [FromBody] CreateConversationRequest body,
            IChatService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CreateConversationAsync(body, ct)));

        group.MapGet("/conversations/{id:guid}/messages", async (
            Guid id,
            int? limit,
            IChatService service,
            CancellationToken ct)
            => (await service.ListMessagesAsync(id, limit, ct)).ToHttpResult());
        group.MapPost("/conversations/{id:guid}/messages", (
            Guid id,
            [FromBody] SendChatMessageRequest body,
            IChatService service,
            IRequestContext requestContext,
            ChatBroadcaster broadcaster,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, async () =>
            {
                var result = await service.SendMessageAsync(id, body, ct);
                if (result is AppResult.Success<ChatMessageDto> success && requestContext.TenantId is { } tenantId)
                {
                    await broadcaster.PublishAsync(tenantId, success.Value, ct);
                }

                return result;
            }));
        group.MapPost("/conversations/{id:guid}/read", (
            Guid id,
            IChatService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.MarkReadAsync(id, ct)));

        return endpoints;
    }
}
