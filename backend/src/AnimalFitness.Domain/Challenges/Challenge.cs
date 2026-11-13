using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Challenges;

/// <summary>Reto de la organización.</summary>
public sealed class Challenge : TenantScopedEntity
{
    private Challenge(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Nombre.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descripción.</summary>
    public string? Description { get; private set; }

    /// <summary>Individual o grupal.</summary>
    public ChallengeKind Kind { get; private set; }

    /// <summary>Métrica de puntuación.</summary>
    public ChallengeMetric Metric { get; private set; }

    /// <summary>Inicio (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Fin (UTC).</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Meta opcional (ej. 12 entrenamientos).</summary>
    public int? TargetValue { get; private set; }

    /// <summary>Estado.</summary>
    public ChallengeStatus Status { get; private set; }

    /// <summary>Crea un reto en borrador.</summary>
    public static Challenge Create(
        Guid tenantId,
        string name,
        ChallengeKind kind,
        ChallengeMetric metric,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset now,
        Guid? createdBy,
        string? description = null,
        int? targetValue = null)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException("La fecha de fin debe ser posterior al inicio.");
        }

        var challenge = new Challenge(tenantId)
        {
            Name = TextRules.Required(name, "El nombre", FieldLengths.NutritionOrChallengeName),
            Description = string.IsNullOrWhiteSpace(description)
                ? null
                : TextRules.Required(description, "La descripción", FieldLengths.ChallengeDescription),
            Kind = TextRules.Defined(kind, "El tipo"),
            Metric = TextRules.Defined(metric, "La métrica"),
            StartsAt = startsAt,
            EndsAt = endsAt,
            TargetValue = targetValue,
            Status = ChallengeStatus.Draft,
        };
        challenge.MarkCreated(now, createdBy);
        return challenge;
    }

    /// <summary>Activa el reto.</summary>
    public void Activate(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != ChallengeStatus.Draft)
        {
            throw new DomainException("Solo se puede activar un reto en borrador.");
        }

        MarkUpdated(now, actor);
        Status = ChallengeStatus.Active;
    }

    /// <summary>Finaliza el reto.</summary>
    public void Complete(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != ChallengeStatus.Active)
        {
            throw new DomainException("Solo se puede finalizar un reto activo.");
        }

        MarkUpdated(now, actor);
        Status = ChallengeStatus.Completed;
    }

    /// <summary>Cancela el reto.</summary>
    public void Cancel(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status is ChallengeStatus.Completed or ChallengeStatus.Cancelled)
        {
            throw new DomainException("El reto ya está cerrado.");
        }

        MarkUpdated(now, actor);
        Status = ChallengeStatus.Cancelled;
    }
}
