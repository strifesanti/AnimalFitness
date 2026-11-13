namespace AnimalFitness.Application.Abstractions.Tenancy;

/// <summary>
/// Valida que una persona tenga membresía activa en una organización.
/// Es la única ruta permitida para consultar membresías sin el filtro de organización ya establecido.
/// </summary>
public interface ITenantAccessService
{
    /// <summary>
    /// Resuelve el acceso de una persona a una organización identificada por su slug.
    /// </summary>
    /// <param name="slug">Identificador legible de la organización.</param>
    /// <param name="userId">Persona autenticada.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>El resultado del acceso.</returns>
    Task<TenantAccessOutcome> ResolveAsync(string slug, Guid userId, CancellationToken cancellationToken = default);
}
