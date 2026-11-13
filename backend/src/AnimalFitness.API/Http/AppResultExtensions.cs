using AnimalFitness.Application.Common;

namespace AnimalFitness.API.Http;

/// <summary>Convierte <see cref="AppResult"/> a respuestas HTTP.</summary>
public static class AppResultExtensions
{
    /// <summary>Mapea el resultado de aplicación a un <see cref="IResult"/>.</summary>
    public static IResult ToHttpResult(this AppResult result)
    {
        if (result is AppResult.Failure failure)
        {
            return Results.Problem(
                title: failure.Title,
                detail: failure.Detail,
                statusCode: failure.StatusCode,
                type: "https://tools.ietf.org/html/rfc9457");
        }

        var type = result.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AppResult.Success<>))
        {
            var value = type.GetProperty("Value")!.GetValue(result);
            var message = type.GetProperty("Message")!.GetValue(result) as string;
            return Results.Ok(new { value, message });
        }

        if (result is AppResult.Success success)
        {
            return Results.Ok(new { message = success.Message });
        }

        return Results.Problem(title: "Resultado no contemplado.", statusCode: 500);
    }
}
