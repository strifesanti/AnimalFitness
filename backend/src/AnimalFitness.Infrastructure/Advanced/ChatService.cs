using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Communication;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Communication;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Advanced;

/// <summary>Chat interno de la organización (Fase 5).</summary>
public sealed class ChatService(
    AppDbContext db,
    AdminAccess access,
    INotificationService notifications,
    TimeProvider timeProvider) : IChatService
{
    private const int DefaultMessages = 50;
    private const int MaxMessages = 200;
    private const int MaxGroupMembers = 50;
    private const int PreviewLength = 120;

    // ------------------------------------------------------------------
    // Contactos y conversaciones
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListContactsAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var me = access.RequireUserId();
        var memberships = await db.TenantMemberships.AsNoTracking()
            .Where(item => item.Status == MembershipStatus.Active && item.UserId != me)
            .ToListAsync(cancellationToken);
        var names = await NamesAsync(memberships.Select(item => item.UserId).ToList(), cancellationToken);

        var items = memberships
            .Select(item => new ChatContactDto(item.UserId, names.GetValueOrDefault(item.UserId, string.Empty), item.Role))
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new AppResult.Success<IReadOnlyList<ChatContactDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> ListConversationsAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var me = access.RequireUserId();
        var mine = await db.ConversationParticipants.AsNoTracking()
            .Where(item => item.UserId == me)
            .ToListAsync(cancellationToken);
        var items = await BuildConversationsAsync(mine, me, cancellationToken);
        return new AppResult.Success<IReadOnlyList<ConversationDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateConversationAsync(CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var me = access.RequireUserId();
        var others = (request.ParticipantUserIds ?? [])
            .Where(id => id != Guid.Empty && id != me)
            .Distinct()
            .ToList();

        if (!Enum.IsDefined(request.Kind))
        {
            return AdminAccess.Fail("El tipo de conversación no es válido.", 400);
        }

        if (request.Kind == ConversationKind.Direct && others.Count != 1)
        {
            return AdminAccess.Fail("Una conversación directa es entre dos personas.", 400);
        }

        if (request.Kind == ConversationKind.Group
            && (others.Count < 1 || others.Count > MaxGroupMembers || string.IsNullOrWhiteSpace(request.Title)))
        {
            return AdminAccess.Fail("Un grupo necesita título y entre 1 y 50 personas más.", 400);
        }

        var activeCount = await db.TenantMemberships
            .CountAsync(item => item.Status == MembershipStatus.Active && others.Contains(item.UserId), cancellationToken);
        if (activeCount != others.Count)
        {
            return AdminAccess.Fail("Todas las personas deben ser miembros activos de la organización.", 400);
        }

        if (request.Kind == ConversationKind.Direct)
        {
            var existing = await FindDirectAsync(me, others[0], cancellationToken);
            if (existing is not null)
            {
                return new AppResult.Success<ConversationDto>(existing, "La conversación ya existía.");
            }
        }

        var now = timeProvider.GetUtcNow();
        var tenantId = access.RequireTenantId();
        Conversation conversation;
        try
        {
            conversation = Conversation.Create(tenantId, request.Kind, now, me, request.Title);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.Conversations.Add(conversation);
        foreach (var userId in others.Prepend(me))
        {
            db.ConversationParticipants.Add(ConversationParticipant.Create(tenantId, conversation.Id, userId, now, me));
        }

        await db.SaveChangesAsync(cancellationToken);

        var participants = await db.ConversationParticipants.AsNoTracking()
            .Where(item => item.UserId == me && item.ConversationId == conversation.Id)
            .ToListAsync(cancellationToken);
        var created = (await BuildConversationsAsync(participants, me, cancellationToken)).First();
        return new AppResult.Success<ConversationDto>(created, "Conversación creada.");
    }

    // ------------------------------------------------------------------
    // Mensajes
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListMessagesAsync(Guid conversationId, int? limit, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (await FindMyParticipationAsync(conversationId, tracking: false, cancellationToken) is null)
        {
            return AdminAccess.Fail("No se encontró la conversación.", 404);
        }

        var take = Math.Clamp(limit ?? DefaultMessages, 1, MaxMessages);

        // SQLite no ordena DateTimeOffset: se ordena en memoria y se devuelven los últimos mensajes.
        var messages = (await db.ChatMessages.AsNoTracking()
                .Where(item => item.ConversationId == conversationId)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.SentAt)
            .Take(take)
            .OrderBy(item => item.SentAt)
            .ToList();

        var names = await NamesAsync(messages.Select(item => item.SenderUserId).Distinct().ToList(), cancellationToken);
        return new AppResult.Success<IReadOnlyList<ChatMessageDto>>(
            messages.Select(item => ToDto(item, names.GetValueOrDefault(item.SenderUserId, string.Empty))).ToList());
    }

    /// <inheritdoc />
    public async Task<AppResult> SendMessageAsync(Guid conversationId, SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var me = access.RequireUserId();
        var participation = await FindMyParticipationAsync(conversationId, tracking: true, cancellationToken);
        var conversation = participation is null
            ? null
            : await db.Conversations.FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (participation is null || conversation is null)
        {
            return AdminAccess.Fail("No se encontró la conversación.", 404);
        }

        if (!string.IsNullOrWhiteSpace(request.AttachmentUrl)
            && !(Uri.TryCreate(request.AttachmentUrl.Trim(), UriKind.Absolute, out var uri)
                && uri.Scheme is "http" or "https"))
        {
            return AdminAccess.Fail("El adjunto debe ser una URL http o https.", 400);
        }

        var now = timeProvider.GetUtcNow();
        ChatMessage message;
        try
        {
            message = ChatMessage.Create(
                access.RequireTenantId(),
                conversationId,
                me,
                request.Body ?? string.Empty,
                now,
                request.AttachmentUrl);
            conversation.TouchLastMessage(now, me);
            participation.MarkRead(now, me);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var names = await NamesAsync([me], cancellationToken);
        var senderName = names.GetValueOrDefault(me, string.Empty);
        await NotifyRecipientsAsync(conversation, message, senderName, me, cancellationToken);

        return new AppResult.Success<ChatMessageDto>(ToDto(message, senderName), "Mensaje enviado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> MarkReadAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var participation = await FindMyParticipationAsync(conversationId, tracking: true, cancellationToken);
        if (participation is null)
        {
            return AdminAccess.Fail("No se encontró la conversación.", 404);
        }

        participation.MarkRead(timeProvider.GetUtcNow(), access.RequireUserId());
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success("Conversación marcada como leída.");
    }

    /// <inheritdoc />
    public async Task<bool> IsParticipantAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChatUse, cancellationToken);
        return failure is null
            && await FindMyParticipationAsync(conversationId, tracking: false, cancellationToken) is not null;
    }

    // ------------------------------------------------------------------
    // Auxiliares
    // ------------------------------------------------------------------

    private async Task<ConversationParticipant?> FindMyParticipationAsync(
        Guid conversationId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var me = access.RequireUserId();
        var query = tracking ? db.ConversationParticipants : db.ConversationParticipants.AsNoTracking();
        return await query.FirstOrDefaultAsync(
            item => item.ConversationId == conversationId && item.UserId == me,
            cancellationToken);
    }

    private async Task<ConversationDto?> FindDirectAsync(Guid me, Guid other, CancellationToken cancellationToken)
    {
        var mine = await db.ConversationParticipants.AsNoTracking()
            .Where(item => item.UserId == me)
            .ToListAsync(cancellationToken);
        var myIds = mine.Select(item => item.ConversationId).ToList();
        if (myIds.Count == 0)
        {
            return null;
        }

        var directIds = await db.Conversations.AsNoTracking()
            .Where(item => myIds.Contains(item.Id) && item.Kind == ConversationKind.Direct)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (directIds.Count == 0)
        {
            return null;
        }

        var participants = await db.ConversationParticipants.AsNoTracking()
            .Where(item => directIds.Contains(item.ConversationId))
            .ToListAsync(cancellationToken);
        var match = participants
            .GroupBy(item => item.ConversationId)
            .FirstOrDefault(group => group.Count() == 2 && group.Any(item => item.UserId == other));
        if (match is null)
        {
            return null;
        }

        var mineMatch = mine.Where(item => item.ConversationId == match.Key).ToList();
        return (await BuildConversationsAsync(mineMatch, me, cancellationToken)).FirstOrDefault();
    }

    private async Task<List<ConversationDto>> BuildConversationsAsync(
        IReadOnlyList<ConversationParticipant> mine,
        Guid me,
        CancellationToken cancellationToken)
    {
        var ids = mine.Select(item => item.ConversationId).ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var conversations = await db.Conversations.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var participants = await db.ConversationParticipants.AsNoTracking()
            .Where(item => ids.Contains(item.ConversationId))
            .ToListAsync(cancellationToken);
        var names = await NamesAsync(participants.Select(item => item.UserId).Distinct().ToList(), cancellationToken);

        // Proyección ligera (sin cuerpos) para contar no leídos y localizar el último mensaje.
        var slim = (await db.ChatMessages.AsNoTracking()
                .Where(item => ids.Contains(item.ConversationId))
                .Select(item => new { item.Id, item.ConversationId, item.SenderUserId, item.SentAt })
                .ToListAsync(cancellationToken))
            .ToList();
        var lastIds = slim
            .GroupBy(item => item.ConversationId)
            .Select(group => group.OrderByDescending(item => item.SentAt).First().Id)
            .ToList();
        var previews = lastIds.Count == 0
            ? new Dictionary<Guid, ChatMessage>()
            : await db.ChatMessages.AsNoTracking()
                .Where(item => lastIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.ConversationId, cancellationToken);

        return conversations
            .Select(conversation =>
            {
                var members = participants.Where(item => item.ConversationId == conversation.Id).ToList();
                var lastRead = mine.First(item => item.ConversationId == conversation.Id).LastReadAt;
                var unread = slim.Count(item =>
                    item.ConversationId == conversation.Id
                    && item.SenderUserId != me
                    && (lastRead is null || item.SentAt > lastRead));
                var title = conversation.Kind == ConversationKind.Group
                    ? conversation.Title ?? "Grupo"
                    : string.Join(
                        ", ",
                        members.Where(item => item.UserId != me).Select(item => names.GetValueOrDefault(item.UserId, "—")));
                previews.TryGetValue(conversation.Id, out var last);
                return new ConversationDto(
                    conversation.Id,
                    conversation.Kind,
                    string.IsNullOrWhiteSpace(title) ? "Conversación" : title,
                    conversation.LastMessageAt,
                    last is null ? null : Preview(last),
                    unread,
                    members.Select(item => new ChatParticipantDto(item.UserId, names.GetValueOrDefault(item.UserId, string.Empty))).ToList());
            })
            .OrderByDescending(item => item.LastMessageAt ?? DateTimeOffset.MinValue)
            .ToList();
    }

    private async Task NotifyRecipientsAsync(
        Conversation conversation,
        ChatMessage message,
        string senderName,
        Guid me,
        CancellationToken cancellationToken)
    {
        var recipients = await db.ConversationParticipants.AsNoTracking()
            .Where(item => item.ConversationId == conversation.Id && item.UserId != me)
            .Select(item => item.UserId)
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        // Una sola notificación sin leer por conversación y persona, para no saturar.
        var link = $"/chat?c={conversation.Id:D}";
        var alreadyNotified = (await db.AppNotifications.AsNoTracking()
                .Where(item => recipients.Contains(item.UserId) && !item.IsRead && item.LinkPath == link)
                .Select(item => item.UserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var userId in recipients.Where(id => !alreadyNotified.Contains(id)))
        {
            await notifications.CreateAsync(
                userId,
                NotificationKind.ChatMessage,
                string.IsNullOrWhiteSpace(senderName) ? "Nuevo mensaje" : $"Mensaje de {senderName}",
                Preview(message),
                link,
                cancellationToken);
        }
    }

    private async Task<Dictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await db.UserProfiles.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName.Trim(), cancellationToken);
    }

    private static string Preview(ChatMessage message)
    {
        var text = string.IsNullOrWhiteSpace(message.Body) ? "📎 Adjunto" : message.Body.Trim();
        return text.Length <= PreviewLength ? text : text[..PreviewLength] + "…";
    }

    private static ChatMessageDto ToDto(ChatMessage item, string senderName)
        => new(item.Id, item.ConversationId, item.SenderUserId, senderName, item.Body, item.AttachmentUrl, item.SentAt);
}
