namespace AnimalFitness.Domain.Access;

/// <summary>
/// Estado de una invitación a una organización.
/// </summary>
public enum InvitationStatus
{
    /// <summary>Enviada y todavía utilizable.</summary>
    Pending = 1,

    /// <summary>La persona aceptó y quedó vinculada.</summary>
    Accepted = 2,

    /// <summary>Pasó la fecha de vencimiento sin aceptarse.</summary>
    Expired = 3,

    /// <summary>La canceló quien la emitió.</summary>
    Cancelled = 4,
}
