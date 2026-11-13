using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Tracking;

/// <summary>
/// Valoración física puntual (fuerza, movilidad u observación cualitativa).
/// Complementa las mediciones numéricas y las fotos.
/// </summary>
public sealed class FitnessAssessment : TenantScopedEntity
{
    private FitnessAssessment(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno evaluado.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Momento de la valoración (UTC).</summary>
    public DateTimeOffset AssessedAt { get; private set; }

    /// <summary>Título corto.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Resumen o hallazgos.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Puntuación opcional (1-10).</summary>
    public int? Score { get; private set; }

    /// <summary>Quién realizó la valoración.</summary>
    public Guid AssessedByUserId { get; private set; }

    /// <summary>Crea una valoración.</summary>
    public static FitnessAssessment Create(
        Guid tenantId,
        Guid athleteProfileId,
        DateTimeOffset assessedAt,
        string title,
        string summary,
        Guid assessedByUserId,
        DateTimeOffset now,
        int? score = null)
    {
        if (score is < 1 or > 10)
        {
            throw new DomainException("La puntuación debe estar entre 1 y 10.");
        }

        var assessment = new FitnessAssessment(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            AssessedAt = assessedAt,
            Title = TextRules.Required(title, "El título", FieldLengths.AssessmentTitle),
            Summary = TextRules.Required(summary, "El resumen", FieldLengths.ProgressNotes),
            Score = score,
            AssessedByUserId = TextRules.RequiredId(assessedByUserId, "Quien valora"),
        };
        assessment.MarkCreated(now, assessedByUserId);
        return assessment;
    }

    /// <summary>Actualiza la valoración.</summary>
    public void Update(
        DateTimeOffset assessedAt,
        string title,
        string summary,
        int? score,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        if (score is < 1 or > 10)
        {
            throw new DomainException("La puntuación debe estar entre 1 y 10.");
        }

        AssessedAt = assessedAt;
        Title = TextRules.Required(title, "El título", FieldLengths.AssessmentTitle);
        Summary = TextRules.Required(summary, "El resumen", FieldLengths.ProgressNotes);
        Score = score;
        MarkUpdated(now, actor);
    }
}
