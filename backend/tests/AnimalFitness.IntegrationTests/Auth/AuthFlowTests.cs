using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalFitness.Application.Features.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AnimalFitness.IntegrationTests.Auth;

public sealed class AuthFlowTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthFlowTests(AuthApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSchema();
    }

    [Fact]
    public async Task Register_login_me_and_logout_work_with_csrf_and_cookies()
    {
        using var client = CreateClient();
        var email = $"athlete.{Guid.CreateVersion7():N}@animalfitness.test";
        var password = "ClaveSegura1";

        var csrf = await GetCsrfAsync(client);
        using var registerRequest = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/register",
            new RegisterRequest(email, password, "Ana", "Pérez", null),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(registerRequest)).StatusCode);

        var meAfterRegister = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meAfterRegister.StatusCode);
        using (var meBody = JsonDocument.Parse(await meAfterRegister.Content.ReadAsStringAsync()))
        {
            Assert.Equal(email, meBody.RootElement.GetProperty("user").GetProperty("email").GetString());
            Assert.False(meBody.RootElement.GetProperty("user").GetProperty("emailConfirmed").GetBoolean());
        }

        csrf = await GetCsrfAsync(client);
        using var logoutRequest = CreateJsonRequest(HttpMethod.Post, "/api/v1/auth/logout", new { }, csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(logoutRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);

        csrf = await GetCsrfAsync(client);
        using var loginRequest = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/login",
            new LoginRequest(email, password),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(loginRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Mutating_auth_without_csrf_is_rejected()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("nadie@animalfitness.test", "ClaveSegura1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CSRF", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        using var client = CreateClient();
        var email = $"wrong.{Guid.CreateVersion7():N}@animalfitness.test";
        var csrf = await GetCsrfAsync(client);

        using var registerRequest = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/register",
            new RegisterRequest(email, "ClaveSegura1", "Luis", "Gómez", null),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(registerRequest)).StatusCode);

        csrf = await GetCsrfAsync(client);
        using var logoutRequest = CreateJsonRequest(HttpMethod.Post, "/api/v1/auth/logout", new { }, csrf);
        await client.SendAsync(logoutRequest);

        csrf = await GetCsrfAsync(client);
        using var loginRequest = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/login",
            new LoginRequest(email, "ClaveIncorrecta1"),
            csrf);
        var loginResponse = await client.SendAsync(loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Duplicate_register_returns_neutral_success()
    {
        using var client = CreateClient();
        var email = $"dup.{Guid.CreateVersion7():N}@animalfitness.test";

        var csrf = await GetCsrfAsync(client);
        using var first = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/register",
            new RegisterRequest(email, "ClaveSegura1", "Ana", "Pérez", null),
            csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(first)).StatusCode);

        csrf = await GetCsrfAsync(client);
        using var second = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/register",
            new RegisterRequest(email, "ClaveSegura1", "Ana", "Pérez", null),
            csrf);
        var response = await client.SendAsync(second);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Revisa tu correo", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Forgot_password_is_always_neutral()
    {
        using var client = CreateClient();
        var csrf = await GetCsrfAsync(client);
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/v1/auth/forgot-password",
            new ForgotPasswordRequest("noexiste@animalfitness.test"),
            csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Si la cuenta existe", await response.Content.ReadAsStringAsync());
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
