using AnimalFitness.Application.Common;
using AnimalFitness.Domain.Communication;

namespace AnimalFitness.Application.Features.Communication;

/// <summary>Notificaciones in-app del usuario actual y creación desde otros servicios.</summary>
public interface INotificationService
{
    /// <summary>Lista las notificaciones del usuario actual (más recientes primero).</summary>
    Task<AppResult> ListAsync(bool unreadOnly, int? limit, CancellationToken cancellationToken = default);

    /// <summary>Marca una notificación propia como leída.</summary>
    Task<AppResult> MarkReadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Marca todas las notificaciones propias como leídas.</summary>
    Task<AppResult> MarkAllReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una notificación para un usuario de la organización activa. Es un auxiliar para otros servicios:
    /// no comprueba permisos y guarda los cambios pendientes del contexto.
    /// </summary>
    Task CreateAsync(
        Guid userId,
        NotificationKind kind,
        string title,
        string body,
        string? linkPath = null,
        CancellationToken cancellationToken = default);
}
