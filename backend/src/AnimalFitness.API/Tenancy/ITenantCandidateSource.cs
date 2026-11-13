using AnimalFitness.Application.Abstractions.Tenancy;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Fuente de candidatos de organización a partir de la petición HTTP.
/// </summary>
public interface ITenantCandidateSource
{
    /// <summary>
    /// Extrae un candidato, o <c>null</c> si esta fuente no aporta ninguno.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP.</param>
    TenantCandidate? TryResolve(HttpContext httpContext);
}
