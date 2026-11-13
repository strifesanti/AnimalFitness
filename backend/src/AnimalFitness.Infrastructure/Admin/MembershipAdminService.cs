using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Admin;

/// <summary>Gestión de miembros de la organización activa.</summary>
public sealed class MembershipAdminService(
    AppDbContext db,
    AdminAccess access,
    TimeProvider timeProvider) : IMembershipAdminService
{
    /// <inheritdoc />
    public async Task<AppResult> ListMembersAsync(MembershipRole? role, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.MembersRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var tenantId = access.RequireTenantId();
        var query = db.TenantMemberships.AsNoTracking()
            .Where(membership => membership.Status != MembershipStatus.Removed);

        if (role is not null)
        {
            query = query.Where(membership => membership.Role == role);
        }

        var memberships = (await query.ToListAsync(cancellationToken))
            .OrderBy(membership => membership.JoinedAt)
            .ToList();
        var userIds = memberships.Select(membership => membership.UserId).Distinct().ToList();
        var profiles = await db.UserProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);

        var athleteProfiles = await db.AthleteProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);
        var trainerProfiles = await db.TrainerProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        var items = memberships.Select(membership =>
        {
            profiles.TryGetValue(membership.UserId, out var user);
            athleteProfiles.TryGetValue(membership.UserId, out var athlete);
            trainerProfiles.TryGetValue(membership.UserId, out var trainer);
            return new MemberListItemDto(
                membership.Id,
                membership.UserId,
                user?.Email ?? string.Empty,
                user?.FirstName ?? string.Empty,
                user?.LastName ?? string.Empty,
                membership.Role,
                membership.Status,
                membership.JoinedAt,
                athlete?.Id,
                trainer?.Id);
        }).ToList();

        _ = tenantId;
        return new AppResult.Success<IReadOnlyList<MemberListItemDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> ChangeRoleAsync(
        Guid membershipId,
        ChangeMemberRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.MembersManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var membership = await db.TenantMemberships.FirstOrDefaultAsync(item => item.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return AdminAccess.Fail("No se encontró la membresía.", 404);
        }

        if (membership.Role == MembershipRole.Owner && request.Role != MembershipRole.Owner)
        {
            var owners = await db.TenantMemberships.CountAsync(
                item => item.Role == MembershipRole.Owner && item.Status == MembershipStatus.Active,
                cancellationToken);
            if (owners <= 1)
            {
                return AdminAccess.Fail("Debe quedar al menos un administrador activo.", 400);
            }
        }

        try
        {
            membership.ChangeRole(request.Role, timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await EnsureRoleProfileAsync(membership, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Rol actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> SuspendAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.MembersManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var membership = await db.TenantMemberships.FirstOrDefaultAsync(item => item.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return AdminAccess.Fail("No se encontró la membresía.", 404);
        }

        if (membership.Role == MembershipRole.Owner)
        {
            var owners = await db.TenantMemberships.CountAsync(
                item => item.Role == MembershipRole.Owner && item.Status == MembershipStatus.Active,
                cancellationToken);
            if (owners <= 1)
            {
                return AdminAccess.Fail("No se puede suspender al único administrador.", 400);
            }
        }

        try
        {
            membership.Suspend(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Membresía suspendida.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ReactivateAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.MembersManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var membership = await db.TenantMemberships.FirstOrDefaultAsync(item => item.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return AdminAccess.Fail("No se encontró la membresía.", 404);
        }

        try
        {
            membership.Reactivate(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Membresía reactivada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> RemoveAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.MembersManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var membership = await db.TenantMemberships.FirstOrDefaultAsync(item => item.Id == membershipId, cancellationToken);
        if (membership is null)
        {
            return AdminAccess.Fail("No se encontró la membresía.", 404);
        }

        if (membership.Role == MembershipRole.Owner)
        {
            var owners = await db.TenantMemberships.CountAsync(
                item => item.Role == MembershipRole.Owner && item.Status == MembershipStatus.Active,
                cancellationToken);
            if (owners <= 1)
            {
                return AdminAccess.Fail("No se puede retirar al único administrador.", 400);
            }
        }

        try
        {
            membership.Remove(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Membresía retirada.");
    }

    private async Task EnsureRoleProfileAsync(TenantMembership membership, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var actor = access.RequireUserId();
        if (membership.Role == MembershipRole.Athlete)
        {
            var exists = await db.AthleteProfiles.AnyAsync(profile => profile.UserId == membership.UserId, cancellationToken);
            if (!exists)
            {
                db.AthleteProfiles.Add(AthleteProfile.Create(membership.TenantId, membership.UserId, now, actor));
            }
        }

        if (membership.Role == MembershipRole.Trainer || membership.Role == MembershipRole.Owner)
        {
            var exists = await db.TrainerProfiles.AnyAsync(profile => profile.UserId == membership.UserId, cancellationToken);
            if (!exists && membership.Role == MembershipRole.Trainer)
            {
                db.TrainerProfiles.Add(TrainerProfile.Create(membership.TenantId, membership.UserId, now, actor));
            }
        }
    }
}
