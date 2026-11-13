using Microsoft.AspNetCore.Diagnostics;

namespace AnimalFitness.API.Errors;

/// <summary>
/// Convierte las excepciones no controladas en una respuesta Problem Details.
/// Fuera de Development no incluye el mensaje ni el detalle interno.
/// </summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Error no controlado al procesar {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value);

        var problem = TypedResults.Problem(
            title: "Ocurrió un error inesperado.",
            detail: environment.IsDevelopment() ? exception.Message : null,
            statusCode: StatusCodes.Status500InternalServerError,
            instance: httpContext.Request.Path.Value,
            type: "https://tools.ietf.org/html/rfc9457");

        problem.ProblemDetails.Extensions.TryAdd("traceId", httpContext.TraceIdentifier);
        await problem.ExecuteAsync(httpContext);
        return true;
    }
}
