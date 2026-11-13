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

namespace AnimalFitness.IntegrationTests.Advanced;

public sealed class Phase5FlowTests : IClassFixture<AuthApiFactory>
{
    private const string Password = "ClaveSegura1";
    private readonly AuthApiFactory _factory;

    public Phase5FlowTests(AuthApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSchema();
    }

    private sealed record Seed(Guid OwnerUserId, Guid AthleteUserId, Guid AthleteProfileId);

    [Fact]
    public async Task Athlete_logs_nutrition_and_summary_compares_with_target()
    {
        var (slug, ownerEmail, athleteEmail, _) = await NewTenantAsync();
        using var athlete = CreateClient();
        await LoginAsync(athlete, athleteEmail);
        var url = $"/api/v1/t/{slug}/nutrition";

        // Validación de dominio → 400.
        var invalid = await SendAsync(athlete, HttpMethod.Post, $"{url}/targets",
            new { calories = 100, proteinGrams = 100, carbsGrams = 100, fatGrams = 50, waterMl = 2000 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var target = await SendAsync(athlete, HttpMethod.Post, $"{url}/targets",
            new { calories = 2000, proteinGrams = 100, carbsGrams = 200, fatGrams = 50, waterMl = 2000 });
        Assert.Equal(HttpStatusCode.OK, target.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{url}/foods",
            new { mealType = "Breakfast", name = "Avena", calories = 500, proteinGrams = 25, carbsGrams = 60, fatGrams = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{url}/foods",
            new { mealType = "Lunch", name = "Pollo", calories = 700, proteinGrams = 50, carbsGrams = 70, fatGrams = 15 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{url}/water", new { amountMl = 500 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{url}/water", new { amountMl = 250 })).StatusCode);

        var summaryResponse = await athlete.GetAsync($"{url}/summary");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        using var summaryBody = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        var summary = summaryBody.RootElement.GetProperty("value");
        Assert.Equal(1200, summary.GetProperty("consumed").GetProperty("calories").GetInt32());
        Assert.Equal(750, summary.GetProperty("consumed").GetProperty("waterMl").GetInt32());
        Assert.Equal(800, summary.GetProperty("remaining").GetProperty("calories").GetInt32());
        Assert.Equal(60m, summary.GetProperty("percentages").GetProperty("calories").GetDecimal());
        Assert.Equal(2, summary.GetProperty("foods").GetArrayLength());

        // Otro alumno que no existe → 403 para un alumno; el owner debe indicar alumno.
        var other = await athlete.GetAsync($"{url}/summary?athleteProfileId={Guid.CreateVersion7()}");
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
        using var owner = CreateClient();
        await LoginAsync(owner, ownerEmail);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"{url}/summary")).StatusCode);
    }

    [Fact]
    public async Task Challenge_flow_ranks_participants_by_points_and_awards_winner()
    {
        var (slug, ownerEmail, athleteEmail, seed) = await NewTenantAsync();
        using var owner = CreateClient();
        using var athlete = CreateClient();
        await LoginAsync(owner, ownerEmail);
        await LoginAsync(athlete, athleteEmail);
        var url = $"/api/v1/t/{slug}/challenges";

        // Un alumno no crea retos.
        var forbidden = await SendAsync(athlete, HttpMethod.Post, url, NewChallengeBody());
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var created = await SendAsync(owner, HttpMethod.Post, url, NewChallengeBody());
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var challengeId = (await ReadAsync(created)).GetProperty("value").GetProperty("id").GetGuid();

        // En borrador: no se puede unir y el alumno ni siquiera lo ve.
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(athlete, HttpMethod.Post, $"{url}/{challengeId}/join", new { })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Post, $"{url}/{challengeId}/activate", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(owner, HttpMethod.Post, $"{url}/{challengeId}/activate", new { })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{url}/{challengeId}/join", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(athlete, HttpMethod.Post, $"{url}/{challengeId}/join", new { })).StatusCode);

        // Solo el personal suma puntos.
        var selfPoints = await SendAsync(athlete, HttpMethod.Post, $"{url}/{challengeId}/activities",
            new { athleteProfileId = seed.AthleteProfileId, points = 5 });
        Assert.Equal(HttpStatusCode.Forbidden, selfPoints.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(owner, HttpMethod.Post, $"{url}/{challengeId}/activities",
            new { athleteProfileId = seed.AthleteProfileId, points = 0 })).StatusCode);
        foreach (var points in new[] { 10, 15 })
        {
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Post, $"{url}/{challengeId}/activities",
                new { athleteProfileId = seed.AthleteProfileId, points, note = "Entreno" })).StatusCode);
        }

        var board = (await ReadAsync(await athlete.GetAsync($"{url}/{challengeId}/leaderboard"))).GetProperty("value");
        Assert.Equal(1, board.GetArrayLength());
        Assert.Equal(25, board[0].GetProperty("points").GetInt32());
        Assert.Equal(1, board[0].GetProperty("rank").GetInt32());
        Assert.Equal("Ana Alumna", board[0].GetProperty("athleteName").GetString());

        var listed = (await ReadAsync(await athlete.GetAsync(url))).GetProperty("value");
        Assert.True(listed[0].GetProperty("joined").GetBoolean());

        // Al finalizar, quien lidera recibe un logro y una notificación.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Post, $"{url}/{challengeId}/complete", new { })).StatusCode);
        var achievements = (await ReadAsync(await athlete.GetAsync($"/api/v1/t/{slug}/achievements"))).GetProperty("value");
        Assert.Equal(1, achievements.GetArrayLength());
        Assert.Contains("Ganador", achievements[0].GetProperty("title").GetString());

        var notifications = (await ReadAsync(await athlete.GetAsync($"/api/v1/t/{slug}/notifications"))).GetProperty("value");
        Assert.Equal(1, notifications.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Staff_marks_attendance_and_athlete_only_reads()
    {
        var (slug, ownerEmail, athleteEmail, seed) = await NewTenantAsync();
        using var owner = CreateClient();
        using var athlete = CreateClient();
        await LoginAsync(owner, ownerEmail);
        await LoginAsync(athlete, athleteEmail);
        var url = $"/api/v1/t/{slug}/attendance";
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(athlete, HttpMethod.Post, url,
            new { athleteProfileId = seed.AthleteProfileId, date = today, status = "Present" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Post, url,
            new { athleteProfileId = seed.AthleteProfileId, date = today, status = "Absent" })).StatusCode);

        // Upsert: el mismo día se actualiza, no se duplica.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Put, url,
            new { athleteProfileId = seed.AthleteProfileId, date = today, status = "Present", notes = "Llegó temprano" })).StatusCode);

        var records = (await ReadAsync(await owner.GetAsync(url))).GetProperty("value");
        Assert.Equal(1, records.GetArrayLength());
        Assert.Equal("Present", records[0].GetProperty("status").GetString());
        Assert.Equal("Ana Alumna", records[0].GetProperty("athleteName").GetString());

        var own = (await ReadAsync(await athlete.GetAsync(url))).GetProperty("value");
        Assert.Equal(1, own.GetArrayLength());

        var stats = (await ReadAsync(await athlete.GetAsync($"{url}/stats"))).GetProperty("value");
        Assert.Equal(1, stats.GetProperty("total").GetInt32());
        Assert.Equal(100m, stats.GetProperty("presentPercent").GetDecimal());
    }

    [Fact]
    public async Task Chat_conversation_messages_unread_and_notifications()
    {
        var (slug, ownerEmail, athleteEmail, seed) = await NewTenantAsync();
        using var owner = CreateClient();
        using var athlete = CreateClient();
        await LoginAsync(owner, ownerEmail);
        await LoginAsync(athlete, athleteEmail);
        var url = $"/api/v1/t/{slug}/chat";

        var contacts = (await ReadAsync(await athlete.GetAsync($"{url}/contacts"))).GetProperty("value");
        Assert.Equal(1, contacts.GetArrayLength());

        // Directa entre dos personas; repetirla reutiliza la existente.
        var conversation = await SendAsync(owner, HttpMethod.Post, $"{url}/conversations",
            new { kind = "Direct", participantUserIds = new[] { seed.AthleteUserId } });
        Assert.Equal(HttpStatusCode.OK, conversation.StatusCode);
        var conversationId = (await ReadAsync(conversation)).GetProperty("value").GetProperty("id").GetGuid();
        var again = await SendAsync(owner, HttpMethod.Post, $"{url}/conversations",
            new { kind = "Direct", participantUserIds = new[] { seed.AthleteUserId } });
        Assert.Equal(conversationId, (await ReadAsync(again)).GetProperty("value").GetProperty("id").GetGuid());

        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(owner, HttpMethod.Post, $"{url}/conversations",
            new { kind = "Direct", participantUserIds = new[] { Guid.CreateVersion7() } })).StatusCode);

        var sent = await SendAsync(owner, HttpMethod.Post, $"{url}/conversations/{conversationId}/messages", new { body = "¡Hola Ana!" });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var message = (await ReadAsync(sent)).GetProperty("value");
        Assert.Equal("¡Hola Ana!", message.GetProperty("body").GetString());
        Assert.Equal("Olivia Owner", message.GetProperty("senderName").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(owner, HttpMethod.Post, $"{url}/conversations/{conversationId}/messages", new { body = "  " })).StatusCode);

        var athleteView = (await ReadAsync(await athlete.GetAsync($"{url}/conversations"))).GetProperty("value");
        Assert.Equal(1, athleteView.GetArrayLength());
        Assert.Equal(1, athleteView[0].GetProperty("unreadCount").GetInt32());
        Assert.Equal("Olivia Owner", athleteView[0].GetProperty("title").GetString());

        var messages = (await ReadAsync(await athlete.GetAsync($"{url}/conversations/{conversationId}/messages"))).GetProperty("value");
        Assert.Equal(1, messages.GetArrayLength());

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{url}/conversations/{conversationId}/read", new { })).StatusCode);
        var afterRead = (await ReadAsync(await athlete.GetAsync($"{url}/conversations"))).GetProperty("value");
        Assert.Equal(0, afterRead[0].GetProperty("unreadCount").GetInt32());

        // Una persona ajena a la conversación no puede leerla.
        var outsiderEmail = $"outsider.{Guid.CreateVersion7():N}@animalfitness.test";
        await AddMemberAsync(slug, outsiderEmail, MembershipRole.Trainer);
        using var outsider = CreateClient();
        await LoginAsync(outsider, outsiderEmail);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"{url}/conversations/{conversationId}/messages")).StatusCode);

        // La notificación del mensaje existe y se marca como leída.
        var notificationsUrl = $"/api/v1/t/{slug}/notifications";
        var list = (await ReadAsync(await athlete.GetAsync(notificationsUrl))).GetProperty("value");
        Assert.Equal(1, list.GetProperty("unreadCount").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(athlete, HttpMethod.Post, $"{notificationsUrl}/read-all", new { })).StatusCode);
        var cleared = (await ReadAsync(await athlete.GetAsync($"{notificationsUrl}?unreadOnly=true"))).GetProperty("value");
        Assert.Equal(0, cleared.GetProperty("unreadCount").GetInt32());
    }

    private static object NewChallengeBody()
        => new
        {
            name = "Reto de noviembre",
            kind = "Individual",
            metric = "ManualPoints",
            startsAt = DateTimeOffset.UtcNow.AddDays(-1),
            endsAt = DateTimeOffset.UtcNow.AddDays(30),
        };

    private async Task<(string Slug, string OwnerEmail, string AthleteEmail, Seed Seed)> NewTenantAsync()
    {
        var slug = $"p5-{Guid.CreateVersion7():N}"[..20];
        var ownerEmail = $"owner.{Guid.CreateVersion7():N}@animalfitness.test";
        var athleteEmail = $"athlete.{Guid.CreateVersion7():N}@animalfitness.test";
        var seed = await SeedAsync(slug, ownerEmail, athleteEmail);
        return (slug, ownerEmail, athleteEmail, seed);
    }

    private async Task<Seed> SeedAsync(string slug, string ownerEmail, string athleteEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var ownerId = await CreateUserAsync(userManager, db, ownerEmail, "Olivia", "Owner", now);
        var athleteUserId = await CreateUserAsync(userManager, db, athleteEmail, "Ana", "Alumna", now);

        var tenant = Tenant.Create("Academia Fase 5", slug, ownerEmail, now);
        tenant.Activate(now.AddMinutes(1), ownerId);
        db.Tenants.Add(tenant);
        db.OrganizationSettings.Add(TenantSettings.Create(tenant.Id, now, ownerId));
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, ownerId, MembershipRole.Owner, now, ownerId));
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, athleteUserId, MembershipRole.Athlete, now, ownerId));
        var profile = AthleteProfile.Create(tenant.Id, athleteUserId, now, ownerId);
        db.AthleteProfiles.Add(profile);
        await db.SaveChangesAsync();
        return new Seed(ownerId, athleteUserId, profile.Id);
    }

    private async Task AddMemberAsync(string slug, string email, MembershipRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var userId = await CreateUserAsync(userManager, db, email, "Otto", "Ajeno", now);
        var tenant = db.Tenants.Single(item => item.Slug == slug);
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, userId, role, now, userId));
        await db.SaveChangesAsync();
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

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
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
