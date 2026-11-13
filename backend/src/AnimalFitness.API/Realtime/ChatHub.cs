using System.Security.Claims;
using AnimalFitness.Application.Abstractions.Tenancy;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Communication;
using AnimalFitness.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AnimalFitness.API.Realtime;

/// <summary>
/// Hub de chat en tiempo real (<c>/hubs/chat</c>). Usa las mismas cookies de Identity que la API.
/// La organización no se resuelve en la conexión: cada método recibe el <c>slug</c>, verifica la membresía
/// y que la persona participe en la conversación antes de tocar grupos o datos.
/// Evento recibido por los clientes: <c>messageReceived</c> (<see cref="ChatMessageDto"/>).
/// </summary>
[Authorize]
public sealed class ChatHub(
    ITenantAccessService tenantAccess,
    RequestContext requestContext,
    IChatService chat,
    ChatBroadcaster broadcaster) : Hub
{
    /// <summary>Suscribe la conexión a los mensajes de una conversación en la que participa.</summary>
    public async Task JoinConversation(string slug, Guid conversationId)
    {
        var tenantId = await EstablishAsync(slug);
        if (!await chat.IsParticipantAsync(conversationId, Context.ConnectionAborted))
        {
            throw new HubException("No se encontró la conversación.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ChatBroadcaster.GroupName(tenantId, conversationId),
            Context.ConnectionAborted);
    }

    /// <summary>Deja de recibir los mensajes de una conversación.</summary>
    public async Task LeaveConversation(string slug, Guid conversationId)
    {
        var tenantId = await EstablishAsync(slug);
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ChatBroadcaster.GroupName(tenantId, conversationId),
            Context.ConnectionAborted);
    }

    /// <summary>Envía un mensaje (persistido igual que por la API) y lo difunde al grupo.</summary>
    public async Task SendMessage(string slug, Guid conversationId, string? body, string? attachmentUrl)
    {
        var tenantId = await EstablishAsync(slug);
        var result = await chat.SendMessageAsync(
            conversationId,
            new SendChatMessageRequest(body, attachmentUrl),
            Context.ConnectionAborted);

        switch (result)
        {
            case AppResult.Failure failure:
                throw new HubException(failure.Title);
            case AppResult.Success<ChatMessageDto> success:
                await broadcaster.PublishAsync(tenantId, success.Value, Context.ConnectionAborted);
                break;
        }
    }

    private async Task<Guid> EstablishAsync(string slug)
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var userId) || userId == Guid.Empty)
        {
            throw new HubException("Debes iniciar sesión.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new HubException("Indica la organización.");
        }

        requestContext.EstablishUser(userId);
        var outcome = await tenantAccess.ResolveAsync(slug, userId, Context.ConnectionAborted);
        if (outcome is not TenantAccessOutcome.Granted granted)
        {
            throw new HubException("No tienes acceso a esta organización.");
        }

        requestContext.Establish(granted.Access.TenantId, granted.Access.UserId, granted.Access.Role);
        return granted.Access.TenantId;
    }
}
