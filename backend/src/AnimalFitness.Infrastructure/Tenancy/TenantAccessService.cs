using AnimalFitness.Application.Abstractions.Tenancy;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Tenancy;

/// <summary>
/// Valida el acceso de una persona a una organización.
/// Consulta membresías sin el filtro de organización porque el contexto todavía no está establecido.
/// </summary>
public sealed class TenantAccessService(AppDbContext dbContext) : ITenantAccessService
{
    /// <inheritdoc />
    public async Task<TenantAccessOutcome> ResolveAsync(
        string slug,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("La persona no puede ser vacía.", nameof(userId));
        }

        // Solo se normaliza para la búsqueda. La validación fuerte del slug ocurre al crearlo.
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length is < 3 or > 63)
        {
            return new TenantAccessOutcome.NotFound();
        }

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Slug == normalizedSlug && !item.IsDeleted,
                cancellationToken);

        if (tenant is null)
        {
            return new TenantAccessOutcome.NotFound();
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            return new TenantAccessOutcome.Suspended();
        }

        // Sin organización activa el filtro global no devolvería filas: aquí hay que saltarlo a propósito.
        var membership = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.TenantId == tenant.Id
                    && item.UserId == userId
                    && !item.IsDeleted
                    && item.Status == MembershipStatus.Active,
                cancellationToken);

        if (membership is null)
        {
            return new TenantAccessOutcome.Forbidden("No tienes una membresía activa en esta organización.");
        }

        return new TenantAccessOutcome.Granted(new TenantAccess(
            tenant.Id,
            tenant.Slug,
            tenant.Status,
            userId,
            membership.Role,
            membership.Status));
    }
}
