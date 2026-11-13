using AnimalFitness.Domain.Attendance;

namespace AnimalFitness.Application.Features.Attendance;

/// <summary>Registro de asistencia de un alumno en una fecha.</summary>
public sealed record AttendanceRecordDto(
    Guid Id,
    Guid AthleteProfileId,
    string AthleteName,
    DateOnly Date,
    AttendanceStatus Status,
    string? Notes,
    Guid RecordedByUserId);

/// <summary>Crear o actualizar la asistencia de un alumno en una fecha (una por alumno y día).</summary>
public sealed record UpsertAttendanceRequest(
    Guid AthleteProfileId,
    DateOnly Date,
    AttendanceStatus Status,
    string? Notes);

/// <summary>Estadísticas de asistencia en un rango. <c>AthleteProfileId</c> es nulo para el total de la organización.</summary>
public sealed record AttendanceStatsDto(
    Guid? AthleteProfileId,
    DateOnly From,
    DateOnly To,
    int Total,
    int Present,
    int Late,
    int Absent,
    int Excused,
    decimal PresentPercent,
    decimal AttendedPercent);
