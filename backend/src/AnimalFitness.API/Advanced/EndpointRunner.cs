using AnimalFitness.API.Http;
using AnimalFitness.Application.Common;
using Microsoft.AspNetCore.Antiforgery;

namespace AnimalFitness.API.Advanced;

/// <summary>Valida CSRF y ejecuta la acción de un endpoint que modifica datos.</summary>
internal static class EndpointRunner
{
    /// <summary>Valida el token CSRF y, si es correcto, ejecuta la acción y la convierte en respuesta HTTP.</summary>
    public static async Task<IResult> RunAsync(
        IAntiforgery antiforgery,
        HttpContext http,
        Func<Task<AppResult>> action)
    {
        var csrf = await Csrf.ValidateAsync(antiforgery, http);
        return csrf ?? (await action()).ToHttpResult();
    }
}
