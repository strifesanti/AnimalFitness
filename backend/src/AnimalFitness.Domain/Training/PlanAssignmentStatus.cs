namespace AnimalFitness.Domain.Training;

/// <summary>Estado de la asignación de un plan a un alumno.</summary>
public enum PlanAssignmentStatus
{
    /// <summary>Asignación vigente.</summary>
    Active = 1,

    /// <summary>Asignación finalizada.</summary>
    Ended = 2,
}
