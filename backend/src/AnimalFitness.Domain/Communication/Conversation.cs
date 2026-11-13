using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Communication;

/// <summary>Conversación de chat acotada a una organización.</summary>
public sealed class Conversation : TenantScopedEntity
{
    private Conversation(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Tipo.</summary>
    public ConversationKind Kind { get; private set; }

    /// <summary>Título opcional (grupos).</summary>
    public string? Title { get; private set; }

    /// <summary>Último mensaje (UTC).</summary>
    public DateTimeOffset? LastMessageAt { get; private set; }

    /// <summary>Crea una conversación.</summary>
    public static Conversation Create(
        Guid tenantId,
        ConversationKind kind,
        DateTimeOffset now,
        Guid? createdBy,
        string? title = null)
    {
        var conversation = new Conversation(tenantId)
        {
            Kind = TextRules.Defined(kind, "El tipo"),
            Title = string.IsNullOrWhiteSpace(title)
                ? null
                : TextRules.Required(title, "El título", FieldLengths.NutritionOrChallengeName),
        };
        conversation.MarkCreated(now, createdBy);
        return conversation;
    }

    /// <summary>Actualiza la marca de último mensaje.</summary>
    public void TouchLastMessage(DateTimeOffset at, Guid? actor)
    {
        EnsureNotDeleted();
        MarkUpdated(at, actor);
        LastMessageAt = at;
    }
}
