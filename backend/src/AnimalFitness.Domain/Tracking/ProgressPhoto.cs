using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Tracking;

/// <summary>
/// Fotografía de evolución. La URL apunta a un recurso externo o a almacenamiento futuro;
/// no se sube el binario en esta fase.
/// </summary>
public sealed class ProgressPhoto : TenantScopedEntity
{
    private ProgressPhoto(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Momento de la foto (UTC).</summary>
    public DateTimeOffset TakenAt { get; private set; }

    /// <summary>Pose / ángulo.</summary>
    public PhotoPose Pose { get; private set; }

    /// <summary>URL de la imagen.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>Leyenda opcional.</summary>
    public string? Caption { get; private set; }

    /// <summary>Notas.</summary>
    public string? Notes { get; private set; }

    /// <summary>Quién registró la foto.</summary>
    public Guid RecordedByUserId { get; private set; }

    /// <summary>Registra una foto de progreso.</summary>
    public static ProgressPhoto Create(
        Guid tenantId,
        Guid athleteProfileId,
        DateTimeOffset takenAt,
        PhotoPose pose,
        string url,
        Guid recordedByUserId,
        DateTimeOffset now,
        string? caption = null,
        string? notes = null)
    {
        var photo = new ProgressPhoto(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            TakenAt = takenAt,
            Pose = TextRules.Defined(pose, "La pose"),
            Url = TextRules.Required(url, "La URL", FieldLengths.MediaUrl),
            Caption = string.IsNullOrWhiteSpace(caption)
                ? null
                : TextRules.Required(caption, "La leyenda", FieldLengths.ProgressNotes),
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : TextRules.Required(notes, "Las notas", FieldLengths.ProgressNotes),
            RecordedByUserId = TextRules.RequiredId(recordedByUserId, "Quien registra"),
        };
        photo.MarkCreated(now, recordedByUserId);
        return photo;
    }

    /// <summary>Actualiza metadatos de la foto.</summary>
    public void Update(
        DateTimeOffset takenAt,
        PhotoPose pose,
        string url,
        string? caption,
        string? notes,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        TakenAt = takenAt;
        Pose = TextRules.Defined(pose, "La pose");
        Url = TextRules.Required(url, "La URL", FieldLengths.MediaUrl);
        Caption = string.IsNullOrWhiteSpace(caption)
            ? null
            : TextRules.Required(caption, "La leyenda", FieldLengths.ProgressNotes);
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : TextRules.Required(notes, "Las notas", FieldLengths.ProgressNotes);
        MarkUpdated(now, actor);
    }
}
