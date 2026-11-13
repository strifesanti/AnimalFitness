using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Profiles;

/// <summary>Perfiles de usuario, alumno, entrenador y objetivos.</summary>
public interface IProfileAdminService
{
    /// <summary>Actualiza el perfil global del usuario autenticado.</summary>
    Task<AppResult> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista alumnos de la organización activa.</summary>
    Task<AppResult> ListAthletesAsync(CancellationToken cancellationToken = default);

    /// <summary>Actualiza el perfil físico de un alumno.</summary>
    Task<AppResult> UpdateAthleteAsync(Guid athleteProfileId, UpdateAthleteProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista entrenadores de la organización activa.</summary>
    Task<AppResult> ListTrainersAsync(CancellationToken cancellationToken = default);

    /// <summary>Actualiza el perfil de un entrenador.</summary>
    Task<AppResult> UpdateTrainerAsync(Guid trainerProfileId, UpdateTrainerProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Asigna un alumno a un entrenador.</summary>
    Task<AppResult> AssignAthleteAsync(AssignAthleteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Finaliza una asignación entrenador–alumno.</summary>
    Task<AppResult> UnassignAthleteAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Lista objetivos de un alumno.</summary>
    Task<AppResult> ListGoalsAsync(Guid athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Crea un objetivo para un alumno.</summary>
    Task<AppResult> CreateGoalAsync(CreateAthleteGoalRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marca un objetivo como completado.</summary>
    Task<AppResult> CompleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>Cancela un objetivo.</summary>
    Task<AppResult> CancelGoalAsync(Guid goalId, CancellationToken cancellationToken = default);
}
