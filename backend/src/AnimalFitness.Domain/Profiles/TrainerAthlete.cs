using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Profiles;

/// <summary>
/// Asignación de un alumno a un entrenador dentro de la misma organización.
/// </summary>
public sealed class TrainerAthlete : TenantScopedEntity
{
    private TrainerAthlete(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Perfil del entrenador.</summary>
    public Guid TrainerProfileId { get; private set; }

    /// <summary>Perfil del alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Momento de la asignación, en UTC.</summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>Estado.</summary>
    public TrainerAthleteAssignmentStatus Status { get; private set; }

    /// <summary>
    /// Crea una asignación activa.
    /// </summary>
    public static TrainerAthlete Create(
        Guid tenantId,
        Guid trainerProfileId,
        Guid athleteProfileId,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (trainerProfileId == athleteProfileId)
        {
            throw new DomainException("El entrenador y el alumno no pueden ser el mismo perfil.");
        }

        var assignment = new TrainerAthlete(tenantId)
        {
            TrainerProfileId = TextRules.RequiredId(trainerProfileId, "El entrenador"),
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            AssignedAt = now,
            Status = TrainerAthleteAssignmentStatus.Active,
        };
        assignment.MarkCreated(now, createdBy);
        return assignment;
    }

    /// <summary>Finaliza la asignación.</summary>
    public void End(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != TrainerAthleteAssignmentStatus.Active)
        {
            throw new DomainException("La asignación ya no está activa.");
        }

        MarkUpdated(now, actor);
        Status = TrainerAthleteAssignmentStatus.Ended;
    }
}
