using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Domain;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Admin;

/// <summary>Organizaciones de plataforma y configuración de la academia activa.</summary>
public sealed class OrganizationService(
    AppDbContext db,
    IRequestContext requestContext,
    UserManager<ApplicationUser> userManager,
    AdminAccess access,
    TimeProvider timeProvider) : IOrganizationService
{
    /// <inheritdoc />
    public async Task<AppResult> ListPlatformTenantsAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TenantsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var items = await db.Tenants
            .AsNoTracking()
            .Where(tenant => !tenant.IsDeleted)
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new TenantListItemDto(
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.Status,
                tenant.ContactEmail,
                tenant.Phone,
                tenant.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AppResult.Success<IReadOnlyList<TenantListItemDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> GetPlatformTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TenantsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var detail = await LoadDetailAsync(tenantId, cancellationToken);
        return detail is null
            ? AdminAccess.Fail("No se encontró la organización.", 404)
            : new AppResult.Success<TenantDetailDto>(detail);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TenantsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var actor = access.RequireUserId();
        var now = timeProvider.GetUtcNow();

        Tenant tenant;
        string ownerEmail;
        try
        {
            tenant = Tenant.Create(
                request.Name,
                request.Slug,
                request.ContactEmail,
                now,
                actor,
                request.Phone,
                request.TimeZoneId,
                request.Currency);
            ownerEmail = TextRules.NormalizeEmail(request.OwnerEmail);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        if (await db.Tenants.AnyAsync(item => item.Slug == tenant.Slug && !item.IsDeleted, cancellationToken))
        {
            return AdminAccess.Fail("Ya existe una organización con ese identificador.", 409);
        }

        var ownerAccount = await userManager.FindByEmailAsync(ownerEmail);
        if (ownerAccount is null)
        {
            return AdminAccess.Fail(
                "El correo del propietario no tiene cuenta. Regístralo primero o invítalo después de crear la academia.",
                400);
        }

        db.Tenants.Add(tenant);
        db.OrganizationSettings.Add(TenantSettings.Create(tenant.Id, now, actor));
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, ownerAccount.Id, MembershipRole.Owner, now, actor));
        await db.SaveChangesAsync(cancellationToken);

        var detail = await LoadDetailAsync(tenant.Id, cancellationToken);
        return new AppResult.Success<TenantDetailDto>(detail!, "Organización creada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TenantsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var tenant = await db.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId && !item.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return AdminAccess.Fail("No se encontró la organización.", 404);
        }

        try
        {
            tenant.Activate(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Organización activada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TenantsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var tenant = await db.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId && !item.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return AdminAccess.Fail("No se encontró la organización.", 404);
        }

        try
        {
            tenant.Suspend(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Organización suspendida.");
    }

    /// <inheritdoc />
    public async Task<AppResult> GetCurrentOrganizationAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.OrganizationManage, cancellationToken);
        if (failure is not null)
        {
            // Lectura también para trainers/athletes vía MembersRead no; settings needs manage or we allow read with MembersRead
            // Owners manage; allow Owners only for now via OrganizationManage
            return failure;
        }

        if (requestContext.TenantId is not { } tenantId)
        {
            return AdminAccess.Fail("No hay organización activa.", 400);
        }

        var detail = await LoadDetailAsync(tenantId, cancellationToken);
        return detail is null
            ? AdminAccess.Fail("No se encontró la organización.", 404)
            : new AppResult.Success<TenantDetailDto>(detail);
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateCurrentOrganizationAsync(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.OrganizationManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var tenantId = access.RequireTenantId();
        var tenant = await db.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId && !item.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return AdminAccess.Fail("No se encontró la organización.", 404);
        }

        var actor = access.RequireUserId();
        var now = timeProvider.GetUtcNow();
        try
        {
            tenant.UpdateProfile(request.Name, request.ContactEmail, request.Phone, now, actor);
            tenant.ChangeTimeZone(request.TimeZoneId, now, actor);
            tenant.ChangeCurrency(request.Currency, now, actor);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        var detail = await LoadDetailAsync(tenantId, cancellationToken);
        return new AppResult.Success<TenantDetailDto>(detail!, "Organización actualizada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateCurrentSettingsAsync(
        UpdateOrganizationSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.OrganizationManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var tenantId = access.RequireTenantId();
        var settings = await db.OrganizationSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = TenantSettings.Create(tenantId, timeProvider.GetUtcNow(), access.RequireUserId());
            db.OrganizationSettings.Add(settings);
        }

        try
        {
            settings.Update(
                request.PrimaryColor,
                request.Locale,
                request.Currency,
                request.SettingsJson,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        var detail = await LoadDetailAsync(tenantId, cancellationToken);
        return new AppResult.Success<TenantDetailDto>(detail!, "Preferencias actualizadas.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListDirectoryAsync(CancellationToken cancellationToken = default)
    {
        if (requestContext.UserId is null)
        {
            return AdminAccess.Fail("Debes iniciar sesión.", 401);
        }

        var items = await db.Tenants
            .AsNoTracking()
            .Where(tenant => !tenant.IsDeleted && tenant.Status != TenantStatus.Suspended)
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new OrganizationDirectoryItemDto(tenant.Name, tenant.Slug, tenant.Status))
            .ToListAsync(cancellationToken);

        return new AppResult.Success<IReadOnlyList<OrganizationDirectoryItemDto>>(items);
    }

    private async Task<TenantDetailDto?> LoadDetailAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == tenantId && !item.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var settings = await db.OrganizationSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && !item.IsDeleted, cancellationToken);

        return new TenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status,
            tenant.ContactEmail,
            tenant.Phone,
            tenant.TimeZoneId,
            tenant.Currency,
            tenant.LogoFileId,
            settings?.PrimaryColor ?? DomainConstants.DefaultAccentColor,
            settings?.Locale ?? DomainConstants.DefaultCulture,
            settings?.SettingsJson ?? DomainConstants.EmptySettingsJson);
    }
}
