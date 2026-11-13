namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Cabeceras HTTP relacionadas con la organización activa.
/// </summary>
public static class TenantHttpHeaders
{
    /// <summary>Cabecera con el slug de la organización candidata.</summary>
    public const string Tenant = "X-Tenant";
}
