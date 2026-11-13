using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Nutrition;

/// <summary>
/// Nutrición: objetivos, registro de comidas y agua, y resumen diario.
/// Los alumnos solo acceden a su propio perfil; Owner y Trainer indican el alumno con <c>athleteProfileId</c>.
/// </summary>
public interface INutritionService
{
    /// <summary>Lista los objetivos del alumno (vigencia más reciente primero).</summary>
    Task<AppResult> ListTargetsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Crea unos objetivos diarios.</summary>
    Task<AppResult> CreateTargetAsync(SaveNutritionTargetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza unos objetivos diarios.</summary>
    Task<AppResult> UpdateTargetAsync(Guid id, SaveNutritionTargetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Elimina (borrado lógico) unos objetivos.</summary>
    Task<AppResult> DeleteTargetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista las comidas de un día (hoy por defecto).</summary>
    Task<AppResult> ListFoodsAsync(Guid? athleteProfileId, DateOnly? date, CancellationToken cancellationToken = default);

    /// <summary>Registra una comida.</summary>
    Task<AppResult> CreateFoodAsync(CreateFoodEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Elimina (borrado lógico) una comida.</summary>
    Task<AppResult> DeleteFoodAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista los registros de agua de un día (hoy por defecto).</summary>
    Task<AppResult> ListWaterAsync(Guid? athleteProfileId, DateOnly? date, CancellationToken cancellationToken = default);

    /// <summary>Registra agua consumida.</summary>
    Task<AppResult> CreateWaterAsync(CreateWaterLogRequest request, CancellationToken cancellationToken = default);

    /// <summary>Elimina (borrado lógico) un registro de agua.</summary>
    Task<AppResult> DeleteWaterAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Resumen del día: calorías, macros y agua frente al objetivo vigente.</summary>
    Task<AppResult> GetSummaryAsync(Guid? athleteProfileId, DateOnly? date, CancellationToken cancellationToken = default);
}
