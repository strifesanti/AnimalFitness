using AnimalFitness.Application.Abstractions.Tenancy;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Extrae el slug de la cabecera <c>X-Tenant</c>.
/// </summary>
public sealed class HeaderTenantCandidateSource : ITenantCandidateSource
{
    /// <inheritdoc />
    public TenantCandidate? TryResolve(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(TenantHttpHeaders.Tenant, out var values))
        {
            return null;
        }

        var slug = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return new TenantCandidate(slug.Trim(), TenantCandidateSource.Header);
    }
}
