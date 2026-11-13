using AnimalFitness.Application.Abstractions.Tenancy;
using Microsoft.Extensions.Options;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Extrae el slug del subdominio cuando el host termina en el dominio base configurado.
/// Preparado para academia1.animalfitness.com. Desactivado si BaseDomain está vacío.
/// </summary>
public sealed class SubdomainTenantCandidateSource(IOptions<TenantResolutionOptions> options) : ITenantCandidateSource
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "www",
        "api",
        "admin",
        "app",
        "mail",
        "static",
        "animalfitness",
    };

    /// <inheritdoc />
    public TenantCandidate? TryResolve(HttpContext httpContext)
    {
        var baseDomain = options.Value.BaseDomain?.Trim().Trim('.');
        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            return null;
        }

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        if (!host.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase)
            && !host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var subdomain = host[..^(baseDomain.Length + 1)];
        if (subdomain.Contains('.', StringComparison.Ordinal) || Reserved.Contains(subdomain))
        {
            return null;
        }

        return new TenantCandidate(subdomain, TenantCandidateSource.Subdomain);
    }
}
