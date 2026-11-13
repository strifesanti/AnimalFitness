using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Compliance;

/// <summary>
/// Autorización expresa de una persona. Puede ser de la plataforma o de una organización.
/// Revocarla no borra el hecho de que existió.
/// </summary>
public sealed class ConsentRecord : BaseEntity
{
    private ConsentRecord()
    {
    }

    /// <summary>Persona titular de los datos.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Organización a la que aplica. Nulo cuando la autorización es para la plataforma.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Tipo de tratamiento autorizado.</summary>
    public ConsentType ConsentType { get; private set; }

    /// <summary>Versión del texto que la persona aceptó.</summary>
    public string PolicyVersion { get; private set; } = string.Empty;

    /// <summary>Momento en que se otorgó, en UTC.</summary>
    public DateTimeOffset GrantedAt { get; private set; }

    /// <summary>Momento en que se revocó, en UTC. Nulo mientras sigue vigente.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Dirección IP desde la que se otorgó, si se conoce.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Indica si la autorización sigue vigente.</summary>
    public bool IsActive => RevokedAt is null;

    /// <summary>
    /// Registra una autorización vigente.
    /// </summary>
    /// <param name="userId">Titular.</param>
    /// <param name="tenantId">Organización. Nulo para una autorización de la plataforma.</param>
    /// <param name="consentType">Tipo de tratamiento.</param>
    /// <param name="policyVersion">Versión del texto aceptado.</param>
    /// <param name="ipAddress">Dirección IP de origen.</param>
    /// <param name="now">Momento de la autorización, en UTC.</param>
    /// <param name="createdBy">Quien registra la autorización. Normalmente es el titular.</param>
    /// <returns>La autorización vigente.</returns>
    public static ConsentRecord Grant(
        Guid userId,
        Guid? tenantId,
        ConsentType consentType,
        string policyVersion,
        string? ipAddress,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("La organización no es válida.");
        }

        var record = new ConsentRecord
        {
            UserId = TextRules.RequiredId(userId, "La persona"),
            TenantId = tenantId,
            ConsentType = TextRules.Defined(consentType, "El tipo de autorización"),
            PolicyVersion = TextRules.Required(policyVersion, "La versión", FieldLengths.PolicyVersion),
            GrantedAt = now,
            IpAddress = TextRules.OptionalIp(ipAddress),
        };
        record.MarkCreated(now, createdBy);
        return record;
    }

    /// <summary>Revoca la autorización. Se conserva el registro.</summary>
    /// <param name="now">Momento de la revocación, en UTC.</param>
    /// <param name="actor">Usuario que revoca.</param>
    public void Revoke(DateTimeOffset now, Guid? actor)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("La autorización ya fue revocada.");
        }

        if (now < GrantedAt)
        {
            throw new DomainException("La revocación no puede ser anterior a la autorización.");
        }

        MarkUpdated(now, actor);
        RevokedAt = now;
    }
}
