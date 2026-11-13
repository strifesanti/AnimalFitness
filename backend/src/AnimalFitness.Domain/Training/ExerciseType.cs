namespace AnimalFitness.Domain.Training;

/// <summary>Clasificación general del ejercicio.</summary>
public enum ExerciseType
{
    /// <summary>Gimnasio / pesas / máquinas.</summary>
    Gym = 1,

    /// <summary>Calistenia / peso corporal.</summary>
    Calisthenics = 2,

    /// <summary>Cardio / resistencia.</summary>
    Cardio = 3,

    /// <summary>Movilidad u otro.</summary>
    Other = 4,
}
