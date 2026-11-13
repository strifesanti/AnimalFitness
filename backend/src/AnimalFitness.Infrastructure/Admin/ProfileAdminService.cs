using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Profiles;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Admin;

/// <summary>Perfiles globales y de academia.</summary>
public sealed class ProfileAdminService(
    AppDbContext db,
    IRequestContext requestContext,
    UserManager<ApplicationUser> userManager,
    AdminAccess access,
    TimeProvider timeProvider) : IProfileAdminService
{
    /// <inheritdoc />
    public async Task<AppResult> UpdateMyProfileAsync(
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (requestContext.UserId is null)
        {
            return AdminAccess.Fail("Debes iniciar sesión.", 401);
        }

        ArgumentNullException.ThrowIfNull(request);
        var userId = access.RequireUserId();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var account = await userManager.FindByIdAsync(userId.ToString());
        if (profile is null || account is null)
        {
            return AdminAccess.Fail("No se encontró el perfil.", 404);
        }

        try
        {
            profile.Rename(request.FirstName, request.LastName, timeProvider.GetUtcNow(), userId);
            profile.ChangePhone(request.PhoneNumber, timeProvider.GetUtcNow(), userId);
            account.PhoneNumber = profile.PhoneNumber;
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Perfil actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListAthletesAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        await EnsureProfilesForRoleAsync(MembershipRole.Athlete, cancellationToken);

        var athletes = await db.AthleteProfiles.AsNoTracking().ToListAsync(cancellationToken);
        if (requestContext.Role == MembershipRole.Athlete)
        {
            athletes = athletes.Where(profile => profile.UserId == requestContext.UserId).ToList();
        }
        else if (requestContext.Role == MembershipRole.Trainer)
        {
            var trainerProfile = await db.TrainerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(profile => profile.UserId == requestContext.UserId, cancellationToken);
            if (trainerProfile is null)
            {
                return new AppResult.Success<IReadOnlyList<AthleteProfileDto>>([]);
            }

            var assignedIds = await db.TrainerAthletes.AsNoTracking()
                .Where(assignment =>
                    assignment.TrainerProfileId == trainerProfile.Id
                    && assignment.Status == TrainerAthleteAssignmentStatus.Active)
                .Select(assignment => assignment.AthleteProfileId)
                .ToListAsync(cancellationToken);
            athletes = athletes.Where(profile => assignedIds.Contains(profile.Id)).ToList();
        }

        var userIds = athletes.Select(profile => profile.UserId).ToList();
        var users = await db.UserProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);
        var assignments = await db.TrainerAthletes.AsNoTracking()
            .Where(assignment =>
                assignment.Status == TrainerAthleteAssignmentStatus.Active
                && athletes.Select(athlete => athlete.Id).Contains(assignment.AthleteProfileId))
            .ToDictionaryAsync(assignment => assignment.AthleteProfileId, cancellationToken);

        var items = athletes.Select(athlete =>
        {
            users.TryGetValue(athlete.UserId, out var user);
            assignments.TryGetValue(athlete.Id, out var assignment);
            return new AthleteProfileDto(
                athlete.Id,
                athlete.UserId,
                user?.Email ?? string.Empty,
                user?.FirstName ?? string.Empty,
                user?.LastName ?? string.Empty,
                athlete.HeightCm,
                athlete.WeightKg,
                athlete.BirthDate,
                athlete.Notes,
                assignment?.TrainerProfileId);
        }).ToList();

        return new AppResult.Success<IReadOnlyList<AthleteProfileDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateAthleteAsync(
        Guid athleteProfileId,
        UpdateAthleteProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var athlete = await db.AthleteProfiles.FirstOrDefaultAsync(item => item.Id == athleteProfileId, cancellationToken);
        if (athlete is null)
        {
            return AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        try
        {
            athlete.Update(
                request.HeightCm,
                request.WeightKg,
                request.BirthDate,
                request.Notes,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Perfil de alumno actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListTrainersAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TrainersRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        await EnsureProfilesForRoleAsync(MembershipRole.Trainer, cancellationToken);

        var trainers = await db.TrainerProfiles.AsNoTracking().ToListAsync(cancellationToken);
        var userIds = trainers.Select(profile => profile.UserId).ToList();
        var users = await db.UserProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);
        var counts = await db.TrainerAthletes.AsNoTracking()
            .Where(assignment => assignment.Status == TrainerAthleteAssignmentStatus.Active)
            .GroupBy(assignment => assignment.TrainerProfileId)
            .Select(group => new { TrainerProfileId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TrainerProfileId, item => item.Count, cancellationToken);

        var items = trainers.Select(trainer =>
        {
            users.TryGetValue(trainer.UserId, out var user);
            counts.TryGetValue(trainer.Id, out var count);
            return new TrainerProfileDto(
                trainer.Id,
                trainer.UserId,
                user?.Email ?? string.Empty,
                user?.FirstName ?? string.Empty,
                user?.LastName ?? string.Empty,
                trainer.Bio,
                trainer.Specialties,
                count);
        }).ToList();

        return new AppResult.Success<IReadOnlyList<TrainerProfileDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateTrainerAsync(
        Guid trainerProfileId,
        UpdateTrainerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.TrainersManage, cancellationToken);
        if (failure is not null)
        {
            // Trainers can update own profile
            var trainer = await db.TrainerProfiles.FirstOrDefaultAsync(item => item.Id == trainerProfileId, cancellationToken);
            if (trainer is null || trainer.UserId != requestContext.UserId)
            {
                return failure;
            }

            return await UpdateTrainerCoreAsync(trainer, request, cancellationToken);
        }

        ArgumentNullException.ThrowIfNull(request);
        var profile = await db.TrainerProfiles.FirstOrDefaultAsync(item => item.Id == trainerProfileId, cancellationToken);
        if (profile is null)
        {
            return AdminAccess.Fail("No se encontró el entrenador.", 404);
        }

        return await UpdateTrainerCoreAsync(profile, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AppResult> AssignAthleteAsync(AssignAthleteRequest request, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var tenantId = access.RequireTenantId();
        var trainer = await db.TrainerProfiles.FirstOrDefaultAsync(item => item.Id == request.TrainerProfileId, cancellationToken);
        var athlete = await db.AthleteProfiles.FirstOrDefaultAsync(item => item.Id == request.AthleteProfileId, cancellationToken);
        if (trainer is null || athlete is null)
        {
            return AdminAccess.Fail("Entrenador o alumno no encontrado.", 404);
        }

        var active = await db.TrainerAthletes.AnyAsync(
            item => item.AthleteProfileId == athlete.Id && item.Status == TrainerAthleteAssignmentStatus.Active,
            cancellationToken);
        if (active)
        {
            return AdminAccess.Fail("El alumno ya tiene un entrenador asignado.", 409);
        }

        try
        {
            db.TrainerAthletes.Add(
                TrainerAthlete.Create(tenantId, trainer.Id, athlete.Id, timeProvider.GetUtcNow(), access.RequireUserId()));
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Alumno asignado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UnassignAthleteAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var assignment = await db.TrainerAthletes.FirstOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (assignment is null)
        {
            return AdminAccess.Fail("No se encontró la asignación.", 404);
        }

        try
        {
            assignment.End(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Asignación finalizada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListGoalsAsync(Guid athleteProfileId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var items = (await db.AthleteGoals.AsNoTracking()
            .Where(goal => goal.AthleteProfileId == athleteProfileId)
            .ToListAsync(cancellationToken))
            .OrderByDescending(goal => goal.CreatedAt)
            .Select(goal => new AthleteGoalDto(
                goal.Id,
                goal.AthleteProfileId,
                goal.Title,
                goal.Description,
                goal.TargetValue,
                goal.Unit,
                goal.DueDate,
                goal.Status))
            .ToList();

        return new AppResult.Success<IReadOnlyList<AthleteGoalDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateGoalAsync(CreateAthleteGoalRequest request, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var athlete = await db.AthleteProfiles.FirstOrDefaultAsync(item => item.Id == request.AthleteProfileId, cancellationToken);
        if (athlete is null)
        {
            return AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        AthleteGoal goal;
        try
        {
            goal = AthleteGoal.Create(
                access.RequireTenantId(),
                athlete.Id,
                request.Title,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.Description,
                request.TargetValue,
                request.Unit,
                request.DueDate);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.AthleteGoals.Add(goal);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<AthleteGoalDto>(
            new AthleteGoalDto(
                goal.Id,
                goal.AthleteProfileId,
                goal.Title,
                goal.Description,
                goal.TargetValue,
                goal.Unit,
                goal.DueDate,
                goal.Status),
            "Objetivo creado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> CompleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var goal = await db.AthleteGoals.FirstOrDefaultAsync(item => item.Id == goalId, cancellationToken);
        if (goal is null)
        {
            return AdminAccess.Fail("No se encontró el objetivo.", 404);
        }

        try
        {
            goal.Complete(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Objetivo completado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> CancelGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.AthletesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var goal = await db.AthleteGoals.FirstOrDefaultAsync(item => item.Id == goalId, cancellationToken);
        if (goal is null)
        {
            return AdminAccess.Fail("No se encontró el objetivo.", 404);
        }

        try
        {
            goal.Cancel(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Objetivo cancelado.");
    }

    private async Task<AppResult> UpdateTrainerCoreAsync(
        TrainerProfile profile,
        UpdateTrainerProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            profile.Update(request.Bio, request.Specialties, timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Perfil de entrenador actualizado.");
    }

    private async Task EnsureProfilesForRoleAsync(MembershipRole role, CancellationToken cancellationToken)
    {
        var tenantId = access.RequireTenantId();
        var now = timeProvider.GetUtcNow();
        var actor = access.RequireUserId();
        var members = await db.TenantMemberships.AsNoTracking()
            .Where(membership => membership.Role == role && membership.Status == MembershipStatus.Active)
            .Select(membership => membership.UserId)
            .ToListAsync(cancellationToken);

        if (role == MembershipRole.Athlete)
        {
            var existing = await db.AthleteProfiles.AsNoTracking()
                .Select(profile => profile.UserId)
                .ToListAsync(cancellationToken);
            foreach (var userId in members.Except(existing))
            {
                db.AthleteProfiles.Add(AthleteProfile.Create(tenantId, userId, now, actor));
            }
        }
        else if (role == MembershipRole.Trainer)
        {
            var existing = await db.TrainerProfiles.AsNoTracking()
                .Select(profile => profile.UserId)
                .ToListAsync(cancellationToken);
            foreach (var userId in members.Except(existing))
            {
                db.TrainerProfiles.Add(TrainerProfile.Create(tenantId, userId, now, actor));
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
