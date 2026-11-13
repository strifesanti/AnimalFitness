using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Organizations;

/// <summary>Solicitudes de ingreso.</summary>
public interface IJoinRequestService
{
    /// <summary>Lista solicitudes de la organización activa.</summary>
    Task<AppResult> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Envía una solicitud de ingreso a una organización por slug.</summary>
    Task<AppResult> SubmitAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Aprueba una solicitud y crea la membresía de alumno.</summary>
    Task<AppResult> ApproveAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>Rechaza una solicitud con motivo.</summary>
    Task<AppResult> RejectAsync(Guid requestId, RejectJoinRequestRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancela la propia solicitud pendiente.</summary>
    Task<AppResult> CancelAsync(Guid requestId, CancellationToken cancellationToken = default);
}
