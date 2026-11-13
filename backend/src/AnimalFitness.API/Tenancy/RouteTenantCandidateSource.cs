using System.Text.RegularExpressions;
using AnimalFitness.Application.Abstractions.Tenancy;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Extrae el slug de la ruta cuando contiene el segmento <c>/t/{slug}</c>.
/// </summary>
public sealed partial class RouteTenantCandidateSource : ITenantCandidateSource
{
    /// <inheritdoc />
    public TenantCandidate? TryResolve(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var match = RouteSlugRegex().Match(path);
        if (!match.Success)
        {
            return null;
        }

        return new TenantCandidate(match.Groups["slug"].Value, TenantCandidateSource.Route);
    }

    [GeneratedRegex(@"/(?:api/v1/)?t/(?<slug>[a-z0-9]+(?:-[a-z0-9]+)*)(?:/|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RouteSlugRegex();
}
