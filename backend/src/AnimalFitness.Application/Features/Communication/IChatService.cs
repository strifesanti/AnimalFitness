using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Communication;

/// <summary>
/// Chat interno de la organización. Cada usuario solo ve las conversaciones en las que participa.
/// </summary>
public interface IChatService
{
    /// <summary>Lista a las personas activas de la organización con las que se puede iniciar un chat.</summary>
    Task<AppResult> ListContactsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista las conversaciones del usuario actual (más recientes primero) con el total sin leer.</summary>
    Task<AppResult> ListConversationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Crea una conversación directa (2 usuarios) o de grupo.</summary>
    Task<AppResult> CreateConversationAsync(CreateConversationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista los mensajes de una conversación en orden cronológico (los últimos <paramref name="limit"/>).</summary>
    Task<AppResult> ListMessagesAsync(Guid conversationId, int? limit, CancellationToken cancellationToken = default);

    /// <summary>Guarda un mensaje y devuelve su proyección para difundirla en tiempo real.</summary>
    Task<AppResult> SendMessageAsync(Guid conversationId, SendChatMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marca la conversación como leída para el usuario actual.</summary>
    Task<AppResult> MarkReadAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>Indica si el usuario actual participa en la conversación (usado por el hub de tiempo real).</summary>
    Task<bool> IsParticipantAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
