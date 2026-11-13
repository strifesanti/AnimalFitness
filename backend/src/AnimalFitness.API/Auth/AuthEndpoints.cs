using AnimalFitness.Application.Features.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AnimalFitness.API.Auth;

/// <summary>
/// Endpoints de autenticación y sesión.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Registra las rutas bajo <c>/api/v1/auth</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapGet("/csrf", GetCsrfToken)
            .WithName("GetCsrfToken");

        group.MapPost("/register", Register)
            .RequireRateLimiting(AuthServiceCollectionExtensions.AuthRateLimitPolicy)
            .WithName("Register");

        group.MapPost("/login", Login)
            .RequireRateLimiting(AuthServiceCollectionExtensions.AuthRateLimitPolicy)
            .WithName("Login");

        group.MapPost("/logout", Logout)
            .WithName("Logout");

        group.MapGet("/me", Me)
            .WithName("GetMe");

        group.MapPost("/confirm-email", ConfirmEmail)
            .RequireRateLimiting(AuthServiceCollectionExtensions.AuthRateLimitPolicy)
            .WithName("ConfirmEmail");

        group.MapPost("/forgot-password", ForgotPassword)
            .RequireRateLimiting(AuthServiceCollectionExtensions.AuthRateLimitPolicy)
            .WithName("ForgotPassword");

        group.MapPost("/reset-password", ResetPassword)
            .RequireRateLimiting(AuthServiceCollectionExtensions.AuthRateLimitPolicy)
            .WithName("ResetPassword");

        return endpoints;
    }

    private static IResult GetCsrfToken(IAntiforgery antiforgery, HttpContext httpContext)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return Results.Ok(new { token = tokens.RequestToken });
    }

    private static Task<IResult> Register(
        [FromBody] RegisterRequest request,
        IAuthService authService,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => ExecuteMutatingAsync(
            antiforgery,
            httpContext,
            () => authService.RegisterAsync(request, cancellationToken));

    private static Task<IResult> Login(
        [FromBody] LoginRequest request,
        IAuthService authService,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => ExecuteMutatingAsync(
            antiforgery,
            httpContext,
            () => authService.LoginAsync(request, cancellationToken));

    private static async Task<IResult> Logout(
        IAuthService authService,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var csrf = await ValidateCsrfAsync(antiforgery, httpContext);
        if (csrf is not null)
        {
            return csrf;
        }

        await authService.LogoutAsync(cancellationToken);
        return Results.Ok(new { message = "Sesión cerrada." });
    }

    private static async Task<IResult> Me(IAuthService authService, CancellationToken cancellationToken)
        => ToResult(await authService.GetMeAsync(cancellationToken));

    private static Task<IResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        IAuthService authService,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => ExecuteMutatingAsync(
            antiforgery,
            httpContext,
            () => authService.ConfirmEmailAsync(request, cancellationToken));

    private static Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        IAuthService authService,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => ExecuteMutatingAsync(
            antiforgery,
            httpContext,
            () => authService.ForgotPasswordAsync(request, cancellationToken));

    private static Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        IAuthService authService,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => ExecuteMutatingAsync(
            antiforgery,
            httpContext,
            () => authService.ResetPasswordAsync(request, cancellationToken));

    private static async Task<IResult> ExecuteMutatingAsync(
        IAntiforgery antiforgery,
        HttpContext httpContext,
        Func<Task<AuthResult>> action)
    {
        var csrf = await ValidateCsrfAsync(antiforgery, httpContext);
        if (csrf is not null)
        {
            return csrf;
        }

        return ToResult(await action());
    }

    private static async Task<IResult?> ValidateCsrfAsync(IAntiforgery antiforgery, HttpContext httpContext)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: "Token CSRF inválido o ausente.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc9457");
        }
    }

    private static IResult ToResult(AuthResult result)
        => result switch
        {
            AuthResult.Success success when success.User is not null
                => Results.Ok(new { user = success.User, message = success.Message }),
            AuthResult.Success success
                => Results.Ok(new { message = success.Message }),
            AuthResult.Failure failure
                => Results.Problem(
                    title: failure.Title,
                    detail: failure.Detail,
                    statusCode: failure.StatusCode,
                    type: "https://tools.ietf.org/html/rfc9457"),
            _
                => Results.Problem(title: "Resultado de autenticación no contemplado.", statusCode: 500),
        };
}
