namespace AnimalFitness.Infrastructure.Persistence;

/// <summary>
/// Intento de escribir o mover un registro fuera de la organización activa.
/// Es un error de seguridad: la capa de API debe responder 403 y registrar el hecho.
/// </summary>
public sealed class TenantViolationException : Exception
{
    /// <summary>
    /// Crea la excepción con el motivo del bloqueo.
    /// </summary>
    /// <param name="message">Descripción del bloqueo.</param>
    public TenantViolationException(string message)
        : base(message)
    {
    }
}
