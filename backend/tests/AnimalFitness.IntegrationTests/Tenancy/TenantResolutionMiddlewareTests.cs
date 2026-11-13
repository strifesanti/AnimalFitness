using System.Net;
using System.Text.Json;
using AnimalFitness.API.Tenancy;

namespace AnimalFitness.IntegrationTests.Tenancy;

public sealed class TenantResolutionMiddlewareTests : IClassFixture<TenancyApiFactory>
{
    private readonly TenancyApiFactory _factory;

    public TenantResolutionMiddlewareTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    [Fact]
    public async Task Route_candidate_without_authentication_returns_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/t/academia-norte/context");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Member_can_access_their_organization_via_route()
    {
        using var client = CreateAuthenticatedClient(_factory.MemberId);

        var response = await client.GetAsync("/api/v1/t/academia-norte/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_factory.ActiveTenantId.ToString("D"), response.Headers.GetValues("X-Tenant-Id").Single());
        Assert.Equal("academia-norte", response.Headers.GetValues("X-Tenant-Slug").Single());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(_factory.ActiveTenantId, body.RootElement.GetProperty("tenantId").GetGuid());
        Assert.Equal(_factory.MemberId, body.RootElement.GetProperty("userId").GetGuid());
        Assert.Equal("Trainer", body.RootElement.GetProperty("role").GetString());
        Assert.True(body.RootElement.GetProperty("hasTenant").GetBoolean());
    }

    [Fact]
    public async Task Member_can_access_via_header()
    {
        using var client = CreateAuthenticatedClient(_factory.MemberId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenancy/context");
        request.Headers.Add(TenantHttpHeaders.Tenant, "academia-norte");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(_factory.ActiveTenantId, body.RootElement.GetProperty("tenantId").GetGuid());
    }

    [Fact]
    public async Task Outsider_receives_403()
    {
        using var client = CreateAuthenticatedClient(_factory.OutsiderId);

        var response = await client.GetAsync("/api/v1/t/academia-norte/context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("membresía activa", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_organization_returns_404()
    {
        using var client = CreateAuthenticatedClient(_factory.MemberId);

        var response = await client.GetAsync("/api/v1/t/no-existe/context");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Suspended_organization_returns_403()
    {
        using var client = CreateAuthenticatedClient(_factory.MemberId);

        var response = await client.GetAsync("/api/v1/t/academia-sur/context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("suspendida", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Changing_only_the_route_slug_cannot_reach_another_organization()
    {
        using var client = CreateAuthenticatedClient(_factory.MemberId);

        var response = await client.GetAsync($"/api/v1/t/{_factory.SuspendedTenantId:D}/context");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Platform_context_without_candidate_has_user_but_no_tenant()
    {
        using var client = CreateAuthenticatedClient(_factory.MemberId);

        var response = await client.GetAsync("/api/v1/tenancy/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("tenantId").ValueKind);
        Assert.Equal(_factory.MemberId, body.RootElement.GetProperty("userId").GetGuid());
        Assert.False(body.RootElement.GetProperty("hasTenant").GetBoolean());
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString("D"));
        return client;
    }
}
