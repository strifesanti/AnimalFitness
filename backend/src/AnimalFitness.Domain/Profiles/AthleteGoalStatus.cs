namespace AnimalFitness.Domain.Profiles;

/// <summary>Estado de un objetivo del alumno.</summary>
public enum AthleteGoalStatus
{
    /// <summary>Objetivo en curso.</summary>
    Active = 1,

    /// <summary>Objetivo cumplido.</summary>
    Completed = 2,

    /// <summary>Objetivo cancelado.</summary>
    Cancelled = 3,
}
