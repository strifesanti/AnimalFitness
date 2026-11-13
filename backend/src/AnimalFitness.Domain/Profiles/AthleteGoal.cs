using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Profiles;

/// <summary>
/// Objetivo físico o de rendimiento de un alumno.
/// </summary>
public sealed class AthleteGoal : TenantScopedEntity
{
    private AthleteGoal(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Perfil del alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Título corto.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Detalle opcional.</summary>
    public string? Description { get; private set; }

    /// <summary>Valor objetivo numérico opcional.</summary>
    public decimal? TargetValue { get; private set; }

    /// <summary>Unidad del valor objetivo.</summary>
    public string? Unit { get; private set; }

    /// <summary>Fecha límite opcional.</summary>
    public DateOnly? DueDate { get; private set; }

    /// <summary>Estado.</summary>
    public AthleteGoalStatus Status { get; private set; }

    /// <summary>
    /// Crea un objetivo activo.
    /// </summary>
    public static AthleteGoal Create(
        Guid tenantId,
        Guid athleteProfileId,
        string title,
        DateTimeOffset now,
        Guid? createdBy,
        string? description = null,
        decimal? targetValue = null,
        string? unit = null,
        DateOnly? dueDate = null)
    {
        var goal = new AthleteGoal(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            Title = TextRules.Required(title, "El título", FieldLengths.GoalTitle),
            Description = string.IsNullOrWhiteSpace(description)
                ? null
                : TextRules.Required(description, "La descripción", FieldLengths.GoalDescription),
            TargetValue = targetValue,
            Unit = string.IsNullOrWhiteSpace(unit)
                ? null
                : TextRules.Required(unit, "La unidad", FieldLengths.GoalUnit),
            DueDate = dueDate,
            Status = AthleteGoalStatus.Active,
        };
        goal.MarkCreated(now, createdBy);
        return goal;
    }

    /// <summary>Marca el objetivo como cumplido.</summary>
    public void Complete(DateTimeOffset now, Guid? actor)
    {
        EnsureActive();
        MarkUpdated(now, actor);
        Status = AthleteGoalStatus.Completed;
    }

    /// <summary>Cancela el objetivo.</summary>
    public void Cancel(DateTimeOffset now, Guid? actor)
    {
        EnsureActive();
        MarkUpdated(now, actor);
        Status = AthleteGoalStatus.Cancelled;
    }

    private void EnsureActive()
    {
        EnsureNotDeleted();
        if (Status != AthleteGoalStatus.Active)
        {
            throw new DomainException("El objetivo ya no está activo.");
        }
    }
}
