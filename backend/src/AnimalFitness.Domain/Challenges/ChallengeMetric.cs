namespace AnimalFitness.Domain.Challenges;

/// <summary>Métrica con la que se puntúa el reto.</summary>
public enum ChallengeMetric
{
    /// <summary>Número de entrenamientos completados.</summary>
    WorkoutsCompleted = 1,

    /// <summary>Días de asistencia.</summary>
    AttendanceDays = 2,

    /// <summary>Puntos manuales / actividades registradas.</summary>
    ManualPoints = 3,
}
