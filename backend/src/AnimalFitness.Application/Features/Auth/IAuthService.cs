namespace AnimalFitness.Application.Features.Auth;

/// <summary>
/// Casos de uso de autenticación y sesión.
/// </summary>
public interface IAuthService
{
    /// <summary>Registra una cuenta y abre sesión.</summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Inicia sesión.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cierra la sesión actual.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Devuelve el perfil de la persona autenticada.</summary>
    Task<AuthResult> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>Confirma el correo con el token recibido.</summary>
    Task<AuthResult> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>Solicita un correo de recuperación. La respuesta es siempre neutra.</summary>
    Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Restablece la contraseña con el token recibido.</summary>
    Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
