namespace AnimalFitness.Domain.Common;

/// <summary>
/// Incumplimiento de una regla de negocio. No representa un error técnico.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Crea la excepción con el mensaje predeterminado.
    /// </summary>
    public DomainException()
    {
    }

    /// <summary>
    /// Crea la excepción con un mensaje comprensible para quien opera el sistema.
    /// </summary>
    /// <param name="message">Descripción de la regla incumplida.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Crea la excepción con un mensaje y la causa original.
    /// </summary>
    /// <param name="message">Descripción de la regla incumplida.</param>
    /// <param name="innerException">Error que originó el incumplimiento.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
