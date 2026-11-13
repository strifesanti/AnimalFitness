namespace AnimalFitness.Domain.Organizations;

/// <summary>
/// Estado operativo de una organización.
/// </summary>
public enum TenantStatus
{
    /// <summary>Periodo de prueba, antes de activar la suscripción.</summary>
    Trial = 0,

    /// <summary>Organización operativa.</summary>
    Active = 1,

    /// <summary>Organización bloqueada. Sus miembros no operan hasta que se reactive.</summary>
    Suspended = 2,
}
