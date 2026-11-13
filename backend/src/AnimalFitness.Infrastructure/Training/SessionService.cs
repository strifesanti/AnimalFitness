using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Training;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Training;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Training;

/// <summary>Sesiones de entrenamiento y registro de series.</summary>
public sealed class SessionService(
    AppDbContext db,
    IRequestContext requestContext,
    AdminAccess access,
    TimeProvider timeProvider) : ISessionService
{
    private const int MaxListed = 200;

    /// <inheritdoc />
    public async Task<AppResult> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.SessionsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var sessions = db.WorkoutSessions.AsNoTracking().AsQueryable();
        if (IsAthlete)
        {
            var profile = await TrainingLookups.FindAthleteProfileByUserAsync(
                db,
                access.RequireUserId(),
                cancellationToken);
            if (profile is null)
            {
                return new AppResult.Success<IReadOnlyList<WorkoutSessionDto>>([]);
            }

            sessions = sessions.Where(session => session.AthleteProfileId == profile.Id);
        }

        // SQLite no ordena por DateTimeOffset: se materializa y se ordena en memoria.
        var items = (await sessions.ToListAsync(cancellationToken))
            .OrderByDescending(session => session.StartedAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<WorkoutSessionDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> StartAsync(StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.SessionsManageOwn, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var tenantId = access.RequireTenantId();
        var userId = access.RequireUserId();
        var now = timeProvider.GetUtcNow();

        Guid athleteProfileId;
        if (IsAthlete)
        {
            var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, userId, cancellationToken);
            if (own is null)
            {
                own = AthleteProfile.Create(tenantId, userId, now, userId);
                db.AthleteProfiles.Add(own);
            }

            if (request.AthleteProfileId is { } requested && requested != own.Id)
            {
                return AdminAccess.Fail("Solo puedes entrenar con tu propio perfil.", 403);
            }

            athleteProfileId = own.Id;
        }
        else
        {
            if (request.AthleteProfileId is not { } requested)
            {
                return AdminAccess.Fail("Indica el alumno de la sesión.", 400);
            }

            if (!await db.AthleteProfiles.AnyAsync(profile => profile.Id == requested, cancellationToken))
            {
                return AdminAccess.Fail("No se encontró el alumno.", 404);
            }

            athleteProfileId = requested;
        }

        if (request.DayId is not null && request.PlanId is null)
        {
            return AdminAccess.Fail("Indica el plan del día.", 400);
        }

        if (request.PlanId is { } planId)
        {
            var plan = await db.WorkoutPlans.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
            if (plan is null)
            {
                return AdminAccess.Fail("No se encontró el plan.", 404);
            }

            if (IsAthlete)
            {
                var assigned = plan.Status == WorkoutPlanStatus.Published
                    && await db.WorkoutPlanAssignments.AnyAsync(
                        item => item.PlanId == planId
                            && item.AthleteProfileId == athleteProfileId
                            && item.Status == PlanAssignmentStatus.Active,
                        cancellationToken);
                if (!assigned)
                {
                    return AdminAccess.Fail("No se encontró el plan.", 404);
                }
            }

            if (request.DayId is { } dayId
                && !await db.WorkoutDays.AnyAsync(day => day.Id == dayId && day.PlanId == planId, cancellationToken))
            {
                return AdminAccess.Fail("El día no pertenece al plan.", 400);
            }
        }

        WorkoutSession session;
        try
        {
            session = WorkoutSession.Start(
                tenantId,
                athleteProfileId,
                now,
                userId,
                request.PlanId,
                request.DayId,
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutSessionDto>(ToDto(session), "Sesión iniciada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> CompleteAsync(
        Guid sessionId,
        CompleteSessionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var (session, failure) = await LoadSessionAsync(sessionId, Permissions.SessionsManageOwn, track: true, cancellationToken);
        if (failure is not null || session is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró la sesión.", 404);
        }

        try
        {
            session.Complete(timeProvider.GetUtcNow(), access.RequireUserId(), request?.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutSessionDto>(ToDto(session), "Sesión completada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> CancelAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var (session, failure) = await LoadSessionAsync(sessionId, Permissions.SessionsManageOwn, track: true, cancellationToken);
        if (failure is not null || session is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró la sesión.", 404);
        }

        try
        {
            session.Cancel(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutSessionDto>(ToDto(session), "Sesión cancelada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> LogSetAsync(
        Guid sessionId,
        LogSetRequest request,
        CancellationToken cancellationToken = default)
    {
        var (session, failure) = await LoadSessionAsync(sessionId, Permissions.SessionsManageOwn, track: false, cancellationToken);
        if (failure is not null || session is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró la sesión.", 404);
        }

        ArgumentNullException.ThrowIfNull(request);
        if (session.Status != WorkoutSessionStatus.InProgress)
        {
            return AdminAccess.Fail("La sesión ya no está en curso.", 409);
        }

        if (!await db.Exercises.AnyAsync(item => item.Id == request.ExerciseId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró el ejercicio.", 404);
        }

        if (request.WorkoutExerciseId is { } workoutExerciseId)
        {
            var prescription = await db.WorkoutExercises.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == workoutExerciseId, cancellationToken);
            if (prescription is null || prescription.ExerciseId != request.ExerciseId)
            {
                return AdminAccess.Fail("El ejercicio del plan no coincide con el ejercicio registrado.", 400);
            }

            if (session.PlanId is { } planId
                && !await db.WorkoutDays.AnyAsync(
                    day => day.Id == prescription.DayId && day.PlanId == planId,
                    cancellationToken))
            {
                return AdminAccess.Fail("El ejercicio no pertenece al plan de la sesión.", 400);
            }
        }

        ExerciseLog log;
        try
        {
            log = ExerciseLog.Create(
                access.RequireTenantId(),
                session.Id,
                request.ExerciseId,
                request.SetNumber,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.WorkoutExerciseId,
                request.Reps,
                request.WeightKg,
                request.DurationSeconds,
                request.DistanceMeters,
                request.Rpe,
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.ExerciseLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ExerciseLogDto>(ToDto(log), "Serie registrada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListLogsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var (session, failure) = await LoadSessionAsync(sessionId, Permissions.SessionsRead, track: false, cancellationToken);
        if (failure is not null || session is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró la sesión.", 404);
        }

        var items = (await db.ExerciseLogs.AsNoTracking()
                .Where(log => log.SessionId == sessionId)
                .ToListAsync(cancellationToken))
            .OrderBy(log => log.LoggedAt)
            .ThenBy(log => log.SetNumber)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<ExerciseLogDto>>(items);
    }

    private bool IsAthlete => requestContext.Role == MembershipRole.Athlete;

    private async Task<(WorkoutSession? Session, AppResult? Failure)> LoadSessionAsync(
        Guid sessionId,
        string permission,
        bool track,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await access.RequirePermissionAsync(permission, cancellationToken);
        if (failure is not null)
        {
            return (null, failure);
        }

        var query = track ? db.WorkoutSessions.AsQueryable() : db.WorkoutSessions.AsNoTracking();
        var session = await query.FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return (null, AdminAccess.Fail("No se encontró la sesión.", 404));
        }

        if (IsAthlete)
        {
            var own = await TrainingLookups.FindAthleteProfileByUserAsync(
                db,
                access.RequireUserId(),
                cancellationToken);
            if (own is null || own.Id != session.AthleteProfileId)
            {
                return (null, AdminAccess.Fail("No se encontró la sesión.", 404));
            }
        }

        return (session, null);
    }

    private static WorkoutSessionDto ToDto(WorkoutSession session)
        => new(
            session.Id,
            session.AthleteProfileId,
            session.PlanId,
            session.DayId,
            session.Status,
            session.StartedAt,
            session.CompletedAt,
            session.Notes);

    private static ExerciseLogDto ToDto(ExerciseLog log)
        => new(
            log.Id,
            log.SessionId,
            log.WorkoutExerciseId,
            log.ExerciseId,
            log.SetNumber,
            log.Reps,
            log.WeightKg,
            log.DurationSeconds,
            log.DistanceMeters,
            log.Rpe,
            log.Notes,
            log.LoggedAt);
}
