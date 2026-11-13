using System.Text.Encodings.Web;
using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Abstractions.Email;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Admin;

/// <summary>Invitaciones por correo con token hasheado.</summary>
public sealed class InvitationService(
    AppDbContext db,
    IRequestContext requestContext,
    AdminAccess access,
    IEmailSender emailSender,
    TimeProvider timeProvider) : IInvitationService
{
    /// <inheritdoc />
    public async Task<AppResult> ListAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.InvitationsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var items = (await db.Invitations.AsNoTracking().ToListAsync(cancellationToken))
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new InvitationListItemDto(
                invitation.Id,
                invitation.Email,
                invitation.Role,
                invitation.Status.ToString(),
                invitation.ExpiresAt,
                invitation.CreatedAt))
            .ToList();

        return new AppResult.Success<IReadOnlyList<InvitationListItemDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateAsync(CreateInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.InvitationsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        if (request.Role == MembershipRole.Owner)
        {
            return AdminAccess.Fail("No se puede invitar como administrador por este flujo.", 400);
        }

        var tenantId = access.RequireTenantId();
        var actor = access.RequireUserId();
        var now = timeProvider.GetUtcNow();
        var (plain, hash) = InvitationToken.Create();

        Invitation invitation;
        try
        {
            invitation = Invitation.Create(
                tenantId,
                request.Email,
                request.Role,
                hash,
                now.AddDays(7),
                now,
                actor);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        var pendingExists = await db.Invitations.AnyAsync(
            item => item.Email == invitation.Email && item.Status == InvitationStatus.Pending,
            cancellationToken);
        if (pendingExists)
        {
            return AdminAccess.Fail("Ya hay una invitación pendiente para ese correo.", 409);
        }

        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        var tenant = await db.Tenants.AsNoTracking().FirstAsync(item => item.Id == tenantId, cancellationToken);
        await emailSender.SendAsync(
            invitation.Email,
            $"Invitación a {tenant.Name}",
            $"<p>Te invitaron a unirte a <strong>{HtmlEncoder.Default.Encode(tenant.Name)}</strong> como {invitation.Role}.</p>" +
            $"<p>Token de un solo uso (válido 7 días):</p><p><code>{HtmlEncoder.Default.Encode(plain)}</code></p>" +
            "<p>Inicia sesión y acepta la invitación en /invitations/accept</p>",
            cancellationToken);

        return new AppResult.Success<InvitationListItemDto>(
            new InvitationListItemDto(
                invitation.Id,
                invitation.Email,
                invitation.Role,
                invitation.Status.ToString(),
                invitation.ExpiresAt,
                invitation.CreatedAt),
            "Invitación creada. El enlace se envió por correo (provisional: ver log).");
    }

    /// <inheritdoc />
    public async Task<AppResult> CancelAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.InvitationsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.Id == invitationId, cancellationToken);
        if (invitation is null)
        {
            return AdminAccess.Fail("No se encontró la invitación.", 404);
        }

        try
        {
            invitation.Cancel(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Invitación cancelada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> AcceptAsync(AcceptInvitationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (requestContext.UserId is null)
        {
            return AdminAccess.Fail("Debes iniciar sesión.", 401);
        }

        var userId = access.RequireUserId();
        var now = timeProvider.GetUtcNow();

        string hash;
        try
        {
            hash = InvitationToken.Hash(request.Token);
        }
        catch (Exception)
        {
            return AdminAccess.Fail("Token de invitación inválido.", 400);
        }

        var invitation = await db.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                item => item.TokenHash == hash && !item.IsDeleted,
                cancellationToken);

        if (invitation is null)
        {
            return AdminAccess.Fail("No se encontró la invitación.", 404);
        }

        var profile = await db.UserProfiles.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (profile is null)
        {
            return AdminAccess.Fail("No se encontró el perfil del usuario.", 400);
        }

        if (!string.Equals(profile.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return AdminAccess.Fail("Esta invitación corresponde a otro correo.", 403);
        }

        var existing = await db.TenantMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                item => item.TenantId == invitation.TenantId
                    && item.UserId == userId
                    && !item.IsDeleted
                    && item.Status != MembershipStatus.Removed,
                cancellationToken);
        if (existing is not null)
        {
            return AdminAccess.Fail("Ya perteneces a esta organización.", 409);
        }

        try
        {
            if (now >= invitation.ExpiresAt && invitation.Status == InvitationStatus.Pending)
            {
                invitation.Expire(now, userId);
                await db.SaveChangesAsync(cancellationToken);
                return AdminAccess.Fail("La invitación está vencida.", 400);
            }

            invitation.Accept(userId, now, userId);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        var membership = TenantMembership.Create(invitation.TenantId, userId, invitation.Role, now, userId);
        db.TenantMemberships.Add(membership);

        if (invitation.Role == MembershipRole.Athlete)
        {
            var hasAthlete = await db.AthleteProfiles.IgnoreQueryFilters().AnyAsync(
                item => item.TenantId == invitation.TenantId && item.UserId == userId && !item.IsDeleted,
                cancellationToken);
            if (!hasAthlete)
            {
                db.AthleteProfiles.Add(AthleteProfile.Create(invitation.TenantId, userId, now, userId));
            }
        }

        if (invitation.Role == MembershipRole.Trainer)
        {
            var hasTrainer = await db.TrainerProfiles.IgnoreQueryFilters().AnyAsync(
                item => item.TenantId == invitation.TenantId && item.UserId == userId && !item.IsDeleted,
                cancellationToken);
            if (!hasTrainer)
            {
                db.TrainerProfiles.Add(TrainerProfile.Create(invitation.TenantId, userId, now, userId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var tenant = await db.Tenants.AsNoTracking().FirstAsync(item => item.Id == invitation.TenantId, cancellationToken);
        return new AppResult.Success<object>(
            new { tenantId = tenant.Id, slug = tenant.Slug, role = invitation.Role.ToString() },
            "Invitación aceptada.");
    }
}
