using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalFitness.API.Auth;

/// <summary>
/// Cookies de sesión, antiforgery y rate limiting de autenticación.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    /// <summary>Nombre de la política de rate limiting para autenticación.</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// Configura autenticación por cookie Identity, CSRF y límite de intentos.
    /// </summary>
    public static IServiceCollection AddAnimalFitnessAuth(this IServiceCollection services, IHostEnvironment environment)
    {
        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            // Prefijo __Host- solo en Staging/Production (exige HTTPS).
            // En Development/Testing se usa HTTP local con el frontend Next.js.
            var hardened = environment.IsProduction() || environment.IsStaging();
            options.Cookie.Name = hardened ? "__Host-AnimalFitness.Auth" : "AnimalFitness.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = hardened
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "AnimalFitness.XSRF";
            options.Cookie.HttpOnly = false;
            options.Cookie.SecurePolicy = environment.IsProduction() || environment.IsStaging()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        services.AddAuthorization();
        return services;
    }
}
