using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Communication;

/// <summary>Notificación in-app para un usuario dentro de una organización.</summary>
public sealed class AppNotification : TenantScopedEntity
{
    private AppNotification(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Destinatario.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Tipo.</summary>
    public NotificationKind Kind { get; private set; }

    /// <summary>Título.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Cuerpo.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>Enlace relativo opcional.</summary>
    public string? LinkPath { get; private set; }

    /// <summary>Leída.</summary>
    public bool IsRead { get; private set; }

    /// <summary>Momento de creación lógica.</summary>
    public DateTimeOffset RaisedAt { get; private set; }

    /// <summary>Crea una notificación.</summary>
    public static AppNotification Create(
        Guid tenantId,
        Guid userId,
        NotificationKind kind,
        string title,
        string body,
        DateTimeOffset now,
        string? linkPath = null)
    {
        var notification = new AppNotification(tenantId)
        {
            UserId = TextRules.RequiredId(userId, "El destinatario"),
            Kind = TextRules.Defined(kind, "El tipo"),
            Title = TextRules.Required(title, "El título", FieldLengths.NotificationTitle),
            Body = TextRules.Required(body, "El cuerpo", FieldLengths.MessageBody),
            LinkPath = string.IsNullOrWhiteSpace(linkPath)
                ? null
                : TextRules.Required(linkPath, "El enlace", FieldLengths.MediaUrl),
            IsRead = false,
            RaisedAt = now,
        };
        notification.MarkCreated(now, null);
        return notification;
    }

    /// <summary>Marca como leída.</summary>
    public void MarkRead(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (IsRead)
        {
            return;
        }

        MarkUpdated(now, actor);
        IsRead = true;
    }
}
