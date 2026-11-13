using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Training;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Training;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Training;

/// <summary>Biblioteca de ejercicios y categorías.</summary>
public sealed class ExerciseService(
    AppDbContext db,
    AdminAccess access,
    TimeProvider timeProvider) : IExerciseService
{
    /// <inheritdoc />
    public async Task<AppResult> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var items = (await db.ExerciseCategories.AsNoTracking().ToListAsync(cancellationToken))
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<ExerciseCategoryDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateCategoryAsync(
        CreateExerciseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        ExerciseCategory category;
        try
        {
            category = ExerciseCategory.Create(
                access.RequireTenantId(),
                request.Name,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.Description,
                request.SortOrder);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.ExerciseCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ExerciseCategoryDto>(ToDto(category), "Categoría creada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateCategoryAsync(
        Guid categoryId,
        UpdateExerciseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var category = await db.ExerciseCategories.FirstOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (category is null)
        {
            return AdminAccess.Fail("No se encontró la categoría.", 404);
        }

        try
        {
            category.Update(
                request.Name,
                request.Description,
                request.SortOrder,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ExerciseCategoryDto>(ToDto(category), "Categoría actualizada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> ListExercisesAsync(
        string? query,
        ExerciseType? type,
        string? muscle,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesRead, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var exercises = db.Exercises.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var text = query.Trim().ToLower();
            exercises = exercises.Where(exercise => exercise.Name.ToLower().Contains(text));
        }

        if (type is not null)
        {
            exercises = exercises.Where(exercise => exercise.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(muscle))
        {
            var text = muscle.Trim().ToLower();
            exercises = exercises.Where(exercise =>
                exercise.PrimaryMuscle.ToLower().Contains(text)
                || (exercise.SecondaryMuscles != null && exercise.SecondaryMuscles.ToLower().Contains(text)));
        }

        var list = await exercises.OrderBy(exercise => exercise.Name).ToListAsync(cancellationToken);
        var ids = list.Select(exercise => exercise.Id).ToList();
        var media = (await db.ExerciseMediaItems.AsNoTracking()
                .Where(item => ids.Contains(item.ExerciseId))
                .ToListAsync(cancellationToken))
            .GroupBy(item => item.ExerciseId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.SortOrder).ToList());

        var items = list
            .Select(exercise => ToDto(
                exercise,
                media.TryGetValue(exercise.Id, out var found) ? found : []))
            .ToList();
        return new AppResult.Success<IReadOnlyList<ExerciseDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateExerciseAsync(
        SaveExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var reference = await ValidateReferencesAsync(request, cancellationToken);
        if (reference is not null)
        {
            return reference;
        }

        Exercise exercise;
        try
        {
            if (await NameTakenAsync(request.Name, null, cancellationToken))
            {
                return AdminAccess.Fail("Ya existe un ejercicio con ese nombre.", 409);
            }

            exercise = Exercise.Create(
                access.RequireTenantId(),
                request.Name,
                request.PrimaryMuscle,
                request.Type,
                request.Difficulty,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.CategoryId,
                request.Description,
                request.SecondaryMuscles,
                request.Equipment,
                request.Instructions,
                request.VariationOfExerciseId);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.Exercises.Add(exercise);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ExerciseDto>(ToDto(exercise, []), "Ejercicio creado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateExerciseAsync(
        Guid exerciseId,
        SaveExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        var exercise = await db.Exercises.FirstOrDefaultAsync(item => item.Id == exerciseId, cancellationToken);
        if (exercise is null)
        {
            return AdminAccess.Fail("No se encontró el ejercicio.", 404);
        }

        var reference = await ValidateReferencesAsync(request, cancellationToken);
        if (reference is not null)
        {
            return reference;
        }

        try
        {
            if (await NameTakenAsync(request.Name, exerciseId, cancellationToken))
            {
                return AdminAccess.Fail("Ya existe un ejercicio con ese nombre.", 409);
            }

            exercise.Update(
                request.Name,
                request.PrimaryMuscle,
                request.Type,
                request.Difficulty,
                request.CategoryId,
                request.Description,
                request.SecondaryMuscles,
                request.Equipment,
                request.Instructions,
                request.VariationOfExerciseId,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        var media = await LoadMediaAsync(exercise.Id, cancellationToken);
        return new AppResult.Success<ExerciseDto>(ToDto(exercise, media), "Ejercicio actualizado.");
    }

    /// <inheritdoc />
    public async Task<AppResult> AddMediaAsync(
        Guid exerciseId,
        AddExerciseMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.ExercisesManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ArgumentNullException.ThrowIfNull(request);
        if (!await db.Exercises.AnyAsync(item => item.Id == exerciseId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontró el ejercicio.", 404);
        }

        ExerciseMedia media;
        try
        {
            media = ExerciseMedia.Create(
                access.RequireTenantId(),
                exerciseId,
                request.MediaType,
                request.Url,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.SortOrder,
                request.Caption);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.ExerciseMediaItems.Add(media);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<ExerciseMediaDto>(ToDto(media), "Medio agregado.");
    }

    private async Task<AppResult?> ValidateReferencesAsync(SaveExerciseRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId is { } categoryId
            && !await db.ExerciseCategories.AnyAsync(item => item.Id == categoryId, cancellationToken))
        {
            return AdminAccess.Fail("La categoría no existe.", 404);
        }

        if (request.VariationOfExerciseId is { } variationId
            && !await db.Exercises.AnyAsync(item => item.Id == variationId, cancellationToken))
        {
            return AdminAccess.Fail("El ejercicio base de la variación no existe.", 404);
        }

        return null;
    }

    private async Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return await db.Exercises.AnyAsync(
            item => item.Name.ToLower() == normalized && (exceptId == null || item.Id != exceptId),
            cancellationToken);
    }

    private async Task<IReadOnlyList<ExerciseMedia>> LoadMediaAsync(Guid exerciseId, CancellationToken cancellationToken)
        => (await db.ExerciseMediaItems.AsNoTracking()
                .Where(item => item.ExerciseId == exerciseId)
                .ToListAsync(cancellationToken))
            .OrderBy(item => item.SortOrder)
            .ToList();

    private static ExerciseCategoryDto ToDto(ExerciseCategory category)
        => new(category.Id, category.Name, category.Description, category.SortOrder);

    private static ExerciseMediaDto ToDto(ExerciseMedia media)
        => new(media.Id, media.ExerciseId, media.MediaType, media.Url, media.SortOrder, media.Caption);

    private static ExerciseDto ToDto(Exercise exercise, IReadOnlyList<ExerciseMedia> media)
        => new(
            exercise.Id,
            exercise.CategoryId,
            exercise.Name,
            exercise.Description,
            exercise.PrimaryMuscle,
            exercise.SecondaryMuscles,
            exercise.Type,
            exercise.Equipment,
            exercise.Difficulty,
            exercise.Instructions,
            exercise.VariationOfExerciseId,
            media.Select(ToDto).ToList());
}
