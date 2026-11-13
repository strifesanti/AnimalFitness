using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;

namespace AnimalFitness.Application.Abstractions.Tenancy;

/// <summary>
/// Acceso verificado de una persona a una organización.
/// </summary>
/// <param name="TenantId">Organización.</param>
/// <param name="Slug">Identificador legible.</param>
/// <param name="TenantStatus">Estado operativo de la organización.</param>
/// <param name="UserId">Persona.</param>
/// <param name="Role">Rol dentro de la organización.</param>
/// <param name="MembershipStatus">Estado de la membresía.</param>
public sealed record TenantAccess(
    Guid TenantId,
    string Slug,
    TenantStatus TenantStatus,
    Guid UserId,
    MembershipRole Role,
    MembershipStatus MembershipStatus);
