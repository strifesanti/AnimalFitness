using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Challenges;

/// <summary>Reconocimiento otorgado a un alumno.</summary>
public sealed class Achievement : TenantScopedEntity
{
    private Achievement(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Reto opcional de origen.</summary>
    public Guid? ChallengeId { get; private set; }

    /// <summary>Título.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descripción.</summary>
    public string? Description { get; private set; }

    /// <summary>Momento del reconocimiento.</summary>
    public DateTimeOffset AwardedAt { get; private set; }

    /// <summary>Otorga un logro.</summary>
    public static Achievement Create(
        Guid tenantId,
        Guid athleteProfileId,
        string title,
        DateTimeOffset awardedAt,
        DateTimeOffset now,
        Guid? createdBy,
        Guid? challengeId = null,
        string? description = null)
    {
        var achievement = new Achievement(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            ChallengeId = challengeId,
            Title = TextRules.Required(title, "El título", FieldLengths.NutritionOrChallengeName),
            Description = string.IsNullOrWhiteSpace(description)
                ? null
                : TextRules.Required(description, "La descripción", FieldLengths.ChallengeDescription),
            AwardedAt = awardedAt,
        };
        achievement.MarkCreated(now, createdBy);
        return achievement;
    }
}
