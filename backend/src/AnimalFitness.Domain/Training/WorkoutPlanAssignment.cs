using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Asignación de un plan publicado a un alumno.</summary>
public sealed class WorkoutPlanAssignment : TenantScopedEntity
{
    private WorkoutPlanAssignment(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Plan asignado.</summary>
    public Guid PlanId { get; private set; }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Momento de la asignación.</summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>Estado.</summary>
    public PlanAssignmentStatus Status { get; private set; }

    /// <summary>Crea una asignación activa.</summary>
    public static WorkoutPlanAssignment Create(
        Guid tenantId,
        Guid planId,
        Guid athleteProfileId,
        DateTimeOffset now,
        Guid? createdBy)
    {
        var assignment = new WorkoutPlanAssignment(tenantId)
        {
            PlanId = TextRules.RequiredId(planId, "El plan"),
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            AssignedAt = now,
            Status = PlanAssignmentStatus.Active,
        };
        assignment.MarkCreated(now, createdBy);
        return assignment;
    }

    /// <summary>Finaliza la asignación.</summary>
    public void End(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != PlanAssignmentStatus.Active)
        {
            throw new DomainException("La asignación ya no está activa.");
        }

        MarkUpdated(now, actor);
        Status = PlanAssignmentStatus.Ended;
    }
}
