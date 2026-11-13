using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AnimalFitness.IntegrationTests;

public sealed class ApiSmokeTests : IClassFixture<AnimalFitnessApiFactory>
{
    private readonly AnimalFitnessApiFactory _factory;

    public ApiSmokeTests(AnimalFitnessApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Info_returns_service_identity()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("AnimalFitness", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("0.1.0", document.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No se encontró el recurso.", body);
    }

    [Fact]
    public async Task OpenApi_is_hidden_outside_development()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_is_available_in_development()
    {
        await using var factory = new AnimalFitnessApiFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            // Evita el seeder: SQLite :memory: no comparte esquema entre conexiones.
            builder.UseSetting("Seed:Enabled", "false");
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
