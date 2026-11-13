using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Training;

/// <summary>Planes de entrenamiento, versiones y asignaciones.</summary>
public interface IWorkoutService
{
    /// <summary>Lista planes. Los alumnos solo ven planes publicados asignados a ellos.</summary>
    Task<AppResult> ListPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene el detalle de un plan con días y ejercicios.</summary>
    Task<AppResult> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Crea un plan en borrador.</summary>
    Task<AppResult> CreatePlanAsync(CreateWorkoutPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza metadatos de un plan en borrador.</summary>
    Task<AppResult> UpdatePlanAsync(Guid planId, UpdateWorkoutPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Publica un plan en borrador.</summary>
    Task<AppResult> PublishPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Archiva un plan.</summary>
    Task<AppResult> ArchivePlanAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Crea una nueva versión en borrador copiando días y ejercicios.</summary>
    Task<AppResult> CreateNewVersionAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Agrega un día a un plan en borrador.</summary>
    Task<AppResult> AddDayAsync(Guid planId, CreateWorkoutDayRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza un día de un plan en borrador.</summary>
    Task<AppResult> UpdateDayAsync(Guid dayId, UpdateWorkoutDayRequest request, CancellationToken cancellationToken = default);

    /// <summary>Agrega un ejercicio a un día de un plan en borrador.</summary>
    Task<AppResult> AddExerciseAsync(Guid dayId, AddWorkoutExerciseRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza la prescripción de un ejercicio de un plan en borrador.</summary>
    Task<AppResult> UpdateExerciseAsync(Guid workoutExerciseId, UpdateWorkoutExerciseRequest request, CancellationToken cancellationToken = default);

    /// <summary>Asigna un plan publicado a un alumno.</summary>
    Task<AppResult> AssignAsync(Guid planId, AssignWorkoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Finaliza una asignación de plan.</summary>
    Task<AppResult> EndAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
}
