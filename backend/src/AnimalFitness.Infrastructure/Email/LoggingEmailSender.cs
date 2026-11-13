using AnimalFitness.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace AnimalFitness.Infrastructure.Email;

/// <summary>
/// IMPLEMENTACIÓN PROVISIONAL DE DESARROLLO.
/// No envía correo real: escribe el mensaje en el log. Sustituir por un proveedor real antes de Staging/Production.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[Email provisional] Para={To} Asunto={Subject} Cuerpo={Body}",
            to,
            subject,
            htmlBody);
        return Task.CompletedTask;
    }
}
