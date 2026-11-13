using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Training;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Training;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Training;

/// <summary>Planes de entrenamiento, versiones y asignaciones.</summary>
public sealed class WorkoutService(
    AppDbContext db,
    IRequestContext requestContext,
    AdminAccess access,
    TimeProvider timeProvider) : IWorkoutService
{
    private const string NotDraftMessage = "Solo se puede editar un plan en borrador. Crea una nueva versión.";

    /// <inheritdoc />
    public async Task<AppResult> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        List<WorkoutPlan> plans;
        if (CanManage())
        {
            plans = await db.WorkoutPlans.AsNoTracking().ToListAsync(cancellationToken);
        }
        else
        {
            var planIds = await AssignedPlanIdsAsync(cancellationToken);
            plans = planIds.Count == 0
                ? []
                : await db.WorkoutPlans.AsNoTracking()
                    .Where(plan => planIds.Contains(plan.Id) && plan.Status == WorkoutPlanStatus.Published)
                    .ToListAsync(cancellationToken);
        }

        var ids = plans.Select(plan => plan.Id).ToList();
        var dayCounts = await db.WorkoutDays.AsNoTracking()
            .Where(day => ids.Contains(day.PlanId))
            .GroupBy(day => day.PlanId)
            .Select(group => new { PlanId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.PlanId, item => item.Count, cancellationToken);

        var items = plans
            .OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenByDescending(plan => plan.Version)
            .Select(plan => new WorkoutPlanSummaryDto(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.GoalLabel,
                plan.Version,
                plan.ParentPlanId,
                plan.Status,
                dayCounts.TryGetValue(plan.Id, out var count) ? count : 0))
            .ToList();
        return new AppResult.Success<IReadOnlyList<WorkoutPlanSummaryDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var plan = await db.WorkoutPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        if (!CanManage())
        {
            var assigned = await AssignedPlanIdsAsync(cancellationToken);
            if (plan.Status != WorkoutPlanStatus.Published || !assigned.Contains(plan.Id))
            {
                return AdminAccess.Fail("No se encontró el plan.", 404);
            }
        }

        return new AppResult.Success<WorkoutPlanDetailDto>(await BuildDetailAsync(plan, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AppResult> CreatePlanAsync(CreateWorkoutPlanRequest request, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var userId = access.RequireUserId();
        var authorId = await db.TrainerProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(cancellationToken);

        WorkoutPlan plan;
        try
        {
            plan = WorkoutPlan.Create(
                access.RequireTenantId(),
                request.Name,
                timeProvider.GetUtcNow(),
                userId,
                request.Description,
                request.GoalLabel,
                authorId);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WorkoutPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutPlanDetailDto>(ToDetail(plan, []), "Plan creado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdatePlanAsync(
        Guid planId,
        UpdateWorkoutPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var plan = await db.WorkoutPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        if (plan.Status != WorkoutPlanStatus.Draft)
        {
            return AdminAccess.Fail(NotDraftMessage, 409);
        }

        try
        {
            plan.UpdateDraft(
                request.Name,
                request.Description,
                request.GoalLabel,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutPlanDetailDto>(
            await BuildDetailAsync(plan, cancellationToken),
            "Plan actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> PublishPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var plan = await db.WorkoutPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        if (!await db.WorkoutDays.AnyAsync(day => day.PlanId == planId, cancellationToken))
        {
            return AdminAccess.Fail("El plan necesita al menos un día para publicarse.", 400);
        }

        try
        {
            plan.Publish(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Plan publicado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ArchivePlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var plan = await db.WorkoutPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        try
        {
            plan.Archive(timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(Message: "Plan archivado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateNewVersionAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var plan = await db.WorkoutPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        var tenantId = access.RequireTenantId();
        var userId = access.RequireUserId();
        var now = timeProvider.GetUtcNow();

        WorkoutPlan next;
        var newDays = new List<WorkoutDay>();
        var newExercises = new List<WorkoutExercise>();
        try
        {
            next = plan.CreateNextVersion(now, userId);

            var days = await db.WorkoutDays.AsNoTracking()
                .Where(day => day.PlanId == planId)
                .ToListAsync(cancellationToken);
            var dayIds = days.Select(day => day.Id).ToList();
            var exercises = await db.WorkoutExercises.AsNoTracking()
                .Where(item => dayIds.Contains(item.DayId))
                .ToListAsync(cancellationToken);

            foreach (var day in days.OrderBy(item => item.DayIndex))
            {
                var copy = WorkoutDay.Create(tenantId, next.Id, day.DayIndex, day.Name, now, userId, day.Notes);
                newDays.Add(copy);
                foreach (var item in exercises.Where(exercise => exercise.DayId == day.Id).OrderBy(exercise => exercise.SortOrder))
                {
                    newExercises.Add(WorkoutExercise.Create(
                        tenantId,
                        copy.Id,
                        item.ExerciseId,
                        item.SortOrder,
                        item.TargetSets,
                        now,
                        userId,
                        item.TargetReps,
                        item.TargetWeightKg,
                        item.TargetDurationSeconds,
                        item.TargetDistanceMeters,
                        item.RestSeconds,
                        item.Tempo,
                        item.Notes));
                }
            }
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WorkoutPlans.Add(next);
        db.WorkoutDays.AddRange(newDays);
        db.WorkoutExercises.AddRange(newExercises);
        await db.SaveChangesAsync(cancellationToken);

        return new AppResult.Success<WorkoutPlanDetailDto>(
            await BuildDetailAsync(next, cancellationToken),
            "Nueva versión creada en borrador.");
    }

    /// <inheritdoc />
    public async Task<AppResult> AddDayAsync(
        Guid planId,
        CreateWorkoutDayRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var plan = await db.WorkoutPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        if (plan.Status != WorkoutPlanStatus.Draft)
        {
            return AdminAccess.Fail(NotDraftMessage, 409);
        }

        var indexes = await db.WorkoutDays.AsNoTracking()
            .Where(day => day.PlanId == planId)
            .Select(day => day.DayIndex)
            .ToListAsync(cancellationToken);
        var dayIndex = request.DayIndex ?? (indexes.Count == 0 ? 1 : indexes.Max() + 1);
        if (indexes.Contains(dayIndex))
        {
            return AdminAccess.Fail("Ya existe un día con ese índice.", 409);
        }

        WorkoutDay day;
        try
        {
            day = WorkoutDay.Create(
                access.RequireTenantId(),
                planId,
                dayIndex,
                request.Name,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WorkoutDays.Add(day);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutDayDto>(ToDto(day, []), "Día agregado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateDayAsync(
        Guid dayId,
        UpdateWorkoutDayRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var day = await db.WorkoutDays.FirstOrDefaultAsync(item => item.Id == dayId, cancellationToken);
        if (day is null)
        {
            return AdminAccess.Fail("No se encontró el día.", 404);
        }

        var draftFailure = await EnsurePlanIsDraftAsync(day.PlanId, cancellationToken);
        if (draftFailure is not null)
        {
            return draftFailure;
        }

        try
        {
            day.Update(request.Name, request.Notes, timeProvider.GetUtcNow(), access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        var exercises = await LoadExercisesAsync([day.Id], cancellationToken);
        return new AppResult.Success<WorkoutDayDto>(ToDto(day, exercises), "Día actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> AddExerciseAsync(
        Guid dayId,
        AddWorkoutExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var day = await db.WorkoutDays.AsNoTracking().FirstOrDefaultAsync(item => item.Id == dayId, cancellationToken);
        if (day is null)
        {
            return AdminAccess.Fail("No se encontró el día.", 404);
        }

        var draftFailure = await EnsurePlanIsDraftAsync(day.PlanId, cancellationToken);
        if (draftFailure is not null)
        {
            return draftFailure;
        }

        var exercise = await db.Exercises.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ExerciseId, cancellationToken);
        if (exercise is null)
        {
            return AdminAccess.Fail("No se encontró el ejercicio.", 404);
        }

        var orders = await db.WorkoutExercises.AsNoTracking()
            .Where(item => item.DayId == dayId)
            .Select(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        var sortOrder = request.SortOrder ?? (orders.Count == 0 ? 1 : orders.Max() + 1);

        WorkoutExercise item;
        try
        {
            item = WorkoutExercise.Create(
                access.RequireTenantId(),
                dayId,
                exercise.Id,
                sortOrder,
                request.TargetSets,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.TargetReps,
                request.TargetWeightKg,
                request.TargetDurationSeconds,
                request.TargetDistanceMeters,
                request.RestSeconds,
                request.Tempo,
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WorkoutExercises.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutExerciseDto>(ToDto(item, exercise.Name), "Ejercicio agregado al día.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateExerciseAsync(
        Guid workoutExerciseId,
        UpdateWorkoutExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var item = await db.WorkoutExercises.FirstOrDefaultAsync(entry => entry.Id == workoutExerciseId, cancellationToken);
        if (item is null)
        {
            return AdminAccess.Fail("No se encontró el ejercicio del plan.", 404);
        }

        var day = await db.WorkoutDays.AsNoTracking().FirstOrDefaultAsync(entry => entry.Id == item.DayId, cancellationToken);
        if (day is null)
        {
            return AdminAccess.Fail("No se encontró el día.", 404);
        }

        var draftFailure = await EnsurePlanIsDraftAsync(day.PlanId, cancellationToken);
        if (draftFailure is not null)
        {
            return draftFailure;
        }

        try
        {
            item.Update(
                request.SortOrder,
                request.TargetSets,
                request.TargetReps,
                request.TargetWeightKg,
                request.TargetDurationSeconds,
                request.TargetDistanceMeters,
                request.RestSeconds,
                request.Tempo,
                request.Notes,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        var name = await db.Exercises.AsNoTracking()
            .Where(exercise => exercise.Id == item.ExerciseId)
            .Select(exercise => exercise.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        return new AppResult.Success<WorkoutExerciseDto>(ToDto(item, name), "Ejercicio del plan actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> AssignAsync(
        Guid planId,
        AssignWorkoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var plan = await db.WorkoutPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == planId, cancellationToken);
        if (plan is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        if (plan.Status != WorkoutPlanStatus.Published)
        {
            return AdminAccess.Fail("Solo se pueden asignar planes publicados.", 409);
        }

        if (!await db.AthleteProfiles.AnyAsync(item => item.Id == request.AthleteProfileId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var active = await db.WorkoutPlanAssignments.AnyAsync(
            item => item.PlanId == planId
                && item.AthleteProfileId == request.AthleteProfileId
                && item.Status == PlanAssignmentStatus.Active,
            cancellationToken);
        if (active)
        {
            return AdminAccess.Fail("El alumno ya tiene este plan asignado.", 409);
        }

        WorkoutPlanAssignment assignment;
        try
        {
            assignment = WorkoutPlanAssignment.Create(
                access.RequireTenantId(),
                planId,
                request.AthleteProfileId,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WorkoutPlanAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WorkoutAssignmentDto>(
            new WorkoutAssignmentDto(
                assignment.Id,
                assignment.PlanId,
                assignment.AthleteProfileId,
                assignment.AssignedAt,
                assignment.Status),
            "Plan asignado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> EndAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.WorkoutsManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var assignment = await db.WorkoutPlanAssignments
            .FirstOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
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

    private bool CanManage()
        => PermissionMap.Has(Permissions.WorkoutsManage, requestContext.Role, isSuperAdmin: false);

    private async Task<HashSet<Guid>> AssignedPlanIdsAsync(CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { } userId)
        {
            return [];
        }

        var profile = await db.AthleteProfiles.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile is null)
        {
            return [];
        }

        var ids = await db.WorkoutPlanAssignments.AsNoTracking()
            .Where(item => item.AthleteProfileId == profile.Id && item.Status == PlanAssignmentStatus.Active)
            .Select(item => item.PlanId)
            .ToListAsync(cancellationToken);
        return [.. ids];
    }

    private async Task<AppResult?> EnsurePlanIsDraftAsync(Guid planId, CancellationToken cancellationToken)
    {
        var status = await db.WorkoutPlans.AsNoTracking()
            .Where(plan => plan.Id == planId)
            .Select(plan => (WorkoutPlanStatus?)plan.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return AdminAccess.Fail("No se encontró el plan.", 404);
        }

        return status == WorkoutPlanStatus.Draft ? null : AdminAccess.Fail(NotDraftMessage, 409);
    }

    private async Task<IReadOnlyList<WorkoutExerciseDto>> LoadExercisesAsync(
        IReadOnlyCollection<Guid> dayIds,
        CancellationToken cancellationToken)
    {
        var items = await db.WorkoutExercises.AsNoTracking()
            .Where(item => dayIds.Contains(item.DayId))
            .ToListAsync(cancellationToken);
        var exerciseIds = items.Select(item => item.ExerciseId).Distinct().ToList();
        var names = await db.Exercises.AsNoTracking()
            .Where(exercise => exerciseIds.Contains(exercise.Id))
            .ToDictionaryAsync(exercise => exercise.Id, exercise => exercise.Name, cancellationToken);
        return items
            .OrderBy(item => item.SortOrder)
            .Select(item => ToDto(item, names.TryGetValue(item.ExerciseId, out var name) ? name : string.Empty))
            .ToList();
    }

    private async Task<WorkoutPlanDetailDto> BuildDetailAsync(WorkoutPlan plan, CancellationToken cancellationToken)
    {
        var days = (await db.WorkoutDays.AsNoTracking()
                .Where(day => day.PlanId == plan.Id)
                .ToListAsync(cancellationToken))
            .OrderBy(day => day.DayIndex)
            .ToList();
        var exercises = await LoadExercisesAsync(days.Select(day => day.Id).ToList(), cancellationToken);
        var dtos = days
            .Select(day => ToDto(day, exercises.Where(item => item.DayId == day.Id).ToList()))
            .ToList();
        return ToDetail(plan, dtos);
    }

    private static WorkoutPlanDetailDto ToDetail(WorkoutPlan plan, IReadOnlyList<WorkoutDayDto> days)
        => new(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.GoalLabel,
            plan.Version,
            plan.ParentPlanId,
            plan.Status,
            days);

    private static WorkoutDayDto ToDto(WorkoutDay day, IReadOnlyList<WorkoutExerciseDto> exercises)
        => new(day.Id, day.PlanId, day.DayIndex, day.Name, day.Notes, exercises);

    private static WorkoutExerciseDto ToDto(WorkoutExercise item, string exerciseName)
        => new(
            item.Id,
            item.DayId,
            item.ExerciseId,
            exerciseName,
            item.SortOrder,
            item.TargetSets,
            item.TargetReps,
            item.TargetWeightKg,
            item.TargetDurationSeconds,
            item.TargetDistanceMeters,
            item.RestSeconds,
            item.Tempo,
            item.Notes);
}
