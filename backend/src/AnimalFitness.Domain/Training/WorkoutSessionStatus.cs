namespace AnimalFitness.Domain.Training;

/// <summary>Estado de una sesión de entrenamiento.</summary>
public enum WorkoutSessionStatus
{
    /// <summary>Sesión en curso.</summary>
    InProgress = 1,

    /// <summary>Sesión finalizada.</summary>
    Completed = 2,

    /// <summary>Sesión cancelada.</summary>
    Cancelled = 3,
}
