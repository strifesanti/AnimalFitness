using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Día dentro de un plan de entrenamiento.</summary>
public sealed class WorkoutDay : TenantScopedEntity
{
    private WorkoutDay(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Plan dueño.</summary>
    public Guid PlanId { get; private set; }

    /// <summary>Índice del día (1-based).</summary>
    public int DayIndex { get; private set; }

    /// <summary>Nombre visible (Lunes, Día A, etc.).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Notas del día.</summary>
    public string? Notes { get; private set; }

    /// <summary>Crea un día del plan.</summary>
    public static WorkoutDay Create(
        Guid tenantId,
        Guid planId,
        int dayIndex,
        string name,
        DateTimeOffset now,
        Guid? createdBy,
        string? notes = null)
    {
        if (dayIndex < 1)
        {
            throw new DomainException("El índice del día debe ser al menos 1.");
        }

        var day = new WorkoutDay(tenantId)
        {
            PlanId = TextRules.RequiredId(planId, "El plan"),
            DayIndex = dayIndex,
            Name = TextRules.Required(name, "El nombre", FieldLengths.WorkoutName),
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : TextRules.Required(notes, "Las notas", FieldLengths.WorkoutNotes),
        };
        day.MarkCreated(now, createdBy);
        return day;
    }

    /// <summary>Actualiza nombre y notas.</summary>
    public void Update(string name, string? notes, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        Name = TextRules.Required(name, "El nombre", FieldLengths.WorkoutName);
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : TextRules.Required(notes, "Las notas", FieldLengths.WorkoutNotes);
        MarkUpdated(now, actor);
    }
}
