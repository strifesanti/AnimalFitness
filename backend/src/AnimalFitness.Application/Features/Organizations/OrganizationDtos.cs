using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;

namespace AnimalFitness.Application.Features.Organizations;

/// <summary>Organización en listados de plataforma.</summary>
public sealed record TenantListItemDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string ContactEmail,
    string? Phone,
    DateTimeOffset CreatedAt);

/// <summary>Detalle de organización.</summary>
public sealed record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string ContactEmail,
    string? Phone,
    string TimeZoneId,
    string Currency,
    Guid? LogoFileId,
    string PrimaryColor,
    string Locale,
    string SettingsJson);

/// <summary>Crear organización (SuperAdmin).</summary>
public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string ContactEmail,
    string OwnerEmail,
    string? Phone = null,
    string? TimeZoneId = null,
    string? Currency = null);

/// <summary>Actualizar perfil de la organización activa.</summary>
public sealed record UpdateOrganizationRequest(
    string Name,
    string ContactEmail,
    string? Phone,
    string TimeZoneId,
    string Currency);

/// <summary>Actualizar preferencias visuales.</summary>
public sealed record UpdateOrganizationSettingsRequest(
    string? PrimaryColor,
    string? Locale,
    string? Currency,
    string? SettingsJson);

/// <summary>Miembro de la organización.</summary>
public sealed record MemberListItemDto(
    Guid MembershipId,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    MembershipRole Role,
    MembershipStatus Status,
    DateTimeOffset JoinedAt,
    Guid? AthleteProfileId,
    Guid? TrainerProfileId);

/// <summary>Cambiar rol de un miembro.</summary>
public sealed record ChangeMemberRoleRequest(MembershipRole Role);

/// <summary>Invitación pendiente o histórica.</summary>
public sealed record InvitationListItemDto(
    Guid Id,
    string Email,
    MembershipRole Role,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>Crear invitación.</summary>
public sealed record CreateInvitationRequest(string Email, MembershipRole Role);

/// <summary>Aceptar invitación con el token en claro.</summary>
public sealed record AcceptInvitationRequest(string Token);

/// <summary>Solicitud de ingreso.</summary>
public sealed record JoinRequestListItemDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    DateTimeOffset CreatedAt,
    string? RejectionReason);

/// <summary>Rechazar solicitud.</summary>
public sealed record RejectJoinRequestRequest(string Reason);

/// <summary>Directorio público de organizaciones para solicitar ingreso.</summary>
public sealed record OrganizationDirectoryItemDto(string Name, string Slug, TenantStatus Status);
