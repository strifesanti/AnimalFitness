namespace AnimalFitness.Domain.Attendance;

/// <summary>Estado de asistencia del día.</summary>
public enum AttendanceStatus
{
    /// <summary>Presente.</summary>
    Present = 1,

    /// <summary>Ausente.</summary>
    Absent = 2,

    /// <summary>Justificado.</summary>
    Excused = 3,

    /// <summary>Tarde.</summary>
    Late = 4,
}
