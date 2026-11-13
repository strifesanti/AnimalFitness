using AnimalFitness.Domain.Training;

namespace AnimalFitness.Application.Features.Training;

// ----- Ejercicios -----

/// <summary>Categoría de ejercicios.</summary>
public sealed record ExerciseCategoryDto(Guid Id, string Name, string? Description, int SortOrder);

/// <summary>Crear una categoría de ejercicios.</summary>
public sealed record CreateExerciseCategoryRequest(string Name, string? Description, int SortOrder = 0);

/// <summary>Actualizar una categoría de ejercicios.</summary>
public sealed record UpdateExerciseCategoryRequest(string Name, string? Description, int SortOrder = 0);

/// <summary>Imagen o video de un ejercicio.</summary>
public sealed record ExerciseMediaDto(
    Guid Id,
    Guid ExerciseId,
    ExerciseMediaType MediaType,
    string Url,
    int SortOrder,
    string? Caption);

/// <summary>Ejercicio de la biblioteca con sus medios.</summary>
public sealed record ExerciseDto(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string? Description,
    string PrimaryMuscle,
    string? SecondaryMuscles,
    ExerciseType Type,
    string? Equipment,
    ExerciseDifficulty Difficulty,
    string? Instructions,
    Guid? VariationOfExerciseId,
    IReadOnlyList<ExerciseMediaDto> Media);

/// <summary>Crear o actualizar un ejercicio.</summary>
public sealed record SaveExerciseRequest(
    string Name,
    string PrimaryMuscle,
    ExerciseType Type,
    ExerciseDifficulty Difficulty,
    Guid? CategoryId = null,
    string? Description = null,
    string? SecondaryMuscles = null,
    string? Equipment = null,
    string? Instructions = null,
    Guid? VariationOfExerciseId = null);

/// <summary>Agregar un medio a un ejercicio.</summary>
public sealed record AddExerciseMediaRequest(
    ExerciseMediaType MediaType,
    string Url,
    int SortOrder = 0,
    string? Caption = null);

// ----- Planes de entrenamiento -----

/// <summary>Resumen de un plan de entrenamiento.</summary>
public sealed record WorkoutPlanSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string? GoalLabel,
    int Version,
    Guid? ParentPlanId,
    WorkoutPlanStatus Status,
    int DayCount);

/// <summary>Ejercicio prescrito en un día.</summary>
public sealed record WorkoutExerciseDto(
    Guid Id,
    Guid DayId,
    Guid ExerciseId,
    string ExerciseName,
    int SortOrder,
    int TargetSets,
    string? TargetReps,
    decimal? TargetWeightKg,
    int? TargetDurationSeconds,
    decimal? TargetDistanceMeters,
    int? RestSeconds,
    string? Tempo,
    string? Notes);

/// <summary>Día de un plan con sus ejercicios.</summary>
public sealed record WorkoutDayDto(
    Guid Id,
    Guid PlanId,
    int DayIndex,
    string Name,
    string? Notes,
    IReadOnlyList<WorkoutExerciseDto> Exercises);

/// <summary>Detalle de un plan con días y ejercicios.</summary>
public sealed record WorkoutPlanDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? GoalLabel,
    int Version,
    Guid? ParentPlanId,
    WorkoutPlanStatus Status,
    IReadOnlyList<WorkoutDayDto> Days);

/// <summary>Crear un plan en borrador.</summary>
public sealed record CreateWorkoutPlanRequest(string Name, string? Description = null, string? GoalLabel = null);

/// <summary>Actualizar metadatos de un plan en borrador.</summary>
public sealed record UpdateWorkoutPlanRequest(string Name, string? Description = null, string? GoalLabel = null);

/// <summary>Agregar un día a un plan en borrador.</summary>
public sealed record CreateWorkoutDayRequest(string Name, string? Notes = null, int? DayIndex = null);

/// <summary>Actualizar un día de un plan en borrador.</summary>
public sealed record UpdateWorkoutDayRequest(string Name, string? Notes = null);

/// <summary>Agregar un ejercicio a un día de un plan en borrador.</summary>
public sealed record AddWorkoutExerciseRequest(
    Guid ExerciseId,
    int TargetSets,
    int? SortOrder = null,
    string? TargetReps = null,
    decimal? TargetWeightKg = null,
    int? TargetDurationSeconds = null,
    decimal? TargetDistanceMeters = null,
    int? RestSeconds = null,
    string? Tempo = null,
    string? Notes = null);

/// <summary>Actualizar la prescripción de un ejercicio de un plan en borrador.</summary>
public sealed record UpdateWorkoutExerciseRequest(
    int SortOrder,
    int TargetSets,
    string? TargetReps = null,
    decimal? TargetWeightKg = null,
    int? TargetDurationSeconds = null,
    decimal? TargetDistanceMeters = null,
    int? RestSeconds = null,
    string? Tempo = null,
    string? Notes = null);

/// <summary>Asignar un plan publicado a un alumno.</summary>
public sealed record AssignWorkoutRequest(Guid AthleteProfileId);

/// <summary>Asignación de un plan a un alumno.</summary>
public sealed record WorkoutAssignmentDto(
    Guid Id,
    Guid PlanId,
    Guid AthleteProfileId,
    DateTimeOffset AssignedAt,
    PlanAssignmentStatus Status);

// ----- Sesiones -----

/// <summary>Sesión de entrenamiento.</summary>
public sealed record WorkoutSessionDto(
    Guid Id,
    Guid AthleteProfileId,
    Guid? PlanId,
    Guid? DayId,
    WorkoutSessionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? Notes);

/// <summary>Iniciar una sesión.</summary>
public sealed record StartSessionRequest(
    Guid? AthleteProfileId = null,
    Guid? PlanId = null,
    Guid? DayId = null,
    string? Notes = null);

/// <summary>Completar una sesión.</summary>
public sealed record CompleteSessionRequest(string? Notes = null);

/// <summary>Registrar una serie ejecutada.</summary>
public sealed record LogSetRequest(
    Guid ExerciseId,
    int SetNumber,
    Guid? WorkoutExerciseId = null,
    int? Reps = null,
    decimal? WeightKg = null,
    int? DurationSeconds = null,
    decimal? DistanceMeters = null,
    int? Rpe = null,
    string? Notes = null);

/// <summary>Serie registrada en una sesión.</summary>
public sealed record ExerciseLogDto(
    Guid Id,
    Guid SessionId,
    Guid? WorkoutExerciseId,
    Guid ExerciseId,
    int SetNumber,
    int? Reps,
    decimal? WeightKg,
    int? DurationSeconds,
    decimal? DistanceMeters,
    int? Rpe,
    string? Notes,
    DateTimeOffset LoggedAt);
