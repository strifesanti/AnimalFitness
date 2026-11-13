using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Communication;

/// <summary>Mensaje de chat. El contenido de archivos/fotos se referencia por URL.</summary>
public sealed class ChatMessage : TenantScopedEntity
{
    private ChatMessage(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Conversación.</summary>
    public Guid ConversationId { get; private set; }

    /// <summary>Autor.</summary>
    public Guid SenderUserId { get; private set; }

    /// <summary>Texto.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>URL de adjunto opcional.</summary>
    public string? AttachmentUrl { get; private set; }

    /// <summary>Momento de envío.</summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>Crea un mensaje.</summary>
    public static ChatMessage Create(
        Guid tenantId,
        Guid conversationId,
        Guid senderUserId,
        string body,
        DateTimeOffset now,
        string? attachmentUrl = null)
    {
        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(attachmentUrl))
        {
            throw new DomainException("El mensaje debe tener texto o un adjunto.");
        }

        var message = new ChatMessage(tenantId)
        {
            ConversationId = TextRules.RequiredId(conversationId, "La conversación"),
            SenderUserId = TextRules.RequiredId(senderUserId, "El remitente"),
            Body = string.IsNullOrWhiteSpace(body)
                ? string.Empty
                : TextRules.Required(body, "El mensaje", FieldLengths.MessageBody),
            AttachmentUrl = string.IsNullOrWhiteSpace(attachmentUrl)
                ? null
                : TextRules.Required(attachmentUrl, "El adjunto", FieldLengths.MediaUrl),
            SentAt = now,
        };
        message.MarkCreated(now, senderUserId);
        return message;
    }
}
