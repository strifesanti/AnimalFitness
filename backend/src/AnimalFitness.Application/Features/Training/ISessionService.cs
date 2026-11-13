using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Training;

/// <summary>Sesiones de entrenamiento y registro de series.</summary>
public interface ISessionService
{
    /// <summary>Lista sesiones. Los alumnos solo ven las propias.</summary>
    Task<AppResult> ListSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Inicia una sesión para el alumno indicado (o el propio si es alumno).</summary>
    Task<AppResult> StartAsync(StartSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Completa una sesión en curso.</summary>
    Task<AppResult> CompleteAsync(Guid sessionId, CompleteSessionRequest? request, CancellationToken cancellationToken = default);

    /// <summary>Cancela una sesión en curso.</summary>
    Task<AppResult> CancelAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Registra una serie en una sesión en curso.</summary>
    Task<AppResult> LogSetAsync(Guid sessionId, LogSetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista las series registradas de una sesión.</summary>
    Task<AppResult> ListLogsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
