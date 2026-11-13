using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Organizations;

/// <summary>Invitaciones a la organización.</summary>
public interface IInvitationService
{
    /// <summary>Lista invitaciones de la organización activa.</summary>
    Task<AppResult> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Crea una invitación y envía el correo.</summary>
    Task<AppResult> CreateAsync(CreateInvitationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancela una invitación pendiente.</summary>
    Task<AppResult> CancelAsync(Guid invitationId, CancellationToken cancellationToken = default);

    /// <summary>Acepta una invitación con el token en claro.</summary>
    Task<AppResult> AcceptAsync(AcceptInvitationRequest request, CancellationToken cancellationToken = default);
}
