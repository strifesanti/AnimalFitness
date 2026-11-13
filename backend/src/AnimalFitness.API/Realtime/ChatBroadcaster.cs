using AnimalFitness.Application.Features.Communication;
using Microsoft.AspNetCore.SignalR;

namespace AnimalFitness.API.Realtime;

/// <summary>
/// Difunde mensajes de chat a los clientes conectados al grupo de su conversación.
/// El grupo incluye la organización para que nunca se mezclen conversaciones entre academias.
/// </summary>
public sealed class ChatBroadcaster(IHubContext<ChatHub> hub)
{
    /// <summary>Nombre del evento que reciben los clientes.</summary>
    public const string MessageReceivedEvent = "messageReceived";

    /// <summary>Nombre del grupo de SignalR de una conversación.</summary>
    public static string GroupName(Guid tenantId, Guid conversationId)
        => $"tenant:{tenantId:D}:conversation:{conversationId:D}";

    /// <summary>Envía el mensaje al grupo de su conversación.</summary>
    public Task PublishAsync(Guid tenantId, ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return hub.Clients
            .Group(GroupName(tenantId, message.ConversationId))
            .SendAsync(MessageReceivedEvent, message, cancellationToken);
    }
}
