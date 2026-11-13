using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Challenges;

/// <summary>Actividad que aporta puntos a un participante.</summary>
public sealed class ChallengeActivity : TenantScopedEntity
{
    private ChallengeActivity(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Reto.</summary>
    public Guid ChallengeId { get; private set; }

    /// <summary>Participante.</summary>
    public Guid ParticipantId { get; private set; }

    /// <summary>Puntos otorgados.</summary>
    public int Points { get; private set; }

    /// <summary>Nota.</summary>
    public string? Note { get; private set; }

    /// <summary>Momento.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Registra actividad.</summary>
    public static ChallengeActivity Create(
        Guid tenantId,
        Guid challengeId,
        Guid participantId,
        int points,
        DateTimeOffset occurredAt,
        DateTimeOffset now,
        Guid? createdBy,
        string? note = null)
    {
        if (points < 1)
        {
            throw new DomainException("Los puntos deben ser positivos.");
        }

        var activity = new ChallengeActivity(tenantId)
        {
            ChallengeId = TextRules.RequiredId(challengeId, "El reto"),
            ParticipantId = TextRules.RequiredId(participantId, "El participante"),
            Points = points,
            Note = string.IsNullOrWhiteSpace(note)
                ? null
                : TextRules.Required(note, "La nota", FieldLengths.ProgressNotes),
            OccurredAt = occurredAt,
        };
        activity.MarkCreated(now, createdBy);
        return activity;
    }
}
