using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Communication;
using AnimalFitness.Domain.Communication;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Advanced;

/// <summary>Notificaciones in-app (Fase 5).</summary>
public sealed class NotificationService(
    AppDbContext db,
    AdminAccess access,
    TimeProvider timeProvider) : INotificationService
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    /// <inheritdoc />
    public async Task<AppResult> ListAsync(bool unreadOnly, int? limit, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.NotificationsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var userId = access.RequireUserId();
        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var mine = await db.AppNotifications.AsNoTracking()
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

        var items = mine
            .Where(item => !unreadOnly || !item.IsRead)
            .OrderByDescending(item => item.RaisedAt)
            .Take(take)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<NotificationListDto>(
            new NotificationListDto(mine.Count(item => !item.IsRead), items));
    }

    /// <inheritdoc />
    public async Task<AppResult> MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.NotificationsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var userId = access.RequireUserId();
        var notification = await db.AppNotifications
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return AdminAccess.Fail("No se encontró la notificación.", 404);
        }

        notification.MarkRead(timeProvider.GetUtcNow(), userId);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<NotificationDto>(ToDto(notification));
    }

    /// <inheritdoc />
    public async Task<AppResult> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.NotificationsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var userId = access.RequireUserId();
        var now = timeProvider.GetUtcNow();
        var unread = await db.AppNotifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.MarkRead(now, userId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success($"{unread.Count} notificaciones marcadas como leídas.");
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        Guid userId,
        NotificationKind kind,
        string title,
        string body,
        string? linkPath = null,
        CancellationToken cancellationToken = default)
    {
        var notification = AppNotification.Create(
            access.RequireTenantId(),
            userId,
            kind,
            title,
            body,
            timeProvider.GetUtcNow(),
            linkPath);
        db.AppNotifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static NotificationDto ToDto(AppNotification item)
        => new(item.Id, item.Kind, item.Title, item.Body, item.LinkPath, item.IsRead, item.RaisedAt);
}
