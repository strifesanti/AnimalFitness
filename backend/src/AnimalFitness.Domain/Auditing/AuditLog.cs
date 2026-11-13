using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Auditing;

/// <summary>
/// Hecho de auditoría, inmutable. El identificador de organización es nulo cuando el hecho es de la plataforma.
/// No implementa <see cref="ITenantEntity"/> para que un filtro genérico no oculte los hechos de plataforma.
/// </summary>
public sealed class AuditLog
{
    private AuditLog()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>Identificador del hecho.</summary>
    public Guid Id { get; private set; }

    /// <summary>Organización afectada. Nulo en hechos de la plataforma.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Persona que realizó la acción. Nulo si fue un proceso del sistema.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Acción realizada, por ejemplo tenant.suspended.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Tipo de la entidad afectada.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Identificador de la entidad afectada, cuando la acción se refiere a una.</summary>
    public Guid? EntityId { get; private set; }

    /// <summary>Detalle del cambio, como objeto JSON. Nulo si la acción no tiene detalle.</summary>
    public string? ChangesJson { get; private set; }

    /// <summary>Dirección IP de origen.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Agente de usuario de origen.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Momento del hecho, en UTC.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Registra un hecho. No existe una operación para modificarlo.
    /// </summary>
    /// <param name="tenantId">Organización. Nulo si el hecho es de la plataforma. No se admite un identificador vacío.</param>
    /// <param name="userId">Autor. Nulo si fue un proceso del sistema.</param>
    /// <param name="action">Nombre de la acción.</param>
    /// <param name="entityType">Tipo de la entidad afectada.</param>
    /// <param name="entityId">Identificador de la entidad afectada.</param>
    /// <param name="changesJson">Objeto JSON con el detalle. Nulo si no hay detalle.</param>
    /// <param name="ipAddress">Dirección IP de origen.</param>
    /// <param name="userAgent">Agente de usuario de origen.</param>
    /// <param name="occurredAt">Momento del hecho, en UTC.</param>
    /// <returns>El hecho registrado.</returns>
    public static AuditLog Record(
        Guid? tenantId,
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? changesJson,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAt)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty || entityId == Guid.Empty)
        {
            throw new DomainException("El identificador no es válido.");
        }

        if (occurredAt == default)
        {
            throw new DomainException("La fecha del hecho no es válida.");
        }

        return new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = TextRules.Required(action, "La acción", FieldLengths.AuditAction),
            EntityType = TextRules.Required(entityType, "El tipo de entidad", FieldLengths.EntityType),
            EntityId = entityId,
            ChangesJson = TextRules.OptionalJsonObject(changesJson),
            IpAddress = TextRules.OptionalIp(ipAddress),
            UserAgent = TextRules.OptionalUserAgent(userAgent),
            OccurredAt = occurredAt,
        };
    }
}
