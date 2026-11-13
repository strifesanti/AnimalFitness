using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Progress;

/// <summary>
/// Seguimiento del progreso: mediciones, fotos, valoraciones, objetivos y series para gráficos.
/// Los alumnos solo acceden a su propio perfil; Owner y Trainer a cualquier alumno de la organización.
/// </summary>
public interface IProgressService
{
    /// <summary>Lista las mediciones del alumno (más recientes primero).</summary>
    Task<AppResult> ListMeasurementsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Registra una medición corporal.</summary>
    Task<AppResult> CreateMeasurementAsync(SaveBodyMeasurementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza una medición corporal.</summary>
    Task<AppResult> UpdateMeasurementAsync(Guid id, SaveBodyMeasurementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista las fotos de progreso del alumno.</summary>
    Task<AppResult> ListPhotosAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Registra una foto de progreso por URL.</summary>
    Task<AppResult> CreatePhotoAsync(SaveProgressPhotoRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza una foto de progreso.</summary>
    Task<AppResult> UpdatePhotoAsync(Guid id, SaveProgressPhotoRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista las valoraciones físicas del alumno.</summary>
    Task<AppResult> ListAssessmentsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Registra una valoración física.</summary>
    Task<AppResult> CreateAssessmentAsync(SaveFitnessAssessmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza una valoración física.</summary>
    Task<AppResult> UpdateAssessmentAsync(Guid id, SaveFitnessAssessmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista los objetivos del alumno.</summary>
    Task<AppResult> ListGoalsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Crea un objetivo para el alumno.</summary>
    Task<AppResult> CreateGoalAsync(CreateProgressGoalRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marca un objetivo como cumplido.</summary>
    Task<AppResult> CompleteGoalAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cancela un objetivo.</summary>
    Task<AppResult> CancelGoalAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Series temporales (peso, grasa, cintura, IMC) en un rango opcional de fechas.</summary>
    Task<AppResult> GetChartsAsync(
        Guid? athleteProfileId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);
}
