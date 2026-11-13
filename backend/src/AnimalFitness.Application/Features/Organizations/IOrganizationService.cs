using AnimalFitness.Application.Common;

namespace AnimalFitness.Application.Features.Organizations;

/// <summary>Casos de uso de organizaciones y directorio.</summary>
public interface IOrganizationService
{
    /// <summary>Lista organizaciones (SuperAdmin).</summary>
    Task<AppResult> ListPlatformTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene el detalle de una organización (SuperAdmin).</summary>
    Task<AppResult> GetPlatformTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Crea una organización y asigna el propietario (SuperAdmin).</summary>
    Task<AppResult> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Activa una organización (SuperAdmin).</summary>
    Task<AppResult> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Suspende una organización (SuperAdmin).</summary>
    Task<AppResult> SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene la organización activa.</summary>
    Task<AppResult> GetCurrentOrganizationAsync(CancellationToken cancellationToken = default);

    /// <summary>Actualiza el perfil de la organización activa.</summary>
    Task<AppResult> UpdateCurrentOrganizationAsync(UpdateOrganizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza las preferencias de la organización activa.</summary>
    Task<AppResult> UpdateCurrentSettingsAsync(UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Directorio de organizaciones para solicitar ingreso.</summary>
    Task<AppResult> ListDirectoryAsync(CancellationToken cancellationToken = default);
}
