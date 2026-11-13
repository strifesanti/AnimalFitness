namespace AnimalFitness.Application.Abstractions.Tenancy;

/// <summary>
/// Origen del candidato de organización.
/// </summary>
public enum TenantCandidateSource
{
    /// <summary>Ruta con segmento <c>/t/{slug}</c>.</summary>
    Route = 1,

    /// <summary>Cabecera <c>X-Tenant</c>.</summary>
    Header = 2,

    /// <summary>Subdominio del host, preparado para el futuro.</summary>
    Subdomain = 3,
}
