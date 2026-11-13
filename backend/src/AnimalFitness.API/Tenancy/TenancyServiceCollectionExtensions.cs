namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Registro de resolución de organización en la capa API.
/// </summary>
public static class TenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registra fuentes de candidatos y el resolutor compuesto.
    /// El orden de registro define la prioridad: ruta, cabecera, subdominio.
    /// </summary>
    public static IServiceCollection AddTenantResolution(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantResolutionOptions>(configuration.GetSection(TenantResolutionOptions.SectionName));
        services.AddSingleton<ITenantCandidateSource, RouteTenantCandidateSource>();
        services.AddSingleton<ITenantCandidateSource, HeaderTenantCandidateSource>();
        services.AddSingleton<ITenantCandidateSource, SubdomainTenantCandidateSource>();
        services.AddSingleton<CompositeTenantCandidateResolver>();
        return services;
    }
}
