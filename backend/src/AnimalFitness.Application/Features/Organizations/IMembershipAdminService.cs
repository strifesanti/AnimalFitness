using AnimalFitness.Application.Common;
using AnimalFitness.Domain.Accounts;

namespace AnimalFitness.Application.Features.Organizations;

/// <summary>Administración de miembros de la organización activa.</summary>
public interface IMembershipAdminService
{
    /// <summary>Lista miembros, opcionalmente filtrados por rol.</summary>
    Task<AppResult> ListMembersAsync(MembershipRole? role, CancellationToken cancellationToken = default);

    /// <summary>Cambia el rol de un miembro.</summary>
    Task<AppResult> ChangeRoleAsync(Guid membershipId, ChangeMemberRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Suspende una membresía.</summary>
    Task<AppResult> SuspendAsync(Guid membershipId, CancellationToken cancellationToken = default);

    /// <summary>Reactiva una membresía suspendida.</summary>
    Task<AppResult> ReactivateAsync(Guid membershipId, CancellationToken cancellationToken = default);

    /// <summary>Retira una membresía.</summary>
    Task<AppResult> RemoveAsync(Guid membershipId, CancellationToken cancellationToken = default);
}
