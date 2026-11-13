using AnimalFitness.Domain.Nutrition;

namespace AnimalFitness.Application.Features.Nutrition;

/// <summary>Objetivos diarios de nutrición vigentes desde una fecha.</summary>
public sealed record NutritionTargetDto(
    Guid Id,
    Guid AthleteProfileId,
    int Calories,
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams,
    int WaterMl,
    DateOnly EffectiveFrom);

/// <summary>Crear o actualizar objetivos. Sin fecha de vigencia se usa el día actual (UTC).</summary>
public sealed record SaveNutritionTargetRequest(
    Guid? AthleteProfileId,
    int Calories,
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams,
    int WaterMl,
    DateOnly? EffectiveFrom);

/// <summary>Registro de comida.</summary>
public sealed record FoodEntryDto(
    Guid Id,
    Guid AthleteProfileId,
    DateOnly EntryDate,
    MealType MealType,
    string Name,
    int Calories,
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams,
    string? Notes);

/// <summary>Registrar una comida. Sin fecha se usa el día actual (UTC).</summary>
public sealed record CreateFoodEntryRequest(
    Guid? AthleteProfileId,
    DateOnly? EntryDate,
    MealType MealType,
    string Name,
    int Calories,
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams,
    string? Notes);

/// <summary>Registro de consumo de agua.</summary>
public sealed record WaterLogDto(
    Guid Id,
    Guid AthleteProfileId,
    DateOnly EntryDate,
    int AmountMl,
    DateTimeOffset LoggedAt);

/// <summary>Registrar agua. Sin fecha se usa el día actual (UTC).</summary>
public sealed record CreateWaterLogRequest(
    Guid? AthleteProfileId,
    DateOnly? EntryDate,
    int AmountMl);

/// <summary>Totales de un día (o restantes frente al objetivo).</summary>
public sealed record NutritionTotalsDto(
    int Calories,
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams,
    int WaterMl);

/// <summary>Porcentaje de cumplimiento del objetivo (0 si el objetivo es 0).</summary>
public sealed record NutritionPercentagesDto(
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Water);

/// <summary>Resumen diario: consumido, objetivo vigente, restante y cumplimiento.</summary>
public sealed record NutritionSummaryDto(
    Guid? AthleteProfileId,
    DateOnly Date,
    NutritionTargetDto? Target,
    NutritionTotalsDto Consumed,
    NutritionTotalsDto? Remaining,
    NutritionPercentagesDto? Percentages,
    IReadOnlyList<FoodEntryDto> Foods,
    IReadOnlyList<WaterLogDto> Water);
