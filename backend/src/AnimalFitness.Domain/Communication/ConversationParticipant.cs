using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Communication;

/// <summary>Participante de una conversación (usuario global).</summary>
public sealed class ConversationParticipant : TenantScopedEntity
{
    private ConversationParticipant(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Conversación.</summary>
    public Guid ConversationId { get; private set; }

    /// <summary>Usuario.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Última lectura.</summary>
    public DateTimeOffset? LastReadAt { get; private set; }

    /// <summary>Une a un usuario.</summary>
    public static ConversationParticipant Create(
        Guid tenantId,
        Guid conversationId,
        Guid userId,
        DateTimeOffset now,
        Guid? createdBy)
    {
        var participant = new ConversationParticipant(tenantId)
        {
            ConversationId = TextRules.RequiredId(conversationId, "La conversación"),
            UserId = TextRules.RequiredId(userId, "El usuario"),
        };
        participant.MarkCreated(now, createdBy);
        return participant;
    }

    /// <summary>Marca como leído.</summary>
    public void MarkRead(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        MarkUpdated(now, actor);
        LastReadAt = now;
    }
}
