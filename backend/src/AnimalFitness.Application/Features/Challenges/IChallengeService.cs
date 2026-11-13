using AnimalFitness.Application.Common;
using AnimalFitness.Domain.Challenges;

namespace AnimalFitness.Application.Features.Challenges;

/// <summary>
/// Retos, participación, puntos, clasificación y logros.
/// Owner y Trainer gestionan retos y puntos; los alumnos consultan, se inscriben y ven la clasificación.
/// </summary>
public interface IChallengeService
{
    /// <summary>Lista los retos de la organización, opcionalmente por estado.</summary>
    Task<AppResult> ListAsync(ChallengeStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un reto.</summary>
    Task<AppResult> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Crea un reto en borrador.</summary>
    Task<AppResult> CreateAsync(CreateChallengeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Activa un reto en borrador.</summary>
    Task<AppResult> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Finaliza un reto activo y reconoce a quien lidera la clasificación.</summary>
    Task<AppResult> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cancela un reto no cerrado.</summary>
    Task<AppResult> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inscribe al alumno en un reto activo.</summary>
    Task<AppResult> JoinAsync(Guid id, JoinChallengeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista las actividades registradas en el reto (más recientes primero).</summary>
    Task<AppResult> ListActivitiesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Registra una actividad y suma sus puntos al participante (reto activo).</summary>
    Task<AppResult> AddActivityAsync(Guid id, AddChallengeActivityRequest request, CancellationToken cancellationToken = default);

    /// <summary>Clasificación del reto ordenada por puntos de mayor a menor.</summary>
    Task<AppResult> GetLeaderboardAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista logros. Los alumnos ven los suyos; Owner y Trainer pueden filtrar por alumno.</summary>
    Task<AppResult> ListAchievementsAsync(Guid? athleteProfileId, CancellationToken cancellationToken = default);

    /// <summary>Otorga un logro a un alumno.</summary>
    Task<AppResult> AwardAchievementAsync(AwardAchievementRequest request, CancellationToken cancellationToken = default);
}
