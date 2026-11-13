using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Access;

/// <summary>
/// Solicitud de una persona para entrar a una organización. La decisión no se puede cambiar después.
/// </summary>
public sealed class JoinRequest : TenantScopedEntity
{
    private JoinRequest(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Persona que solicita el ingreso.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Estado de la solicitud.</summary>
    public JoinRequestStatus Status { get; private set; }

    /// <summary>Persona de la organización que decidió.</summary>
    public Guid? ReviewedBy { get; private set; }

    /// <summary>Momento de la decisión, en UTC.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Motivo del rechazo. Solo existe cuando la solicitud fue rechazada.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Registra una solicitud pendiente.
    /// </summary>
    /// <param name="tenantId">Organización solicitada.</param>
    /// <param name="userId">Persona que solicita el ingreso.</param>
    /// <param name="now">Momento de la solicitud, en UTC.</param>
    /// <returns>La solicitud pendiente.</returns>
    public static JoinRequest Create(Guid tenantId, Guid userId, DateTimeOffset now)
    {
        var request = new JoinRequest(tenantId)
        {
            UserId = TextRules.RequiredId(userId, "La persona"),
            Status = JoinRequestStatus.Pending,
        };
        request.MarkCreated(now, userId);
        return request;
    }

    /// <summary>Aprueba el ingreso.</summary>
    /// <param name="reviewerId">Persona de la organización que aprueba.</param>
    /// <param name="now">Momento de la decisión, en UTC.</param>
    public void Approve(Guid reviewerId, DateTimeOffset now)
    {
        EnsurePending();
        var reviewer = TextRules.RequiredId(reviewerId, "Quien revisa");
        MarkUpdated(now, reviewer);
        ReviewedBy = reviewer;
        ReviewedAt = now;
        Status = JoinRequestStatus.Approved;
    }

    /// <summary>Rechaza el ingreso e indica el motivo.</summary>
    /// <param name="reviewerId">Persona de la organización que rechaza.</param>
    /// <param name="reason">Motivo visible para la persona que solicitó el ingreso.</param>
    /// <param name="now">Momento de la decisión, en UTC.</param>
    public void Reject(Guid reviewerId, string reason, DateTimeOffset now)
    {
        EnsurePending();
        var reviewer = TextRules.RequiredId(reviewerId, "Quien revisa");
        var normalizedReason = TextRules.Required(reason, "El motivo", FieldLengths.RejectionReason);
        MarkUpdated(now, reviewer);
        ReviewedBy = reviewer;
        RejectionReason = normalizedReason;
        ReviewedAt = now;
        Status = JoinRequestStatus.Rejected;
    }

    /// <summary>La persona retira su solicitud.</summary>
    /// <param name="now">Momento de la cancelación, en UTC.</param>
    /// <param name="actor">Usuario que cancela.</param>
    public void Cancel(DateTimeOffset now, Guid? actor)
    {
        EnsurePending();
        MarkUpdated(now, actor);
        Status = JoinRequestStatus.Cancelled;
    }

    private void EnsurePending()
    {
        EnsureNotDeleted();
        if (Status != JoinRequestStatus.Pending)
        {
            throw new DomainException("La solicitud ya fue resuelta.");
        }
    }
}
