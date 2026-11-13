namespace AnimalFitness.Application.Common;

/// <summary>Resultado de un caso de uso de aplicación.</summary>
public abstract record AppResult
{
    /// <summary>Operación correcta sin cuerpo tipado.</summary>
    /// <param name="Message">Mensaje opcional para el cliente.</param>
    public sealed record Success(string? Message = null) : AppResult;

    /// <summary>Operación correcta con cuerpo.</summary>
    /// <typeparam name="T">Tipo del cuerpo.</typeparam>
    /// <param name="Value">Cuerpo de la respuesta.</param>
    /// <param name="Message">Mensaje opcional para el cliente.</param>
    public sealed record Success<T>(T Value, string? Message = null) : AppResult;

    /// <summary>Fallo controlado expuesto al cliente.</summary>
    /// <param name="Title">Título del problema.</param>
    /// <param name="StatusCode">Código HTTP.</param>
    /// <param name="Detail">Detalle opcional.</param>
    public sealed record Failure(string Title, int StatusCode, string? Detail = null) : AppResult;
}
