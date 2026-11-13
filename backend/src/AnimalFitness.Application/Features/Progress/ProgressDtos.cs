using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Tracking;

namespace AnimalFitness.Application.Features.Progress;

// ----- Mediciones -----

/// <summary>Medición corporal con el IMC calculado.</summary>
public sealed record BodyMeasurementDto(
    Guid Id,
    Guid AthleteProfileId,
    DateTimeOffset MeasuredAt,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BodyFatPercent,
    decimal? MuscleMassKg,
    decimal? WaistCm,
    decimal? ChestCm,
    decimal? ArmCm,
    decimal? LegCm,
    decimal? HipCm,
    decimal? Bmi,
    string? Notes,
    Guid RecordedByUserId);

/// <summary>Registrar o actualizar una medición. Si no se indica la fecha se usa el momento actual.</summary>
public sealed record SaveBodyMeasurementRequest(
    Guid? AthleteProfileId,
    DateTimeOffset? MeasuredAt,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BodyFatPercent,
    decimal? MuscleMassKg,
    decimal? WaistCm,
    decimal? ChestCm,
    decimal? ArmCm,
    decimal? LegCm,
    decimal? HipCm,
    string? Notes);

// ----- Fotos -----

/// <summary>Foto de progreso.</summary>
public sealed record ProgressPhotoDto(
    Guid Id,
    Guid AthleteProfileId,
    DateTimeOffset TakenAt,
    PhotoPose Pose,
    string Url,
    string? Caption,
    string? Notes,
    Guid RecordedByUserId);

/// <summary>Registrar o actualizar una foto (solo URL, sin subida binaria).</summary>
public sealed record SaveProgressPhotoRequest(
    Guid? AthleteProfileId,
    DateTimeOffset? TakenAt,
    PhotoPose Pose,
    string Url,
    string? Caption,
    string? Notes);

// ----- Valoraciones -----

/// <summary>Valoración física.</summary>
public sealed record FitnessAssessmentDto(
    Guid Id,
    Guid AthleteProfileId,
    DateTimeOffset AssessedAt,
    string Title,
    string Summary,
    int? Score,
    Guid AssessedByUserId);

/// <summary>Registrar o actualizar una valoración.</summary>
public sealed record SaveFitnessAssessmentRequest(
    Guid? AthleteProfileId,
    DateTimeOffset? AssessedAt,
    string Title,
    string Summary,
    int? Score);

// ----- Objetivos -----

/// <summary>Objetivo de progreso del alumno.</summary>
public sealed record ProgressGoalDto(
    Guid Id,
    Guid AthleteProfileId,
    string Title,
    string? Description,
    decimal? TargetValue,
    string? Unit,
    DateOnly? DueDate,
    AthleteGoalStatus Status);

/// <summary>Crear un objetivo de progreso.</summary>
public sealed record CreateProgressGoalRequest(
    Guid? AthleteProfileId,
    string Title,
    string? Description,
    decimal? TargetValue,
    string? Unit,
    DateOnly? DueDate);

// ----- Gráficos -----

/// <summary>Punto de una serie temporal.</summary>
public sealed record ChartPointDto(DateTimeOffset At, decimal Value);

/// <summary>Series temporales de progreso listas para graficar.</summary>
public sealed record ProgressChartsDto(
    Guid AthleteProfileId,
    IReadOnlyList<ChartPointDto> WeightKg,
    IReadOnlyList<ChartPointDto> BodyFatPercent,
    IReadOnlyList<ChartPointDto> WaistCm,
    IReadOnlyList<ChartPointDto> Bmi);
