using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;

namespace AnimalFitness.Application.Features.Auth;

/// <summary>Solicitud de registro.</summary>
/// <param name="Email">Correo.</param>
/// <param name="Password">Contraseña.</param>
/// <param name="FirstName">Nombres.</param>
/// <param name="LastName">Apellidos.</param>
/// <param name="PhoneNumber">Teléfono opcional.</param>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);

/// <summary>Solicitud de inicio de sesión.</summary>
/// <param name="Email">Correo.</param>
/// <param name="Password">Contraseña.</param>
/// <param name="RememberMe">Mantener la sesión más tiempo.</param>
public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);

/// <summary>Confirmación de correo.</summary>
/// <param name="UserId">Identificador de la persona.</param>
/// <param name="Token">Token de un solo uso.</param>
public sealed record ConfirmEmailRequest(Guid UserId, string Token);

/// <summary>Solicitud de recuperación de contraseña.</summary>
/// <param name="Email">Correo.</param>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>Restablecimiento de contraseña.</summary>
/// <param name="Email">Correo.</param>
/// <param name="Token">Token de un solo uso.</param>
/// <param name="NewPassword">Nueva contraseña.</param>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <summary>Membresía visible en /me.</summary>
/// <param name="TenantId">Organización.</param>
/// <param name="Slug">Identificador legible.</param>
/// <param name="TenantName">Nombre de la organización.</param>
/// <param name="TenantStatus">Estado de la organización.</param>
/// <param name="Role">Rol en la organización.</param>
/// <param name="MembershipStatus">Estado de la membresía.</param>
public sealed record MembershipSummaryDto(
    Guid TenantId,
    string Slug,
    string TenantName,
    TenantStatus TenantStatus,
    MembershipRole Role,
    MembershipStatus MembershipStatus);

/// <summary>Respuesta de /me y de login/register.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Email">Correo.</param>
/// <param name="FirstName">Nombres.</param>
/// <param name="LastName">Apellidos.</param>
/// <param name="PhoneNumber">Teléfono.</param>
/// <param name="EmailConfirmed">Correo confirmado.</param>
/// <param name="IsSuperAdmin">Es administrador global.</param>
/// <param name="Memberships">Membresías activas.</param>
/// <param name="ActiveTenantId">Organización activa en la petición, si hay.</param>
/// <param name="ActiveRole">Rol en la organización activa, si hay.</param>
public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool IsSuperAdmin,
    IReadOnlyList<MembershipSummaryDto> Memberships,
    Guid? ActiveTenantId,
    MembershipRole? ActiveRole);

/// <summary>Resultado de una operación de autenticación.</summary>
public abstract record AuthResult
{
    /// <summary>Operación correcta.</summary>
    public sealed record Success(AuthUserDto? User = null, string? Message = null) : AuthResult;

    /// <summary>Credenciales o estado inválido. El mensaje es neutro hacia el cliente cuando corresponde.</summary>
    public sealed record Failure(string Title, int StatusCode, string? Detail = null) : AuthResult;
}
