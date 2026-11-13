using AnimalFitness.Application.Abstractions.Tenancy;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Combina las fuentes de candidatos. Prioridad: ruta, cabecera, subdominio.
/// </summary>
public sealed class CompositeTenantCandidateResolver(IEnumerable<ITenantCandidateSource> sources)
{
    private readonly ITenantCandidateSource[] _sources = sources.ToArray();

    /// <summary>
    /// Devuelve el primer candidato encontrado según la prioridad de registro.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP.</param>
    public TenantCandidate? Resolve(HttpContext httpContext)
    {
        foreach (var source in _sources)
        {
            var candidate = source.TryResolve(httpContext);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }
}
