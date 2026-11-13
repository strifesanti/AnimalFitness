using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Challenges;

/// <summary>Participante de un reto.</summary>
public sealed class ChallengeParticipant : TenantScopedEntity
{
    private ChallengeParticipant(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Reto.</summary>
    public Guid ChallengeId { get; private set; }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Puntos acumulados.</summary>
    public int Points { get; private set; }

    /// <summary>Momento de inscripción.</summary>
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>Inscribe a un alumno.</summary>
    public static ChallengeParticipant Create(
        Guid tenantId,
        Guid challengeId,
        Guid athleteProfileId,
        DateTimeOffset now,
        Guid? createdBy)
    {
        var participant = new ChallengeParticipant(tenantId)
        {
            ChallengeId = TextRules.RequiredId(challengeId, "El reto"),
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            Points = 0,
            JoinedAt = now,
        };
        participant.MarkCreated(now, createdBy);
        return participant;
    }

    /// <summary>Suma puntos.</summary>
    public void AddPoints(int points, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (points < 1)
        {
            throw new DomainException("Los puntos deben ser positivos.");
        }

        MarkUpdated(now, actor);
        Points += points;
    }
}
