using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Nutrition;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Nutrition;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Advanced;

/// <summary>Nutrición: objetivos, comidas, agua y resumen diario (Fase 5).</summary>
public sealed class NutritionService(
    AppDbContext db,
    AdminAccess access,
    AthleteResolver athletes,
    TimeProvider timeProvider) : INutritionService
{
    private const int MaxListed = 500;

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    // ------------------------------------------------------------------
    // Objetivos
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListTargetsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.NutritionRead, athleteProfileId, false, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        if (target.AthleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<NutritionTargetDto>>([]);
        }

        var items = (await db.NutritionTargets.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.CreatedAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();
        return new AppResult.Success<IReadOnlyList<NutritionTargetDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateTargetAsync(SaveNutritionTargetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = await athletes.ForWriteAsync(Permissions.NutritionManage, request.AthleteProfileId, true, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } id)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        NutritionTarget entity;
        try
        {
            entity = NutritionTarget.Create(
                access.RequireTenantId(),
                id,
                request.Calories,
                request.ProteinGrams,
                request.CarbsGrams,
                request.FatGrams,
                request.WaterMl,
                request.EffectiveFrom ?? Today,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.NutritionTargets.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<NutritionTargetDto>(ToDto(entity), "Objetivos guardados.");
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdateTargetAsync(Guid id, SaveNutritionTargetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, failure) = await access.RequirePermissionAsync(Permissions.NutritionManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var entity = await db.NutritionTargets.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null || !await athletes.CanTouchAsync(entity.AthleteProfileId, cancellationToken))
        {
            return AdminAccess.Fail("No se encontraron los objetivos.", 404);
        }

        try
        {
            entity.Update(
                request.Calories,
                request.ProteinGrams,
                request.CarbsGrams,
                request.FatGrams,
                request.WaterMl,
                request.EffectiveFrom ?? entity.EffectiveFrom,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<NutritionTargetDto>(ToDto(entity), "Objetivos actualizados.");
    }

    /// <inheritdoc />
    public async Task<AppResult> DeleteTargetAsync(Guid id, CancellationToken cancellationToken = default)
        => await DeleteAsync(db.NutritionTargets, id, item => item.AthleteProfileId, "Objetivos eliminados.", "No se encontraron los objetivos.", cancellationToken);

    // ------------------------------------------------------------------
    // Comidas
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListFoodsAsync(Guid? athleteProfileId, DateOnly? date, CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.NutritionRead, athleteProfileId, false, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        if (target.AthleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<FoodEntryDto>>([]);
        }

        return new AppResult.Success<IReadOnlyList<FoodEntryDto>>(await LoadFoodsAsync(id, date ?? Today, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateFoodAsync(CreateFoodEntryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = await athletes.ForWriteAsync(Permissions.NutritionManage, request.AthleteProfileId, true, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } id)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        FoodEntry entry;
        try
        {
            entry = FoodEntry.Create(
                access.RequireTenantId(),
                id,
                request.EntryDate ?? Today,
                request.MealType,
                request.Name,
                request.Calories,
                request.ProteinGrams,
                request.CarbsGrams,
                request.FatGrams,
                timeProvider.GetUtcNow(),
                access.RequireUserId(),
                request.Notes);
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.FoodEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<FoodEntryDto>(ToDto(entry), "Comida registrada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> DeleteFoodAsync(Guid id, CancellationToken cancellationToken = default)
        => await DeleteAsync(db.FoodEntries, id, item => item.AthleteProfileId, "Comida eliminada.", "No se encontró la comida.", cancellationToken);

    // ------------------------------------------------------------------
    // Agua
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> ListWaterAsync(Guid? athleteProfileId, DateOnly? date, CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.NutritionRead, athleteProfileId, false, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        if (target.AthleteId is not { } id)
        {
            return new AppResult.Success<IReadOnlyList<WaterLogDto>>([]);
        }

        return new AppResult.Success<IReadOnlyList<WaterLogDto>>(await LoadWaterAsync(id, date ?? Today, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AppResult> CreateWaterAsync(CreateWaterLogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = await athletes.ForWriteAsync(Permissions.NutritionManage, request.AthleteProfileId, true, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } id)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        WaterLog log;
        try
        {
            log = WaterLog.Create(
                access.RequireTenantId(),
                id,
                request.EntryDate ?? Today,
                request.AmountMl,
                timeProvider.GetUtcNow(),
                access.RequireUserId());
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        db.WaterLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success<WaterLogDto>(ToDto(log), "Agua registrada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> DeleteWaterAsync(Guid id, CancellationToken cancellationToken = default)
        => await DeleteAsync(db.WaterLogs, id, item => item.AthleteProfileId, "Registro de agua eliminado.", "No se encontró el registro de agua.", cancellationToken);

    // ------------------------------------------------------------------
    // Resumen
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<AppResult> GetSummaryAsync(Guid? athleteProfileId, DateOnly? date, CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.NutritionRead, athleteProfileId, false, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        var day = date ?? Today;
        if (target.AthleteId is not { } id)
        {
            return new AppResult.Success<NutritionSummaryDto>(
                new NutritionSummaryDto(null, day, null, new NutritionTotalsDto(0, 0, 0, 0, 0), null, null, [], []));
        }

        var foods = await LoadFoodsAsync(id, day, cancellationToken);
        var water = await LoadWaterAsync(id, day, cancellationToken);

        // Objetivo vigente: el más reciente con vigencia hasta el día consultado.
        var current = (await db.NutritionTargets.AsNoTracking()
                .Where(item => item.AthleteProfileId == id)
                .ToListAsync(cancellationToken))
            .Where(item => item.EffectiveFrom <= day)
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        var summary = BuildSummary(id, day, current is null ? null : ToDto(current), foods, water);
        return new AppResult.Success<NutritionSummaryDto>(summary);
    }

    /// <summary>Calcula consumido, restante y porcentajes. Pública para poder probarla sin base de datos.</summary>
    public static NutritionSummaryDto BuildSummary(
        Guid athleteProfileId,
        DateOnly date,
        NutritionTargetDto? target,
        IReadOnlyList<FoodEntryDto> foods,
        IReadOnlyList<WaterLogDto> water)
    {
        ArgumentNullException.ThrowIfNull(foods);
        ArgumentNullException.ThrowIfNull(water);

        var consumed = new NutritionTotalsDto(
            foods.Sum(item => item.Calories),
            foods.Sum(item => item.ProteinGrams),
            foods.Sum(item => item.CarbsGrams),
            foods.Sum(item => item.FatGrams),
            water.Sum(item => item.AmountMl));

        NutritionTotalsDto? remaining = null;
        NutritionPercentagesDto? percentages = null;
        if (target is not null)
        {
            remaining = new NutritionTotalsDto(
                Math.Max(0, target.Calories - consumed.Calories),
                Math.Max(0, target.ProteinGrams - consumed.ProteinGrams),
                Math.Max(0, target.CarbsGrams - consumed.CarbsGrams),
                Math.Max(0, target.FatGrams - consumed.FatGrams),
                Math.Max(0, target.WaterMl - consumed.WaterMl));
            percentages = new NutritionPercentagesDto(
                Percent(consumed.Calories, target.Calories),
                Percent(consumed.ProteinGrams, target.ProteinGrams),
                Percent(consumed.CarbsGrams, target.CarbsGrams),
                Percent(consumed.FatGrams, target.FatGrams),
                Percent(consumed.WaterMl, target.WaterMl));
        }

        return new NutritionSummaryDto(athleteProfileId, date, target, consumed, remaining, percentages, foods, water);
    }

    private static decimal Percent(decimal value, decimal goal)
        => goal <= 0 ? 0 : Math.Round(value * 100m / goal, 1);

    // ------------------------------------------------------------------
    // Auxiliares
    // ------------------------------------------------------------------

    private async Task<List<FoodEntryDto>> LoadFoodsAsync(Guid athleteId, DateOnly day, CancellationToken cancellationToken)
        => (await db.FoodEntries.AsNoTracking()
                .Where(item => item.AthleteProfileId == athleteId && item.EntryDate == day)
                .ToListAsync(cancellationToken))
            .OrderBy(item => item.MealType)
            .ThenBy(item => item.CreatedAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();

    private async Task<List<WaterLogDto>> LoadWaterAsync(Guid athleteId, DateOnly day, CancellationToken cancellationToken)
        => (await db.WaterLogs.AsNoTracking()
                .Where(item => item.AthleteProfileId == athleteId && item.EntryDate == day)
                .ToListAsync(cancellationToken))
            .OrderBy(item => item.CreatedAt)
            .Take(MaxListed)
            .Select(ToDto)
            .ToList();

    private async Task<AppResult> DeleteAsync<TEntity>(
        DbSet<TEntity> set,
        Guid id,
        Func<TEntity, Guid> athleteOf,
        string message,
        string notFound,
        CancellationToken cancellationToken)
        where TEntity : TenantScopedEntity
    {
        var (_, failure) = await access.RequirePermissionAsync(Permissions.NutritionManage, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var entity = await set.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null || !await athletes.CanTouchAsync(athleteOf(entity), cancellationToken))
        {
            return AdminAccess.Fail(notFound, 404);
        }

        set.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new AppResult.Success(message);
    }

    private static NutritionTargetDto ToDto(NutritionTarget item)
        => new(
            item.Id,
            item.AthleteProfileId,
            item.Calories,
            item.ProteinGrams,
            item.CarbsGrams,
            item.FatGrams,
            item.WaterMl,
            item.EffectiveFrom);

    private static FoodEntryDto ToDto(FoodEntry item)
        => new(
            item.Id,
            item.AthleteProfileId,
            item.EntryDate,
            item.MealType,
            item.Name,
            item.Calories,
            item.ProteinGrams,
            item.CarbsGrams,
            item.FatGrams,
            item.Notes);

    private static WaterLogDto ToDto(WaterLog item)
        => new(item.Id, item.AthleteProfileId, item.EntryDate, item.AmountMl, item.CreatedAt);
}
