namespace AnimalFitness.Domain.Common;

/// <summary>
/// Entidad que pertenece a una sola organización. El identificador se asigna al crearla y no cambia.
/// </summary>
public interface ITenantEntity
{
    /// <summary>Organización propietaria del registro.</summary>
    Guid TenantId { get; }
}
