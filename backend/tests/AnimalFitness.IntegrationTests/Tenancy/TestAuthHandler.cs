using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnimalFitness.IntegrationTests.Tenancy;

/// <summary>
/// Autenticación de prueba. El identificador de persona se envía en la cabecera X-Test-User-Id.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Nombre del esquema.</summary>
    public const string SchemeName = "Test";

    /// <summary>Cabecera con el Guid de la persona autenticada.</summary>
    public const string UserIdHeader = "X-Test-User-Id";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var values)
            || !Guid.TryParse(values.FirstOrDefault(), out var userId)
            || userId == Guid.Empty)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
        ],
            SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
