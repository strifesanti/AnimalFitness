namespace AnimalFitness.Domain;

/// <summary>
/// Constantes compartidas del dominio.
/// </summary>
public static class DomainConstants
{
    /// <summary>
    /// Moneda predeterminada de la plataforma.
    /// </summary>
    public const string DefaultCurrency = "COP";

    /// <summary>
    /// Cultura predeterminada de la plataforma.
    /// </summary>
    public const string DefaultCulture = "es-CO";

    /// <summary>
    /// Zona horaria predeterminada. Las fechas se guardan en UTC y se presentan en esta zona.
    /// </summary>
    public const string DefaultTimeZoneId = "America/Bogota";

    /// <summary>
    /// Color de acento predeterminado de una organización (púrpura de marca).
    /// </summary>
    public const string DefaultAccentColor = "#7C3AED";

    /// <summary>
    /// Configuración JSON vacía.
    /// </summary>
    public const string EmptySettingsJson = "{}";
}
