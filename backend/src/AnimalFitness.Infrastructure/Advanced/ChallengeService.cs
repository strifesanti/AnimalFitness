using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Challenges;
using AnimalFitness.Application.Features.Communication;
using AnimalFitness.Domain.Challenges;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Communication;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Training;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Advanced;

/// <summary>Retos, participación, puntos, clasificación y logros (Fase 5).</summary>
public sealed class ChallengeService(
    AppDbContext db,
    AdminAccess access,
    AthleteResolver athletes,
    INotificationService notifications,
    TimeProvider timeProvider) : IChallengeService
{
    private const int MaxListed = 500;
    private const int MaxPointsPerActivity = 10_000;
    private const int MaxTitleLength = 120;

    // ------------------------------------------------------------------
    // Retos
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListAsync(ChallengeStatus? status, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChallengesRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var challenges = (await db.Challenges.AsNoTracking().ToListAsync(cancellationToken))
            .Where(item => status is null || item.Status == status)
            .Where(item => !athletes.IsAthlete || item.Status != ChallengeStatus.Draft)
            .OrderByDescending(item => item.StartsAt)
            .Take(MaxListed)
            .ToList();

        var dtos = await ProjectAsync(challenges, cancellationToken);
        return new AppResult.Success<IReadOnlyList<ChallengeDto>>(dtos);
    }

    /// <inheritdoc />
    public async Task<AppResult> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var (challenge, failure) = await FindVisibleAsync(id, Permissions.ChallengesRead, tracking: false, cancellationToken);
        if (failure is not null || challenge is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró el reto.", 404);
        }

        return new AppResult.Success<ChallengeDto>((await ProjectAsync([challenge], cancellationToken))[0]);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateAsync(CreateChallengeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ChallengesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (request.TargetValue is <= 0)
        {
            return AdminAccess.Fail("La meta debe ser positiva.", 400);
        }

        Challenge challenge;
        try
        {
            challenge = Challenge.Create(
                access.RequireTenantId(),
                request.Name,
                request.Kind,
                request.Metric,
                request.StartsAt,
                request.EndsAt,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.Description,
                request.TargetValue);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.Challenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ChallengeDto>((await ProjectAsync([challenge], cancellationToken))[0], "Reto creado.");
    }

    /// <inheritdoc />
    public Task<AppResult> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, (challenge, now, actor) => challenge.Activate(now, actor), "Reto activado.", cancellationToken);

    /// <inheritdoc />
    public async Task<AppResult> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var (_, permissionFailure) = await access.RequirePermissionAsync(Permissions.ChallengesManage, cancellationToken);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        var challenge = await db.Challenges.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null)
        {
            return AdminAccess.Fail("No se encontró el reto.", 404);
        }

        var now = timeProvider.GetUtcNow();
        var actor = access.RequireUserId();
        try
        {
            challenge.Complete(now, actor);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        // Quien lidera la clasificación (con puntos) recibe un logro; los empates comparten el reconocimiento.
        var participants = await db.ChallengeParticipants.AsNoTracking()
            .Where(item => item.ChallengeId == id)
            .ToListAsync(cancellationToken);
        var topPoints = participants.Count == 0 ? 0 : participants.Max(item => item.Points);
        var winners = topPoints > 0
            ? participants.Where(item => item.Points == topPoints).ToList()
            : new List<ChallengeParticipant>();
        foreach (var winner in winners)
        {
            db.Achievements.Add(Achievement.Create(
                access.RequireTenantId(),
                winner.AthleteProfileId,
                Truncate($"Ganador: {challenge.Name}", MaxTitleLength),
                now,
                now,
                actor,
                challenge.Id,
                $"{topPoints} puntos en el reto."));
        }

        await db.SaveChangesAsync(cancellationToken);
        await NotifyAthletesAsync(
            winners.Select(item => item.AthleteProfileId),
            "¡Reto ganado!",
            $"Ganaste el reto «{challenge.Name}».",
            cancellationToken);

        return new AppResult.Success<ChallengeDto>((await ProjectAsync([challenge], cancellationToken))[0], "Reto finalizado.");
    }

    /// <inheritdoc />
    public Task<AppResult> CancelAsync(Guid id, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, (challenge, now, actor) => challenge.Cancel(now, actor), "Reto cancelado.", cancellationToken);

    private async Task<AppResult> ChangeStatusAsync(
        Guid id,
        Action<Challenge, DateTimeOffset, Guid?> change,
        string message,
        CancellationToken cancellationToken)
    {
        var (_, permissionFailure) = await access.RequirePermissionAsync(Permissions.ChallengesManage, cancellationToken);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        var challenge = await db.Challenges.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null)
        {
            return AdminAccess.Fail("No se encontró el reto.", 404);
        }

        try
        {
            change(challenge, timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ChallengeDto>((await ProjectAsync([challenge], cancellationToken))[0], message);
    }

    // ------------------------------------------------------------------
    // Participación y puntos
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> JoinAsync(Guid id, JoinChallengeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Los alumnos se inscriben solos (lectura de retos); el personal inscribe a otros con permiso de gestión.
        var permission = athletes.IsAthlete ? Permissions.ChallengesRead : Permissions.ChallengesManage;
        var target = await athletes.ForWriteAsync(permission, request.AthleteProfileId, true, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } athleteId)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null || (athletes.IsAthlete && challenge.Status == ChallengeStatus.Draft))
        {
            return AdminAccess.Fail("No se encontró el reto.", 404);
        }

        if (challenge.Status != ChallengeStatus.Active)
        {
            return AdminAccess.Fail("Solo puedes unirte a un reto activo.", 400);
        }

        if (await db.ChallengeParticipants.AnyAsync(
                item => item.ChallengeId == id && item.AthleteProfileId == athleteId,
                cancellationToken))
        {
            return AdminAccess.Fail("Ya participas en este reto.", 409);
        }

        var participant = ChallengeParticipant.Create(
            access.RequireTenantId(),
            id,
            athleteId,
            timeProvider.GetUtcNow(),
            access.RequireUserId());
        db.ChallengeParticipants.Add(participant);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ChallengeParticipantDto>(
            new ChallengeParticipantDto(participant.Id, participant.ChallengeId, participant.AthleteProfileId, participant.Points, participant.JoinedAt),
            "Te uniste al reto.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListActivitiesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var (challenge, failure) = await FindVisibleAsync(id, Permissions.ChallengesRead, tracking: false, cancellationToken);
        if (failure is not null || challenge is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró el reto.", 404);
        }

        var participants = await db.ChallengeParticipants.AsNoTracking()
            .Where(item => item.ChallengeId == id)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var names = await athletes.NamesAsync(
            participants.Values.Select(item => item.AthleteProfileId).Distinct().ToList(),
            cancellationToken);

        var items = (await db.ChallengeActivities.AsNoTracking()
                .Where(item => item.ChallengeId == id)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.OccurredAt)
            .Take(MaxListed)
            .Where(item => participants.ContainsKey(item.ParticipantId))
            .Select(item =>
            {
                var athleteId = participants[item.ParticipantId].AthleteProfileId;
                return new ChallengeActivityDto(
                    item.Id,
                    item.ChallengeId,
                    item.ParticipantId,
                    athleteId,
                    names.GetValueOrDefault(athleteId, string.Empty),
                    item.Points,
                    item.Note,
                    item.OccurredAt);
            })
            .ToList();
        return new AppResult.Success<IReadOnlyList<ChallengeActivityDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> AddActivityAsync(Guid id, AddChallengeActivityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = await athletes.ForWriteAsync(Permissions.ChallengesManage, request.AthleteProfileId, false, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } athleteId)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var challenge = await db.Challenges.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null)
        {
            return AdminAccess.Fail("No se encontró el reto.", 404);
        }

        if (challenge.Status != ChallengeStatus.Active)
        {
            return AdminAccess.Fail("Solo se registran puntos en un reto activo.", 400);
        }

        if (request.Points is < 1 or > MaxPointsPerActivity)
        {
            return AdminAccess.Fail($"Los puntos deben estar entre 1 y {MaxPointsPerActivity}.", 400);
        }

        var participant = await db.ChallengeParticipants.FirstOrDefaultAsync(
            item => item.ChallengeId == id && item.AthleteProfileId == athleteId,
            cancellationToken);
        if (participant is null)
        {
            return AdminAccess.Fail("El alumno no participa en el reto.", 400);
        }

        var now = timeProvider.GetUtcNow();
        var actor = access.RequireUserId();
        ChallengeActivity activity;
        try
        {
            activity = ChallengeActivity.Create(
                access.RequireTenantId(),
                id,
                participant.Id,
                request.Points,
                request.OccurredAt ?? now,
                now,
                actor,
                request.Note);
            participant.AddPoints(request.Points, now, actor);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.ChallengeActivities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        var names = await athletes.NamesAsync([athleteId], cancellationToken);
        return new AppResult.Success<ChallengeActivityDto>(
            new ChallengeActivityDto(
                activity.Id,
                activity.ChallengeId,
                activity.ParticipantId,
                athleteId,
                names.GetValueOrDefault(athleteId, string.Empty),
                activity.Points,
                activity.Note,
                activity.OccurredAt),
            "Puntos registrados.");
    }

    /// <inheritdoc />
    public async Task<AppResult> GetLeaderboardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var (challenge, failure) = await FindVisibleAsync(id, Permissions.ChallengesRead, tracking: false, cancellationToken);
        if (failure is not null || challenge is null)
        {
            return failure ?? AdminAccess.Fail("No se encontró el reto.", 404);
        }

        var participants = await db.ChallengeParticipants.AsNoTracking()
            .Where(item => item.ChallengeId == id)
            .ToListAsync(cancellationToken);
        var names = await athletes.NamesAsync(
            participants.Select(item => item.AthleteProfileId).Distinct().ToList(),
            cancellationToken);

        return new AppResult.Success<IReadOnlyList<LeaderboardEntryDto>>(BuildLeaderboard(participants, names));
    }

    /// <summary>
    /// Ordena por puntos de mayor a menor (desempate por inscripción) con posición de competición:
    /// los empatados comparten rango. Pública para poder probarla sin base de datos.
    /// </summary>
    public static IReadOnlyList<LeaderboardEntryDto> BuildLeaderboard(
        IEnumerable<ChallengeParticipant> participants,
        IReadOnlyDictionary<Guid, string> names)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(names);

        var ordered = participants
            .OrderByDescending(item => item.Points)
            .ThenBy(item => item.JoinedAt)
            .ToList();
        var entries = new List<LeaderboardEntryDto>(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            var rank = index > 0 && ordered[index - 1].Points == item.Points
                ? entries[index - 1].Rank
                : index + 1;
            entries.Add(new LeaderboardEntryDto(
                rank,
                item.Id,
                item.AthleteProfileId,
                names.TryGetValue(item.AthleteProfileId, out var name) ? name : string.Empty,
                item.Points));
        }

        return entries;
    }

    // ------------------------------------------------------------------
    // Logros
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListAchievementsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.ChallengesRead, athleteProfileId, true, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        if (!target.IsAll && target.AthleteId is null)
        {
            return new AppResult.Success<IReadOnlyList<AchievementDto>>([]);
        }

        var query = db.Achievements.AsNoTracking();
        if (target.AthleteId is { } athleteId)
        {
            query = query.Where(item => item.AthleteProfileId == athleteId);
        }

        var achievements = (await query.ToListAsync(cancellationToken))
            .OrderByDescending(item => item.AwardedAt)
            .Take(MaxListed)
            .ToList();
        var names = await athletes.NamesAsync(
            achievements.Select(item => item.AthleteProfileId).Distinct().ToList(),
            cancellationToken);

        return new AppResult.Success<IReadOnlyList<AchievementDto>>(
            achievements.Select(item => ToDto(item, names)).ToList());
    }

    /// <inheritdoc />
    public async Task<AppResult> AwardAchievementAsync(AwardAchievementRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = await athletes.ForWriteAsync(Permissions.ChallengesManage, request.AthleteProfileId, false, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } athleteId)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        if (request.ChallengeId is { } challengeId
            && !await db.Challenges.AnyAsync(item => item.Id == challengeId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró el reto.", 404);
        }

        var now = timeProvider.GetUtcNow();
        Achievement achievement;
        try
        {
            achievement = Achievement.Create(
                access.RequireTenantId(),
                athleteId,
                request.Title,
                now,
                now,
                access.RequireUserId(),
                request.ChallengeId,
                request.Description);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.Achievements.Add(achievement);
        await db.SaveChangesAsync(cancellationToken);
        await NotifyAthletesAsync([athleteId], "Nuevo logro", achievement.Title, cancellationToken);

        var names = await athletes.NamesAsync([athleteId], cancellationToken);
        return new AppResult.Success<AchievementDto>(ToDto(achievement, names), "Logro otorgado.");
    }

    // ------------------------------------------------------------------
    // Auxiliares
    // ------------------------------------------------------------------

    private async Task<(Challenge? Challenge, AppResult? Failure)> FindVisibleAsync(
        Guid id,
        string permission,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await access.RequirePermissionAsync(permission, cancellationToken);
        if (failure is not null)
        {
            return (null, failure);
        }

        var query = tracking ? db.Challenges : db.Challenges.AsNoTracking();
        var challenge = await query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (challenge is null || (athletes.IsAthlete && challenge.Status == ChallengeStatus.Draft))
        {
            return (null, AdminAccess.Fail("No se encontró el reto.", 404));
        }

        return (challenge, null);
    }

    private async Task<List<ChallengeDto>> ProjectAsync(
        IReadOnlyList<Challenge> challenges,
        CancellationToken cancellationToken)
    {
        if (challenges.Count == 0)
        {
            return [];
        }

        var ids = challenges.Select(item => item.Id).ToList();
        var participants = await db.ChallengeParticipants.AsNoTracking()
            .Where(item => ids.Contains(item.ChallengeId))
            .ToListAsync(cancellationToken);

        Guid? ownId = null;
        if (athletes.IsAthlete)
        {
            ownId = (await TrainingLookups.FindAthleteProfileByUserAsync(db, access.RequireUserId(), cancellationToken))?.Id;
        }

        return challenges.Select(item =>
        {
            var mine = participants.Where(participant => participant.ChallengeId == item.Id).ToList();
            return new ChallengeDto(
                item.Id,
                item.Name,
                item.Description,
                item.Kind,
                item.Metric,
                item.StartsAt,
                item.EndsAt,
                item.TargetValue,
                item.Status,
                mine.Count,
                ownId is { } own && mine.Any(participant => participant.AthleteProfileId == own));
        }).ToList();
    }

    private async Task NotifyAthletesAsync(
        IEnumerable<Guid> athleteProfileIds,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var ids = athleteProfileIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var userIds = await db.AthleteProfiles.AsNoTracking()
            .Where(profile => ids.Contains(profile.Id))
            .Select(profile => profile.UserId)
            .ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            await notifications.CreateAsync(
                userId,
                NotificationKind.Challenge,
                title,
                body,
                "/challenges",
                cancellationToken);
        }
    }

    private static AchievementDto ToDto(Achievement item, IReadOnlyDictionary<Guid, string> names)
        => new(
            item.Id,
            item.AthleteProfileId,
            names.TryGetValue(item.AthleteProfileId, out var name) ? name : string.Empty,
            item.ChallengeId,
            item.Title,
            item.Description,
            item.AwardedAt);

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
