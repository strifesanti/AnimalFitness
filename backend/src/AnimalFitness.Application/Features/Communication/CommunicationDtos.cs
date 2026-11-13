using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Communication;

namespace AnimalFitness.Application.Features.Communication;

/// <summary>Persona de la organización con la que se puede conversar.</summary>
public sealed record ChatContactDto(Guid UserId, string Name, MembershipRole Role);

/// <summary>Participante de una conversación.</summary>
public sealed record ChatParticipantDto(Guid UserId, string Name);

/// <summary>Conversación del usuario actual.</summary>
public sealed record ConversationDto(
    Guid Id,
    ConversationKind Kind,
    string Title,
    DateTimeOffset? LastMessageAt,
    string? LastMessagePreview,
    int UnreadCount,
    IReadOnlyList<ChatParticipantDto> Participants);

/// <summary>
/// Crear una conversación. Directa: exactamente un usuario en <c>ParticipantUserIds</c> (se reutiliza si ya existe).
/// Grupo: título y al menos un usuario.
/// </summary>
public sealed record CreateConversationRequest(
    ConversationKind Kind,
    string? Title,
    IReadOnlyList<Guid> ParticipantUserIds);

/// <summary>Mensaje de chat.</summary>
public sealed record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderName,
    string Body,
    string? AttachmentUrl,
    DateTimeOffset SentAt);

/// <summary>Enviar un mensaje (texto, adjunto por URL o ambos).</summary>
public sealed record SendChatMessageRequest(string? Body, string? AttachmentUrl);

/// <summary>Notificación in-app.</summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationKind Kind,
    string Title,
    string Body,
    string? LinkPath,
    bool IsRead,
    DateTimeOffset RaisedAt);

/// <summary>Notificaciones del usuario con el total sin leer.</summary>
public sealed record NotificationListDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);
