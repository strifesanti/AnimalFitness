namespace AnimalFitness.Domain.Profiles;

/// <summary>Estado de la asignación entrenador–alumno.</summary>
public enum TrainerAthleteAssignmentStatus
{
    /// <summary>Asignación vigente.</summary>
    Active = 1,

    /// <summary>Asignación finalizada.</summary>
    Ended = 2,
}
