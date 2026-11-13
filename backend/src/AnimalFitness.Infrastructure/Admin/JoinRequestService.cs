using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Admin;

/// <summary>Solicitudes de ingreso a organizaciones.</summary>
public sealed class JoinRequestService(
    AppDbContext db,
    IRequestContext requestContext,
    AdminAccess access,
    TimeProvider timeProvider) : IJoinRequestService
{
    /// <inheritdoc />
    public async Task<AppResult> ListAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.JoinRequestsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var requests = (await db.JoinRequests.AsNoTracking().ToListAsync(cancellationToken))
            .OrderByDescending(request => request.CreatedAt)
            .ToList();

        var userIds = requests.Select(request => request.UserId).Distinct().ToList();
        var users = await db.UserProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);

        var items = requests.Select(request =>
        {
            users.TryGetValue(request.UserId, out var user);
            return new JoinRequestListItemDto(
                request.Id,
                request.UserId,
                user?.Email ?? string.Empty,
                user?.FirstName ?? string.Empty,
                user?.LastName ?? string.Empty,
                request.Status.ToString(),
                request.CreatedAt,
                request.RejectionReason);
        }).ToList();

        return new AppResult.Success<IReadOnlyList<JoinRequestListItemDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> SubmitAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (requestContext.UserId is null)
        {
            return AdminAccess.Fail("Debes iniciar sesión.", 401);
        }

        var userId = access.RequireUserId();
        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Slug == normalized && !item.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return AdminAccess.Fail("No se encontró la organización.", 404);
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            return AdminAccess.Fail("La organización está suspendida.", 403);
        }

        var alreadyMember = await db.TenantMemberships.IgnoreQueryFilters().AnyAsync(
            item => item.TenantId == tenant.Id
                && item.UserId == userId
                && !item.IsDeleted
                && item.Status != MembershipStatus.Removed,
            cancellationToken);
        if (alreadyMember)
        {
            return AdminAccess.Fail("Ya perteneces a esta organización.", 409);
        }

        var pending = await db.JoinRequests.IgnoreQueryFilters().AnyAsync(
            item => item.TenantId == tenant.Id
                && item.UserId == userId
                && !item.IsDeleted
                && item.Status == JoinRequestStatus.Pending,
            cancellationToken);
        if (pending)
        {
            return AdminAccess.Fail("Ya tienes una solicitud pendiente.", 409);
        }

        var now = timeProvider.GetUtcNow();
        var joinRequest = JoinRequest.Create(tenant.Id, userId, now);
        db.JoinRequests.Add(joinRequest);
        await db.SaveChangesAsync(cancellationToken);

        return new AppResult.Success<object>(
            new { id = joinRequest.Id, slug = tenant.Slug, status = joinRequest.Status.ToString() },
            "Solicitud enviada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ApproveAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.JoinRequestsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var joinRequest = await db.JoinRequests.FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (joinRequest is null)
        {
            return AdminAccess.Fail("No se encontró la solicitud.", 404);
        }

        var actor = access.RequireUserId();
        var now = timeProvider.GetUtcNow();
        try
        {
            joinRequest.Approve(actor, now);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        var existing = await db.TenantMemberships.IgnoreQueryFilters().FirstOrDefaultAsync(
            item => item.TenantId == joinRequest.TenantId
                && item.UserId == joinRequest.UserId
                && !item.IsDeleted
                && item.Status != MembershipStatus.Removed,
            cancellationToken);
        if (existing is null)
        {
            db.TenantMemberships.Add(
                TenantMembership.Create(joinRequest.TenantId, joinRequest.UserId, MembershipRole.Athlete, now, actor));
        }

        var hasAthlete = await db.AthleteProfiles.AnyAsync(
            item => item.UserId == joinRequest.UserId,
            cancellationToken);
        if (!hasAthlete)
        {
            db.AthleteProfiles.Add(AthleteProfile.Create(joinRequest.TenantId, joinRequest.UserId, now, actor));
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Solicitud aprobada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> RejectAsync(
        Guid requestId,
        RejectJoinRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.JoinRequestsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var joinRequest = await db.JoinRequests.FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (joinRequest is null)
        {
            return AdminAccess.Fail("No se encontró la solicitud.", 404);
        }

        try
        {
            joinRequest.Reject(access.RequireUserId(), request.Reason, timeProvider.GetUtcNow());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Solicitud rechazada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> CancelAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        if (requestContext.UserId is null)
        {
            return AdminAccess.Fail("Debes iniciar sesión.", 401);
        }

        var userId = access.RequireUserId();
        var joinRequest = await db.JoinRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == requestId && !item.IsDeleted, cancellationToken);
        if (joinRequest is null)
        {
            return AdminAccess.Fail("No se encontró la solicitud.", 404);
        }

        if (joinRequest.UserId != userId)
        {
            return AdminAccess.Fail("Solo puedes cancelar tu propia solicitud.", 403);
        }

        try
        {
            joinRequest.Cancel(timeProvider.GetUtcNow(), userId);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Solicitud cancelada.");
    }
}
