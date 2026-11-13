using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Imagen o video de un ejercicio.</summary>
public sealed class ExerciseMedia : TenantScopedEntity
{
    private ExerciseMedia(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Ejercicio dueño.</summary>
    public Guid ExerciseId { get; private set; }

    /// <summary>Tipo de medio.</summary>
    public ExerciseMediaType MediaType { get; private set; }

    /// <summary>URL del recurso.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>Orden.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Leyenda opcional.</summary>
    public string? Caption { get; private set; }

    /// <summary>Crea un medio.</summary>
    public static ExerciseMedia Create(
        Guid tenantId,
        Guid exerciseId,
        ExerciseMediaType mediaType,
        string url,
        DateTimeOffset now,
        Guid? createdBy,
        int sortOrder = 0,
        string? caption = null)
    {
        var media = new ExerciseMedia(tenantId)
        {
            ExerciseId = TextRules.RequiredId(exerciseId, "El ejercicio"),
            MediaType = TextRules.Defined(mediaType, "El tipo de medio"),
            Url = TextRules.Required(url, "La URL", FieldLengths.MediaUrl),
            SortOrder = sortOrder,
            Caption = string.IsNullOrWhiteSpace(caption)
                ? null
                : TextRules.Required(caption, "La leyenda", FieldLengths.WorkoutNotes),
        };
        media.MarkCreated(now, createdBy);
        return media;
    }
}
