using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalFitness.Application.Features.Auth;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using AnimalFitness.IntegrationTests.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalFitness.IntegrationTests.Progress;

public sealed class ProgressFlowTests : IClassFixture<AuthApiFactory>
{
    private const string Password = "ClaveSegura1";
    private readonly AuthApiFactory _factory;

    public ProgressFlowTests(AuthApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSchema();
    }

    [Fact]
    public async Task Owner_and_athlete_track_progress()
    {
        var slug = $"prog-{Guid.CreateVersion7():N}"[..20];
        var ownerEmail = $"owner.{Guid.CreateVersion7():N}@animalfitness.test";
        var athleteEmail = $"athlete.{Guid.CreateVersion7():N}@animalfitness.test";
        var athleteProfileId = await SeedAsync(slug, ownerEmail, athleteEmail);

        using var owner = CreateClient();
        await LoginAsync(owner, ownerEmail);
        var baseUrl = $"/api/v1/t/{slug}/progress";

        // Owner debe indicar el alumno.
        var missing = await SendAsync(owner, HttpMethod.Post, $"{baseUrl}/measurements", new { weightKg = 70 });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        // Medición con IMC calculado.
        var created = await SendAsync(
            owner,
            HttpMethod.Post,
            $"{baseUrl}/measurements",
            new { athleteProfileId, weightKg = 70, heightCm = 175, waistCm = 80 });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var value = createdBody.RootElement.GetProperty("value");
        Assert.Equal(22.86m, value.GetProperty("bmi").GetDecimal());
        var measurementId = value.GetProperty("id").GetGuid();

        var updated = await SendAsync(
            owner,
            HttpMethod.Put,
            $"{baseUrl}/measurements/{measurementId}",
            new { weightKg = 72, heightCm = 175 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        // Series para gráficos.
        var charts = await owner.GetAsync($"{baseUrl}/charts?athleteProfileId={athleteProfileId}");
        Assert.Equal(HttpStatusCode.OK, charts.StatusCode);
        using var chartsBody = JsonDocument.Parse(await charts.Content.ReadAsStringAsync());
        var series = chartsBody.RootElement.GetProperty("value");
        Assert.Equal(1, series.GetProperty("weightKg").GetArrayLength());
        Assert.Equal(1, series.GetProperty("bmi").GetArrayLength());
        Assert.Equal(0, series.GetProperty("waistCm").GetArrayLength());

        // Foto y valoración.
        var photo = await SendAsync(
            owner,
            HttpMethod.Post,
            $"{baseUrl}/photos",
            new { athleteProfileId, pose = "Front", url = "https://example.test/a.jpg" });
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        var assessment = await SendAsync(
            owner,
            HttpMethod.Post,
            $"{baseUrl}/assessments",
            new { athleteProfileId, title = "Fuerza", summary = "Buen nivel", score = 8 });
        Assert.Equal(HttpStatusCode.OK, assessment.StatusCode);

        // Objetivo: crear y completar.
        var goal = await SendAsync(
            owner,
            HttpMethod.Post,
            $"{baseUrl}/goals",
            new { athleteProfileId, title = "Bajar a 75 kg", targetValue = 75, unit = "kg" });
        Assert.Equal(HttpStatusCode.OK, goal.StatusCode);
        using var goalBody = JsonDocument.Parse(await goal.Content.ReadAsStringAsync());
        var goalId = goalBody.RootElement.GetProperty("value").GetProperty("id").GetGuid();
        var complete = await SendAsync(owner, HttpMethod.Post, $"{baseUrl}/goals/{goalId}/complete", new { });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var again = await SendAsync(owner, HttpMethod.Post, $"{baseUrl}/goals/{goalId}/cancel", new { });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        // El alumno ve y registra solo lo propio.
        using var athlete = CreateClient();
        await LoginAsync(athlete, athleteEmail);
        var own = await athlete.GetAsync($"{baseUrl}/measurements");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        using var ownBody = JsonDocument.Parse(await own.Content.ReadAsStringAsync());
        Assert.Equal(1, ownBody.RootElement.GetProperty("value").GetArrayLength());

        var other = await athlete.GetAsync($"{baseUrl}/measurements?athleteProfileId={Guid.CreateVersion7()}");
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);

        var ownCreate = await SendAsync(athlete, HttpMethod.Post, $"{baseUrl}/measurements", new { weightKg = 71 });
        Assert.Equal(HttpStatusCode.OK, ownCreate.StatusCode);
    }

    private async Task<Guid> SeedAsync(string slug, string ownerEmail, string athleteEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var ownerId = await CreateUserAsync(userManager, db, ownerEmail, "Olivia", "Owner", now);
        var athleteUserId = await CreateUserAsync(userManager, db, athleteEmail, "Ana", "Alumna", now);

        var tenant = Tenant.Create("Academia Progreso", slug, ownerEmail, now);
        tenant.Activate(now.AddMinutes(1), ownerId);
        db.Tenants.Add(tenant);
        db.OrganizationSettings.Add(TenantSettings.Create(tenant.Id, now, ownerId));
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, ownerId, MembershipRole.Owner, now, ownerId));
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, athleteUserId, MembershipRole.Athlete, now, ownerId));
        var profile = AthleteProfile.Create(tenant.Id, athleteUserId, now, ownerId);
        db.AthleteProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile.Id;
    }

    private static async Task<Guid> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        string email,
        string firstName,
        string lastName,
        DateTimeOffset now)
    {
        var id = Guid.CreateVersion7();
        var account = new ApplicationUser(id) { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await userManager.CreateAsync(account, Password)).Succeeded);
        db.UserProfiles.Add(User.Create(email, firstName, lastName, now, id: id));
        return id;
    }

    private async Task LoginAsync(HttpClient client, string email)
    {
        var csrf = await GetCsrfAsync(client);
        using var login = CreateJsonRequest(HttpMethod.Post, "/api/v1/auth/login", new LoginRequest(email, Password), csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object body)
    {
        var csrf = await GetCsrfAsync(client);
        using var request = CreateJsonRequest(method, url, body, csrf);
        return await client.SendAsync(request);
    }

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("No se recibió token CSRF.");
    }

    private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string url, T body, string csrf)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }),
        };
        request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", csrf);
        return request;
    }
}
