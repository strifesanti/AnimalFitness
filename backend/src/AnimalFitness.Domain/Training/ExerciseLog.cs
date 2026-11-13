using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Registro de una serie ejecutada en una sesión.</summary>
public sealed class ExerciseLog : TenantScopedEntity
{
    private ExerciseLog(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Sesión.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Prescripción del plan, si aplica.</summary>
    public Guid? WorkoutExerciseId { get; private set; }

    /// <summary>Ejercicio de la biblioteca.</summary>
    public Guid ExerciseId { get; private set; }

    /// <summary>Número de serie (1-based).</summary>
    public int SetNumber { get; private set; }

    /// <summary>Repeticiones realizadas.</summary>
    public int? Reps { get; private set; }

    /// <summary>Peso en kg.</summary>
    public decimal? WeightKg { get; private set; }

    /// <summary>Duración en segundos.</summary>
    public int? DurationSeconds { get; private set; }

    /// <summary>Distancia en metros.</summary>
    public decimal? DistanceMeters { get; private set; }

    /// <summary>Esfuerzo percibido (1-10).</summary>
    public int? Rpe { get; private set; }

    /// <summary>Notas.</summary>
    public string? Notes { get; private set; }

    /// <summary>Momento del registro.</summary>
    public DateTimeOffset LoggedAt { get; private set; }

    /// <summary>Registra una serie.</summary>
    public static ExerciseLog Create(
        Guid tenantId,
        Guid sessionId,
        Guid exerciseId,
        int setNumber,
        DateTimeOffset now,
        Guid? createdBy,
        Guid? workoutExerciseId = null,
        int? reps = null,
        decimal? weightKg = null,
        int? durationSeconds = null,
        decimal? distanceMeters = null,
        int? rpe = null,
        string? notes = null)
    {
        if (setNumber < 1)
        {
            throw new DomainException("El número de serie debe ser al menos 1.");
        }

        if (rpe is < 1 or > 10)
        {
            throw new DomainException("El RPE debe estar entre 1 y 10.");
        }

        var log = new ExerciseLog(tenantId)
        {
            SessionId = TextRules.RequiredId(sessionId, "La sesión"),
            WorkoutExerciseId = workoutExerciseId,
            ExerciseId = TextRules.RequiredId(exerciseId, "El ejercicio"),
            SetNumber = setNumber,
            Reps = reps,
            WeightKg = weightKg,
            DurationSeconds = durationSeconds,
            DistanceMeters = distanceMeters,
            Rpe = rpe,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : TextRules.Required(notes, "Las notas", FieldLengths.WorkoutNotes),
            LoggedAt = now,
        };
        log.MarkCreated(now, createdBy);
        return log;
    }
}
