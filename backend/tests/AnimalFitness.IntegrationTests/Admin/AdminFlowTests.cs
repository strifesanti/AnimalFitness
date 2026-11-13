using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalFitness.Application.Features.Auth;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using AnimalFitness.Infrastructure.Security;
using AnimalFitness.IntegrationTests.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalFitness.IntegrationTests.Admin;

public sealed class AdminFlowTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AdminFlowTests(AuthApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSchema();
    }

    [Fact]
    public async Task Join_request_approve_grants_athlete_membership()
    {
        var slug = $"join-{Guid.CreateVersion7():N}"[..20];
        var ownerEmail = $"owner.{Guid.CreateVersion7():N}@animalfitness.test";
        var athleteEmail = $"athlete.{Guid.CreateVersion7():N}@animalfitness.test";
        const string password = "ClaveSegura1";

        await SeedTenantWithOwnerAsync(slug, ownerEmail, password);

        using var athleteClient = CreateClient();
        await RegisterAndLoginAsync(athleteClient, athleteEmail, password, "Ana", "Alumna");

        var csrf = await GetCsrfAsync(athleteClient);
        using var submit = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/v1/organizations/{slug}/join-requests",
            new { },
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await athleteClient.SendAsync(submit)).StatusCode);

        using var ownerClient = CreateClient();
        await LoginAsync(ownerClient, ownerEmail, password);

        var listResponse = await ownerClient.GetAsync($"/api/v1/t/{slug}/join-requests");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listBody = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var requestId = listBody.RootElement.GetProperty("value")[0].GetProperty("id").GetGuid();

        csrf = await GetCsrfAsync(ownerClient);
        using var approve = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/v1/t/{slug}/join-requests/{requestId}/approve",
            new { },
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.SendAsync(approve)).StatusCode);

        var members = await ownerClient.GetAsync($"/api/v1/t/{slug}/members?role=Athlete");
        Assert.Equal(HttpStatusCode.OK, members.StatusCode);
        using var membersBody = JsonDocument.Parse(await members.Content.ReadAsStringAsync());
        var emails = membersBody.RootElement.GetProperty("value").EnumerateArray()
            .Select(item => item.GetProperty("email").GetString())
            .ToArray();
        Assert.Contains(athleteEmail, emails);
    }

    [Fact]
    public async Task Invitation_accept_adds_membership_for_matching_email()
    {
        var slug = $"inv-{Guid.CreateVersion7():N}"[..20];
        var ownerEmail = $"owner.{Guid.CreateVersion7():N}@animalfitness.test";
        var trainerEmail = $"trainer.{Guid.CreateVersion7():N}@animalfitness.test";
        const string password = "ClaveSegura1";

        var (tenantId, ownerId) = await SeedTenantWithOwnerAsync(slug, ownerEmail, password);

        using var trainerClient = CreateClient();
        await RegisterAndLoginAsync(trainerClient, trainerEmail, password, "Tomas", "Trainer");

        var (plainToken, hash) = InvitationToken.Create();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            db.Invitations.Add(Invitation.Create(
                tenantId,
                trainerEmail,
                MembershipRole.Trainer,
                hash,
                now.AddDays(3),
                now,
                ownerId));
            await db.SaveChangesAsync();
        }

        var csrf = await GetCsrfAsync(trainerClient);
        using var accept = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/invitations/accept",
            new AcceptInvitationRequest(plainToken),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await trainerClient.SendAsync(accept)).StatusCode);

        using var ownerClient = CreateClient();
        await LoginAsync(ownerClient, ownerEmail, password);
        var trainers = await ownerClient.GetAsync($"/api/v1/t/{slug}/members?role=Trainer");
        Assert.Equal(HttpStatusCode.OK, trainers.StatusCode);
        using var body = JsonDocument.Parse(await trainers.Content.ReadAsStringAsync());
        var emails = body.RootElement.GetProperty("value").EnumerateArray()
            .Select(item => item.GetProperty("email").GetString())
            .ToArray();
        Assert.Contains(trainerEmail, emails);
    }

    private async Task<(Guid TenantId, Guid OwnerId)> SeedTenantWithOwnerAsync(
        string slug,
        string ownerEmail,
        string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var ownerId = Guid.CreateVersion7();
        var account = new ApplicationUser(ownerId)
        {
            UserName = ownerEmail,
            Email = ownerEmail,
            EmailConfirmed = true,
        };
        Assert.True((await userManager.CreateAsync(account, password)).Succeeded);

        db.UserProfiles.Add(User.Create(ownerEmail, "Olivia", "Owner", now, id: ownerId));
        var tenant = Tenant.Create("Academia Test", slug, ownerEmail, now);
        tenant.Activate(now.AddMinutes(1), ownerId);
        db.Tenants.Add(tenant);
        db.OrganizationSettings.Add(TenantSettings.Create(tenant.Id, now, ownerId));
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, ownerId, MembershipRole.Owner, now, ownerId));
        await db.SaveChangesAsync();
        return (tenant.Id, ownerId);
    }

    private async Task RegisterAndLoginAsync(
        HttpClient client,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        var csrf = await GetCsrfAsync(client);
        using var register = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/register",
            new RegisterRequest(email, password, firstName, lastName, null),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(register)).StatusCode);
    }

    private async Task LoginAsync(HttpClient client, string email, string password)
    {
        var csrf = await GetCsrfAsync(client);
        using var login = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/login",
            new LoginRequest(email, password),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login)).StatusCode);
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
