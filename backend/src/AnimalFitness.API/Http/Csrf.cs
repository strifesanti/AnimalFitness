using Microsoft.AspNetCore.Antiforgery;

namespace AnimalFitness.API.Http;

/// <summary>Validación CSRF compartida.</summary>
public static class Csrf
{
    /// <summary>Valida el token o devuelve un Problem Details.</summary>
    public static async Task<IResult?> ValidateAsync(IAntiforgery antiforgery, HttpContext httpContext)
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
}
