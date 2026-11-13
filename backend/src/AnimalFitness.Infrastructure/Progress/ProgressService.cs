using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Progress;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Tracking;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Training;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Progress;

/// <summary>Seguimiento del progreso de los alumnos (Fase 4).</summary>
public sealed class ProgressService(
    AppDbContext db,
    IRequestContext requestContext,
    AdminAccess access,
    TimeProvider timeProvider) : IProgressService
{
    private const int MaxListed = 500;

    private bool IsAthlete => requestContext.Role == MembershipRole.Athlete;

    // ------------------------------------------------------------------
    // Mediciones
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListMeasurementsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default)
    {
        var (athleteId, failure) = await ResolveForReadAsync(athleteProfileId, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (athleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<BodyMeasurementDto>>([]);
        }

        var items = (await db.BodyMeasurements.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.MeasuredAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<BodyMeasurementDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateMeasurementAsync(SaveBodyMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (athleteId, failure) = await ResolveForWriteAsync(request.AthleteProfileId, cancellationToken);
        if (failure is not null || athleteId is not { } id)
        {
            return failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var now = timeProvider.GetUtcNow();
        BodyMeasurement measurement;
        try
        {
            measurement = BodyMeasurement.Create(
                access.RequireTenantId(),
                id,
                request.MeasuredAt ?? now,
                access.RequireUserId(),
                now,
                request.WeightKg,
                request.HeightCm,
                request.BodyFatPercent,
                request.MuscleMassKg,
                request.WaistCm,
                request.ChestCm,
                request.ArmCm,
                request.LegCm,
                request.HipCm,
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.BodyMeasurements.Add(measurement);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<BodyMeasurementDto>(ToDto(measurement), "Medición registrada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateMeasurementAsync(Guid id, SaveBodyMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, permissionFailure) = await access.RequirePermissionAsync(Permissions.ProgressManage, cancellationToken);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        var measurement = await db.BodyMeasurements.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (measurement is null || !await CanTouchAsync(measurement.AthleteProfileId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró la medición.", 404);
        }

        try
        {
            measurement.Update(
                request.MeasuredAt ?? measurement.MeasuredAt,
                request.WeightKg,
                request.HeightCm,
                request.BodyFatPercent,
                request.MuscleMassKg,
                request.WaistCm,
                request.ChestCm,
                request.ArmCm,
                request.LegCm,
                request.HipCm,
                request.Notes,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<BodyMeasurementDto>(ToDto(measurement), "Medición actualizada.");
    }

    // ------------------------------------------------------------------
    // Fotos
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListPhotosAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default)
    {
        var (athleteId, failure) = await ResolveForReadAsync(athleteProfileId, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (athleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<ProgressPhotoDto>>([]);
        }

        var items = (await db.ProgressPhotos.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.TakenAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<ProgressPhotoDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreatePhotoAsync(SaveProgressPhotoRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (athleteId, failure) = await ResolveForWriteAsync(request.AthleteProfileId, cancellationToken);
        if (failure is not null || athleteId is not { } id)
        {
            return failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var now = timeProvider.GetUtcNow();
        ProgressPhoto photo;
        try
        {
            photo = ProgressPhoto.Create(
                access.RequireTenantId(),
                id,
                request.TakenAt ?? now,
                request.Pose,
                request.Url,
                access.RequireUserId(),
                now,
                request.Caption,
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.ProgressPhotos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ProgressPhotoDto>(ToDto(photo), "Foto registrada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdatePhotoAsync(Guid id, SaveProgressPhotoRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, permissionFailure) = await access.RequirePermissionAsync(Permissions.ProgressManage, cancellationToken);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        var photo = await db.ProgressPhotos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (photo is null || !await CanTouchAsync(photo.AthleteProfileId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró la foto.", 404);
        }

        try
        {
            photo.Update(
                request.TakenAt ?? photo.TakenAt,
                request.Pose,
                request.Url,
                request.Caption,
                request.Notes,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ProgressPhotoDto>(ToDto(photo), "Foto actualizada.");
    }

    // ------------------------------------------------------------------
    // Valoraciones
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListAssessmentsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default)
    {
        var (athleteId, failure) = await ResolveForReadAsync(athleteProfileId, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (athleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<FitnessAssessmentDto>>([]);
        }

        var items = (await db.FitnessAssessments.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.AssessedAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<FitnessAssessmentDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateAssessmentAsync(SaveFitnessAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (athleteId, failure) = await ResolveForWriteAsync(request.AthleteProfileId, cancellationToken);
        if (failure is not null || athleteId is not { } id)
        {
            return failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var now = timeProvider.GetUtcNow();
        FitnessAssessment assessment;
        try
        {
            assessment = FitnessAssessment.Create(
                access.RequireTenantId(),
                id,
                request.AssessedAt ?? now,
                request.Title,
                request.Summary,
                access.RequireUserId(),
                now,
                request.Score);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.FitnessAssessments.Add(assessment);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<FitnessAssessmentDto>(ToDto(assessment), "Valoración registrada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateAssessmentAsync(Guid id, SaveFitnessAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, permissionFailure) = await access.RequirePermissionAsync(Permissions.ProgressManage, cancellationToken);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        var assessment = await db.FitnessAssessments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (assessment is null || !await CanTouchAsync(assessment.AthleteProfileId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró la valoración.", 404);
        }

        try
        {
            assessment.Update(
                request.AssessedAt ?? assessment.AssessedAt,
                request.Title,
                request.Summary,
                request.Score,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<FitnessAssessmentDto>(ToDto(assessment), "Valoración actualizada.");
    }

    // ------------------------------------------------------------------
    // Objetivos
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListGoalsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default)
    {
        var (athleteId, failure) = await ResolveForReadAsync(athleteProfileId, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (athleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<ProgressGoalDto>>([]);
        }

        var items = (await db.AthleteGoals.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.CreatedAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<ProgressGoalDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateGoalAsync(CreateProgressGoalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (athleteId, failure) = await ResolveForWriteAsync(request.AthleteProfileId, cancellationToken);
        if (failure is not null || athleteId is not { } id)
        {
            return failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        AthleteGoal goal;
        try
        {
            goal = AthleteGoal.Create(
                access.RequireTenantId(),
                id,
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
        return new AppResult.Success<ProgressGoalDto>(ToDto(goal), "Objetivo creado.");
    }

    /// <inheritdoc />
    public Task<AppResult> CompleteGoalAsync(Guid id, CancellationToken cancellationToken = default)
        => ChangeGoalAsync(id, (goal, now, actor) => goal.Complete(now, actor), "Objetivo cumplido.", cancellationToken);

    /// <inheritdoc />
    public Task<AppResult> CancelGoalAsync(Guid id, CancellationToken cancellationToken = default)
        => ChangeGoalAsync(id, (goal, now, actor) => goal.Cancel(now, actor), "Objetivo cancelado.", cancellationToken);

    private async Task<AppResult> ChangeGoalAsync(
        Guid id,
        Action<AthleteGoal, DateTimeOffset, Guid?> change,
        string message,
        CancellationToken cancellationToken)
    {
        var (_, permissionFailure) = await access.RequirePermissionAsync(Permissions.ProgressManage, cancellationToken);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        var goal = await db.AthleteGoals.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (goal is null || !await CanTouchAsync(goal.AthleteProfileId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró el objetivo.", 404);
        }

        try
        {
            change(goal, timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ProgressGoalDto>(ToDto(goal), message);
    }

    // ------------------------------------------------------------------
    // Gráficos
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> GetChartsAsync(
        Guid? athleteProfileId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var (athleteId, failure) = await ResolveForReadAsync(athleteProfileId, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (from is { } start && to is { } end && start > end)
        {
            return AdminAccess.Fail("El rango de fechas no es válido.", 400);
        }

        if (athleteId is not { } id)
        {
            return new AppResult.Success<ProgressChartsDto>(
                new ProgressChartsDto(Guid.Empty, [], [], [], []));
        }

        // SQLite no compara/ordena DateTimeOffset: se filtra y ordena en memoria.
        var measurements = (await db.BodyMeasurements.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .Where(item => (from is null || item.MeasuredAt >= from) && (to is null || item.MeasuredAt <= to))
            .OrderBy(item => item.MeasuredAt)
            .ToList();

        var charts = new ProgressChartsDto(
            id,
            Series(measurements, item => item.WeightKg),
            Series(measurements, item => item.BodyFatPercent),
            Series(measurements, item => item.WaistCm),
            Series(measurements, item => item.ComputeBmi()));
        return new AppResult.Success<ProgressChartsDto>(charts);
    }

    private static List<ChartPointDto> Series(
        IEnumerable<BodyMeasurement> measurements,
        Func<BodyMeasurement, decimal?> selector)
        => measurements
            .Select(item => (item.MeasuredAt, Value: selector(item)))
            .Where(point => point.Value.HasValue)
            .Select(point => new ChartPointDto(point.MeasuredAt, point.Value!.Value))
            .ToList();

    // ------------------------------------------------------------------
    // Alumno objetivo y permisos
    // ------------------------------------------------------------------

    /// <summary>
    /// Resuelve el alumno para lecturas. Un alumno sin perfil recibe <c>null</c> (resultado vacío).
    /// </summary>
    private async Task<(Guid? AthleteId, AppResult? Failure)> ResolveForReadAsync(
        Guid? requested,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ProgressRead, cancellationToken);
        if (failure is not null)
        {
            return (null, failure);
        }

        if (IsAthlete)
        {
            var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, access.RequireUserId(), cancellationToken);
            if (requested is { } other && other != own?.Id)
            {
                return (null, AdminAccess.Fail("Solo puedes ver tu propio progreso.", 403));
            }

            return (own?.Id, null);
        }

        return await RequireAthleteAsync(requested, cancellationToken);
    }

    /// <summary>Resuelve el alumno para escrituras; crea el perfil propio de un alumno si falta.</summary>
    private async Task<(Guid? AthleteId, AppResult? Failure)> ResolveForWriteAsync(
        Guid? requested,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ProgressManage, cancellationToken);
        if (failure is not null)
        {
            return (null, failure);
        }

        if (IsAthlete)
        {
            var userId = access.RequireUserId();
            var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, userId, cancellationToken);
            if (own is null)
            {
                own = AthleteProfile.Create(access.RequireTenantId(), userId, timeProvider.GetUtcNow(), userId);
                db.AthleteProfiles.Add(own);
            }

            if (requested is { } other && other != own.Id)
            {
                return (null, AdminAccess.Fail("Solo puedes registrar tu propio progreso.", 403));
            }

            return (own.Id, null);
        }

        return await RequireAthleteAsync(requested, cancellationToken);
    }

    private async Task<(Guid? AthleteId, AppResult? Failure)> RequireAthleteAsync(
        Guid? requested,
        CancellationToken cancellationToken)
    {
        if (requested is not { } id || id == Guid.Empty)
        {
            return (null, AdminAccess.Fail("Indica el alumno.", 400));
        }

        if (!await db.AthleteProfiles.AnyAsync(profile => profile.Id == id, cancellationToken))
        {
            return (null, AdminAccess.Fail("No se encontró el alumno.", 404));
        }

        return (id, null);
    }

    /// <summary>Un alumno solo puede tocar registros de su propio perfil.</summary>
    private async Task<bool> CanTouchAsync(Guid athleteProfileId, CancellationToken cancellationToken)
    {
        if (!IsAthlete)
        {
            return true;
        }

        var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, access.RequireUserId(), cancellationToken);
        return own is not null && own.Id == athleteProfileId;
    }

    // ------------------------------------------------------------------
    // Proyecciones
    // ------------------------------------------------------------------

    private static BodyMeasurementDto ToDto(BodyMeasurement item)
        => new(
            item.Id,
            item.AthleteProfileId,
            item.MeasuredAt,
            item.WeightKg,
            item.HeightCm,
            item.BodyFatPercent,
            item.MuscleMassKg,
            item.WaistCm,
            item.ChestCm,
            item.ArmCm,
            item.LegCm,
            item.HipCm,
            item.ComputeBmi(),
            item.Notes,
            item.RecordedByUserId);

    private static ProgressPhotoDto ToDto(ProgressPhoto item)
        => new(
            item.Id,
            item.AthleteProfileId,
            item.TakenAt,
            item.Pose,
            item.Url,
            item.Caption,
            item.Notes,
            item.RecordedByUserId);

    private static FitnessAssessmentDto ToDto(FitnessAssessment item)
        => new(
            item.Id,
            item.AthleteProfileId,
            item.AssessedAt,
            item.Title,
            item.Summary,
            item.Score,
            item.AssessedByUserId);

    private static ProgressGoalDto ToDto(AthleteGoal item)
        => new(
            item.Id,
            item.AthleteProfileId,
            item.Title,
            item.Description,
            item.TargetValue,
            item.Unit,
            item.DueDate,
            item.Status);
}
