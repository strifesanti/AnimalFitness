namespace AnimalFitness.Domain.Common;

/// <summary>
/// Entidad que pertenece a una organización. El vínculo se fija en el constructor.
/// </summary>
public abstract class TenantScopedEntity : SoftDeletableEntity, ITenantEntity
{
    /// <summary>
    /// Crea la entidad dentro de una organización.
    /// </summary>
    /// <param name="tenantId">Organización propietaria.</param>
    protected TenantScopedEntity(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("La organización es obligatoria.");
        }

        TenantId = tenantId;
    }

    /// <inheritdoc />
    public Guid TenantId { get; private set; }
}
