using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Attendance;

/// <summary>
/// Asistencia de alumnos. Los alumnos solo consultan la suya; Owner y Trainer registran y consultan toda la organización.
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Lista registros entre dos fechas (últimos 30 días por defecto).
    /// Sin <paramref name="athleteProfileId"/>, Owner y Trainer reciben a todos los alumnos.
    /// </summary>
    Task<AppResult> ListAsync(
        Guid? athleteProfileId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    /// <summary>Crea o actualiza el registro de un alumno en una fecha (solo Owner y Trainer).</summary>
    Task<AppResult> UpsertAsync(UpsertAttendanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Estadísticas de asistencia (porcentaje de presentes) en un rango.</summary>
    Task<AppResult> GetStatsAsync(
        Guid? athleteProfileId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
