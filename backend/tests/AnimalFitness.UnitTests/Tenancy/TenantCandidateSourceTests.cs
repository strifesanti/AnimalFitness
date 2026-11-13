using AnimalFitness.API.Tenancy;
using AnimalFitness.Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AnimalFitness.UnitTests.Tenancy;

public sealed class TenantCandidateSourceTests
{
    [Theory]
    [InlineData("/api/v1/t/academia-norte/context", "academia-norte")]
    [InlineData("/t/academia-norte", "academia-norte")]
    [InlineData("/api/v1/t/academia-norte", "academia-norte")]
    public void Route_source_reads_the_slug(string path, string expected)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;

        var candidate = new RouteTenantCandidateSource().TryResolve(http);

        Assert.NotNull(candidate);
        Assert.Equal(expected, candidate!.Slug);
        Assert.Equal(TenantCandidateSource.Route, candidate.Source);
    }

    [Fact]
    public void Header_source_reads_x_tenant()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[TenantHttpHeaders.Tenant] = "academia-norte";

        var candidate = new HeaderTenantCandidateSource().TryResolve(http);

        Assert.NotNull(candidate);
        Assert.Equal("academia-norte", candidate!.Slug);
        Assert.Equal(TenantCandidateSource.Header, candidate.Source);
    }

    [Fact]
    public void Subdomain_source_reads_the_leftmost_label_when_base_domain_is_set()
    {
        var http = new DefaultHttpContext();
        http.Request.Host = new HostString("academia-norte.animalfitness.com");
        var source = new SubdomainTenantCandidateSource(Options.Create(new TenantResolutionOptions
        {
            BaseDomain = "animalfitness.com",
        }));

        var candidate = source.TryResolve(http);

        Assert.NotNull(candidate);
        Assert.Equal("academia-norte", candidate!.Slug);
        Assert.Equal(TenantCandidateSource.Subdomain, candidate.Source);
    }

    [Fact]
    public void Subdomain_source_is_disabled_without_base_domain()
    {
        var http = new DefaultHttpContext();
        http.Request.Host = new HostString("academia-norte.animalfitness.com");
        var source = new SubdomainTenantCandidateSource(Options.Create(new TenantResolutionOptions()));

        Assert.Null(source.TryResolve(http));
    }

    [Fact]
    public void Composite_prefers_route_over_header()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/v1/t/desde-ruta/context";
        http.Request.Headers[TenantHttpHeaders.Tenant] = "desde-cabecera";

        var resolver = new CompositeTenantCandidateResolver(
        [
            new RouteTenantCandidateSource(),
            new HeaderTenantCandidateSource(),
            new SubdomainTenantCandidateSource(Options.Create(new TenantResolutionOptions())),
        ]);

        var candidate = resolver.Resolve(http);

        Assert.Equal("desde-ruta", candidate!.Slug);
        Assert.Equal(TenantCandidateSource.Route, candidate.Source);
    }
}
