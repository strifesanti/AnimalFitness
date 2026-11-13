using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace AnimalFitness.Infrastructure.Admin;

/// <summary>
/// Comprobaciones de permiso reutilizadas por los servicios de administración.
/// </summary>
public sealed class AdminAccess(
    IRequestContext requestContext,
    UserManager<ApplicationUser> userManager)
{
    /// <summary>Exige un permiso concreto según el rol o SuperAdmin.</summary>
    public async Task<(bool IsSuperAdmin, AppResult? Failure)> RequirePermissionAsync(
        string permission,
        CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { } userId)
        {
            return (false, Fail("Debes iniciar sesión.", 401));
        }

        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
        {
            return (false, Fail("Debes iniciar sesión.", 401));
        }

        var isSuperAdmin = await userManager.IsInRoleAsync(account, PlatformRoles.SuperAdmin);
        if (!PermissionMap.Has(permission, requestContext.Role, isSuperAdmin))
        {
            return (isSuperAdmin, Fail("No tienes permiso para esta acción.", 403));
        }

        return (isSuperAdmin, null);
    }

    /// <summary>Exige el rol global SuperAdmin.</summary>
    public async Task<(bool IsSuperAdmin, AppResult? Failure)> RequireSuperAdminAsync(
        CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { } userId)
        {
            return (false, Fail("Debes iniciar sesión.", 401));
        }

        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
        {
            return (false, Fail("Debes iniciar sesión.", 401));
        }

        var isSuperAdmin = await userManager.IsInRoleAsync(account, PlatformRoles.SuperAdmin);
        if (!isSuperAdmin)
        {
            return (false, Fail("Solo el SuperAdmin puede realizar esta acción.", 403));
        }

        return (true, null);
    }

    /// <summary>Devuelve el usuario del contexto o falla.</summary>
    public Guid RequireUserId()
        => requestContext.UserId ?? throw new InvalidOperationException("No hay usuario en el contexto.");

    /// <summary>Devuelve la organización del contexto o falla.</summary>
    public Guid RequireTenantId()
        => requestContext.TenantId ?? throw new InvalidOperationException("No hay organización en el contexto.");

    /// <summary>Crea un fallo controlado.</summary>
    public static AppResult Fail(string title, int status, string? detail = null)
        => new AppResult.Failure(title, status, detail);
}
