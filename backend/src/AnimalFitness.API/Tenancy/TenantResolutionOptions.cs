namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Opciones de resolución de organización a partir del host.
/// </summary>
public sealed class TenantResolutionOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "TenantResolution";

    /// <summary>
    /// Dominio base de la plataforma, por ejemplo animalfitness.com.
    /// Un subdominio distinto de www se interpreta como slug. Vacío desactiva esta fuente.
    /// </summary>
    public string? BaseDomain { get; set; }
}
