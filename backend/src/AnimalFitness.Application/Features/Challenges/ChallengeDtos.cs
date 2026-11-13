using AnimalFitness.Domain.Challenges;

namespace AnimalFitness.Application.Features.Challenges;

/// <summary>Reto con su participación.</summary>
public sealed record ChallengeDto(
    Guid Id,
    string Name,
    string? Description,
    ChallengeKind Kind,
    ChallengeMetric Metric,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? TargetValue,
    ChallengeStatus Status,
    int ParticipantCount,
    bool Joined);

/// <summary>Crear un reto (queda en borrador).</summary>
public sealed record CreateChallengeRequest(
    string Name,
    string? Description,
    ChallengeKind Kind,
    ChallengeMetric Metric,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? TargetValue);

/// <summary>Inscribirse en un reto. Los alumnos omiten el perfil; Owner y Trainer inscriben a un alumno.</summary>
public sealed record JoinChallengeRequest(Guid? AthleteProfileId);

/// <summary>Participante inscrito.</summary>
public sealed record ChallengeParticipantDto(
    Guid Id,
    Guid ChallengeId,
    Guid AthleteProfileId,
    int Points,
    DateTimeOffset JoinedAt);

/// <summary>Sumar puntos a un participante del reto.</summary>
public sealed record AddChallengeActivityRequest(
    Guid AthleteProfileId,
    int Points,
    string? Note,
    DateTimeOffset? OccurredAt);

/// <summary>Actividad registrada en un reto.</summary>
public sealed record ChallengeActivityDto(
    Guid Id,
    Guid ChallengeId,
    Guid ParticipantId,
    Guid AthleteProfileId,
    string AthleteName,
    int Points,
    string? Note,
    DateTimeOffset OccurredAt);

/// <summary>Posición de la clasificación (mayor puntuación primero).</summary>
public sealed record LeaderboardEntryDto(
    int Rank,
    Guid ParticipantId,
    Guid AthleteProfileId,
    string AthleteName,
    int Points);

/// <summary>Logro otorgado a un alumno.</summary>
public sealed record AchievementDto(
    Guid Id,
    Guid AthleteProfileId,
    string AthleteName,
    Guid? ChallengeId,
    string Title,
    string? Description,
    DateTimeOffset AwardedAt);

/// <summary>Otorgar un logro.</summary>
public sealed record AwardAchievementRequest(
    Guid AthleteProfileId,
    string Title,
    string? Description,
    Guid? ChallengeId);
