namespace AnimalFitness.Domain.Accounts;

/// <summary>
/// Estado de la pertenencia de una persona a una organización.
/// </summary>
public enum MembershipStatus
{
    /// <summary>Puede operar según su rol.</summary>
    Active = 1,

    /// <summary>Sigue vinculada, pero no puede operar.</summary>
    Suspended = 2,

    /// <summary>La vinculación terminó. Para volver a entrar hace falta una membresía nueva.</summary>
    Removed = 3,
}
