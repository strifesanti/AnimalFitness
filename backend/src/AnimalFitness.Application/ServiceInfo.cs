namespace AnimalFitness.Application;

/// <summary>
/// Identidad pública del servicio.
/// </summary>
/// <param name="Name">Nombre del producto.</param>
/// <param name="Version">Versión informativa del servicio.</param>
public sealed record ServiceInfo(string Name, string Version)
{
    /// <summary>
    /// Valores actuales del servicio.
    /// </summary>
    public static ServiceInfo Current { get; } = new("AnimalFitness", "0.1.0");
}
