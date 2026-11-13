using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Ejercicio prescrito en un día de plan (plantilla, no registro histórico de sesión).</summary>
public sealed class WorkoutExercise : TenantScopedEntity
{
    private WorkoutExercise(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Día del plan.</summary>
    public Guid DayId { get; private set; }

    /// <summary>Ejercicio de la biblioteca.</summary>
    public Guid ExerciseId { get; private set; }

    /// <summary>Orden en el día.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Series objetivo.</summary>
    public int TargetSets { get; private set; }

    /// <summary>Repeticiones objetivo (texto libre: 8-12, AMRAP…).</summary>
    public string? TargetReps { get; private set; }

    /// <summary>Peso objetivo en kg.</summary>
    public decimal? TargetWeightKg { get; private set; }

    /// <summary>Duración objetivo en segundos.</summary>
    public int? TargetDurationSeconds { get; private set; }

    /// <summary>Distancia objetivo en metros.</summary>
    public decimal? TargetDistanceMeters { get; private set; }

    /// <summary>Descanso entre series en segundos.</summary>
    public int? RestSeconds { get; private set; }

    /// <summary>Tempo.</summary>
    public string? Tempo { get; private set; }

    /// <summary>Observaciones.</summary>
    public string? Notes { get; private set; }

    /// <summary>Crea la prescripción.</summary>
    public static WorkoutExercise Create(
        Guid tenantId,
        Guid dayId,
        Guid exerciseId,
        int sortOrder,
        int targetSets,
        DateTimeOffset now,
        Guid? createdBy,
        string? targetReps = null,
        decimal? targetWeightKg = null,
        int? targetDurationSeconds = null,
        decimal? targetDistanceMeters = null,
        int? restSeconds = null,
        string? tempo = null,
        string? notes = null)
    {
        var item = new WorkoutExercise(tenantId)
        {
            DayId = TextRules.RequiredId(dayId, "El día"),
            ExerciseId = TextRules.RequiredId(exerciseId, "El ejercicio"),
        };
        item.Apply(
            sortOrder,
            targetSets,
            targetReps,
            targetWeightKg,
            targetDurationSeconds,
            targetDistanceMeters,
            restSeconds,
            tempo,
            notes);
        item.MarkCreated(now, createdBy);
        return item;
    }

    /// <summary>Actualiza la prescripción.</summary>
    public void Update(
        int sortOrder,
        int targetSets,
        string? targetReps,
        decimal? targetWeightKg,
        int? targetDurationSeconds,
        decimal? targetDistanceMeters,
        int? restSeconds,
        string? tempo,
        string? notes,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        Apply(
            sortOrder,
            targetSets,
            targetReps,
            targetWeightKg,
            targetDurationSeconds,
            targetDistanceMeters,
            restSeconds,
            tempo,
            notes);
        MarkUpdated(now, actor);
    }

    private void Apply(
        int sortOrder,
        int targetSets,
        string? targetReps,
        decimal? targetWeightKg,
        int? targetDurationSeconds,
        decimal? targetDistanceMeters,
        int? restSeconds,
        string? tempo,
        string? notes)
    {
        if (targetSets < 1)
        {
            throw new DomainException("Debe haber al menos una serie.");
        }

        SortOrder = sortOrder;
        TargetSets = targetSets;
        TargetReps = string.IsNullOrWhiteSpace(targetReps)
            ? null
            : TextRules.Required(targetReps, "Las repeticiones", 40);
        TargetWeightKg = targetWeightKg;
        TargetDurationSeconds = targetDurationSeconds;
        TargetDistanceMeters = targetDistanceMeters;
        RestSeconds = restSeconds;
        Tempo = string.IsNullOrWhiteSpace(tempo)
            ? null
            : TextRules.Required(tempo, "El tempo", FieldLengths.Tempo);
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : TextRules.Required(notes, "Las notas", FieldLengths.WorkoutNotes);
    }
}
