using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>
/// Sesión ejecutada por un alumno. Conserva referencias al plan/día de la versión usada;
/// no se altera si el entrenador publica una versión nueva del plan.
/// </summary>
public sealed class WorkoutSession : TenantScopedEntity
{
    private WorkoutSession(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno que entrena.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Plan de la versión usada (opcional si es sesión libre).</summary>
    public Guid? PlanId { get; private set; }

    /// <summary>Día del plan (opcional).</summary>
    public Guid? DayId { get; private set; }

    /// <summary>Estado.</summary>
    public WorkoutSessionStatus Status { get; private set; }

    /// <summary>Inicio.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Fin.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Notas de la sesión.</summary>
    public string? Notes { get; private set; }

    /// <summary>Inicia una sesión.</summary>
    public static WorkoutSession Start(
        Guid tenantId,
        Guid athleteProfileId,
        DateTimeOffset now,
        Guid? createdBy,
        Guid? planId = null,
        Guid? dayId = null,
        string? notes = null)
    {
        var session = new WorkoutSession(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            PlanId = planId,
            DayId = dayId,
            Status = WorkoutSessionStatus.InProgress,
            StartedAt = now,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : TextRules.Required(notes, "Las notas", FieldLengths.WorkoutNotes),
        };
        session.MarkCreated(now, createdBy);
        return session;
    }

    /// <summary>Completa la sesión.</summary>
    public void Complete(DateTimeOffset now, Guid? actor, string? notes = null)
    {
        EnsureInProgress();
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = TextRules.Required(notes, "Las notas", FieldLengths.WorkoutNotes);
        }

        MarkUpdated(now, actor);
        Status = WorkoutSessionStatus.Completed;
        CompletedAt = now;
    }

    /// <summary>Cancela la sesión.</summary>
    public void Cancel(DateTimeOffset now, Guid? actor)
    {
        EnsureInProgress();
        MarkUpdated(now, actor);
        Status = WorkoutSessionStatus.Cancelled;
        CompletedAt = now;
    }

    private void EnsureInProgress()
    {
        EnsureNotDeleted();
        if (Status != WorkoutSessionStatus.InProgress)
        {
            throw new DomainException("La sesión ya no está en curso.");
        }
    }
}
