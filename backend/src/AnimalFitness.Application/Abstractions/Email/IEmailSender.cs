namespace AnimalFitness.Application.Abstractions.Email;

/// <summary>
/// Envío de correo. La implementación de producción usará un proveedor real;
/// en Development puede usarse una implementación provisional claramente identificada.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envía un correo.
    /// </summary>
    /// <param name="to">Destinatario.</param>
    /// <param name="subject">Asunto.</param>
    /// <param name="htmlBody">Cuerpo HTML.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
