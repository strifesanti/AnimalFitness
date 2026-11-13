using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Attendance;

/// <summary>Registro de asistencia de un alumno en una fecha.</summary>
public sealed class AttendanceRecord : TenantScopedEntity
{
    private AttendanceRecord(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Fecha de la sesión/día.</summary>
    public DateOnly AttendanceDate { get; private set; }

    /// <summary>Estado.</summary>
    public AttendanceStatus Status { get; private set; }

    /// <summary>Notas.</summary>
    public string? Notes { get; private set; }

    /// <summary>Quién registró.</summary>
    public Guid RecordedByUserId { get; private set; }

    /// <summary>Crea un registro.</summary>
    public static AttendanceRecord Create(
        Guid tenantId,
        Guid athleteProfileId,
        DateOnly attendanceDate,
        AttendanceStatus status,
        Guid recordedByUserId,
        DateTimeOffset now,
        string? notes = null)
    {
        var record = new AttendanceRecord(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            AttendanceDate = attendanceDate,
            Status = TextRules.Defined(status, "El estado"),
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : TextRules.Required(notes, "Las notas", FieldLengths.ProgressNotes),
            RecordedByUserId = TextRules.RequiredId(recordedByUserId, "Quien registra"),
        };
        record.MarkCreated(now, recordedByUserId);
        return record;
    }

    /// <summary>Actualiza estado y notas.</summary>
    public void Update(AttendanceStatus status, string? notes, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        Status = TextRules.Defined(status, "El estado");
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : TextRules.Required(notes, "Las notas", FieldLengths.ProgressNotes);
        MarkUpdated(now, actor);
    }
}
