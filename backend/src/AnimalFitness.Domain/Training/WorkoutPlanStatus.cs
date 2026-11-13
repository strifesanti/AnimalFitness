namespace AnimalFitness.Domain.Training;

/// <summary>Estado de publicación de un plan de entrenamiento.</summary>
public enum WorkoutPlanStatus
{
    /// <summary>Editable; aún no asignable como vigente.</summary>
    Draft = 1,

    /// <summary>Publicado. Las ediciones estructurales deben versionarse.</summary>
    Published = 2,

    /// <summary>Archivado; no se asigna a nuevos alumnos.</summary>
    Archived = 3,
}
