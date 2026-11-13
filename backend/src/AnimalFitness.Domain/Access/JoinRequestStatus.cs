namespace AnimalFitness.Domain.Access;

/// <summary>
/// Estado de una solicitud de ingreso a una organización.
/// </summary>
public enum JoinRequestStatus
{
    /// <summary>Espera la decisión de la organización.</summary>
    Pending = 1,

    /// <summary>La organización aprobó el ingreso.</summary>
    Approved = 2,

    /// <summary>La organización rechazó el ingreso.</summary>
    Rejected = 3,

    /// <summary>La persona retiró la solicitud.</summary>
    Cancelled = 4,
}
