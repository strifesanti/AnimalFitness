using System.Security.Claims;
using System.Text.Encodings.Web;
using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Abstractions.Email;
using AnimalFitness.Application.Features.Auth;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnimalFitness.Infrastructure.Auth;

/// <summary>
/// Autenticación con Identity: crea el perfil de dominio en la misma transacción que la cuenta.
/// </summary>
public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    IRequestContext requestContext,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private const string NeutralAccountMessage =
        "Si la cuenta existe, recibirás instrucciones en el correo indicado.";

    /// <inheritdoc />
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email;
        string firstName;
        string lastName;
        string? phone;
        try
        {
            email = TextRules.NormalizeEmail(request.Email);
            firstName = TextRules.PersonName(request.FirstName, "El nombre");
            lastName = TextRules.PersonName(request.LastName, "El apellido");
            phone = TextRules.OptionalPhone(request.PhoneNumber);
        }
        catch (DomainException ex)
        {
            return Fail(ex.Message, 400);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 10)
        {
            return Fail("La contraseña debe tener al menos 10 caracteres.", 400);
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            // Respuesta neutra: no revelar si el correo ya está registrado.
            logger.LogInformation("Intento de registro con correo ya existente.");
            return new AuthResult.Success(Message: "Revisa tu correo para continuar con el registro.");
        }

        var userId = Guid.CreateVersion7();
        var account = new ApplicationUser(userId)
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            PhoneNumber = phone,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var createResult = await userManager.CreateAsync(account, request.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Fail(
                    "No se pudo crear la cuenta.",
                    400,
                    string.Join(" ", createResult.Errors.Select(error => error.Description)));
            }

            var profile = User.Create(
                email,
                firstName,
                lastName,
                timeProvider.GetUtcNow(),
                phoneNumber: phone,
                id: userId);
            dbContext.UserProfiles.Add(profile);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(account);
        await emailSender.SendAsync(
            email,
            "Confirma tu correo en AnimalFitness",
            $"<p>Hola {HtmlEncoder.Default.Encode(firstName)},</p>" +
            $"<p>Tu código de confirmación es:</p><p><code>{HtmlEncoder.Default.Encode(token)}</code></p>" +
            $"<p>UserId: {userId:D}</p>",
            cancellationToken);

        await signInManager.SignInAsync(account, isPersistent: false);
        return new AuthResult.Success(await BuildUserDtoAsync(account, cancellationToken), "Cuenta creada. Revisa tu correo para confirmarla.");
    }

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email;
        try
        {
            email = TextRules.NormalizeEmail(request.Email);
        }
        catch (DomainException)
        {
            return Fail("Correo o contraseña incorrectos.", 401);
        }

        var account = await userManager.FindByEmailAsync(email);
        if (account is null)
        {
            return Fail("Correo o contraseña incorrectos.", 401);
        }

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(user => user.Id == account.Id, cancellationToken);
        if (profile is null || !profile.IsActive || profile.IsDeleted)
        {
            return Fail("Correo o contraseña incorrectos.", 401);
        }

        var result = await signInManager.CheckPasswordSignInAsync(account, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Fail("La cuenta está bloqueada temporalmente por intentos fallidos.", 423);
        }

        if (!result.Succeeded)
        {
            return Fail("Correo o contraseña incorrectos.", 401);
        }

        await signInManager.SignInAsync(account, isPersistent: request.RememberMe);
        profile.RecordLogin(timeProvider.GetUtcNow(), profile.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult.Success(await BuildUserDtoAsync(account, cancellationToken));
    }

    /// <inheritdoc />
    public Task LogoutAsync(CancellationToken cancellationToken = default)
        => signInManager.SignOutAsync();

    /// <inheritdoc />
    public async Task<AuthResult> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var userId = requestContext.UserId;
        if (userId is null)
        {
            var principal = signInManager.Context.User;
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed))
            {
                return Fail("No autenticado.", 401);
            }

            userId = parsed;
        }

        var account = await userManager.FindByIdAsync(userId.Value.ToString());
        if (account is null)
        {
            return Fail("No autenticado.", 401);
        }

        return new AuthResult.Success(await BuildUserDtoAsync(account, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AuthResult> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Fail("El token es obligatorio.", 400);
        }

        var account = await userManager.FindByIdAsync(request.UserId.ToString());
        if (account is null)
        {
            return Fail("No se pudo confirmar el correo.", 400);
        }

        var result = await userManager.ConfirmEmailAsync(account, request.Token);
        if (!result.Succeeded)
        {
            return Fail("No se pudo confirmar el correo.", 400);
        }

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(user => user.Id == account.Id, cancellationToken);
        profile?.ConfirmEmail(timeProvider.GetUtcNow(), account.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult.Success(Message: "Correo confirmado.");
    }

    /// <inheritdoc />
    public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email;
        try
        {
            email = TextRules.NormalizeEmail(request.Email);
        }
        catch (DomainException)
        {
            return new AuthResult.Success(Message: NeutralAccountMessage);
        }

        var account = await userManager.FindByEmailAsync(email);
        if (account is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(account);
            await emailSender.SendAsync(
                email,
                "Restablece tu contraseña en AnimalFitness",
                $"<p>Usa este token para restablecer tu contraseña:</p><p><code>{HtmlEncoder.Default.Encode(token)}</code></p>",
                cancellationToken);
        }

        return new AuthResult.Success(Message: NeutralAccountMessage);
    }

    /// <inheritdoc />
    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email;
        try
        {
            email = TextRules.NormalizeEmail(request.Email);
        }
        catch (DomainException)
        {
            return Fail("No se pudo restablecer la contraseña.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Fail("El token y la nueva contraseña son obligatorios.", 400);
        }

        var account = await userManager.FindByEmailAsync(email);
        if (account is null)
        {
            return Fail("No se pudo restablecer la contraseña.", 400);
        }

        var result = await userManager.ResetPasswordAsync(account, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return Fail("No se pudo restablecer la contraseña.", 400);
        }

        await userManager.UpdateSecurityStampAsync(account);
        return new AuthResult.Success(Message: "Contraseña actualizada. Ya puedes iniciar sesión.");
    }

    private async Task<AuthUserDto> BuildUserDtoAsync(ApplicationUser account, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(user => user.Id == account.Id, cancellationToken);

        var isSuperAdmin = await userManager.IsInRoleAsync(account, PlatformRoles.SuperAdmin);

        var memberships = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(membership => membership.UserId == account.Id
                && !membership.IsDeleted
                && membership.Status == MembershipStatus.Active)
            .Join(
                dbContext.Tenants.AsNoTracking().Where(tenant => !tenant.IsDeleted),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => new MembershipSummaryDto(
                    tenant.Id,
                    tenant.Slug,
                    tenant.Name,
                    tenant.Status,
                    membership.Role,
                    membership.Status))
            .ToListAsync(cancellationToken);

        return new AuthUserDto(
            profile.Id,
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.PhoneNumber,
            profile.EmailConfirmed || account.EmailConfirmed,
            isSuperAdmin,
            memberships,
            requestContext.TenantId,
            requestContext.Role);
    }

    private static AuthResult.Failure Fail(string title, int statusCode, string? detail = null)
        => new(title, statusCode, detail);
}
