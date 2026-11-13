namespace AnimalFitness.Domain.Accounts;

/// <summary>
/// Rol de una persona dentro de una organización. No incluye el administrador global.
/// </summary>
public enum MembershipRole
{
    /// <summary>Propietario de la academia o gimnasio.</summary>
    Owner = 1,

    /// <summary>Entrenador.</summary>
    Trainer = 2,

    /// <summary>Alumno.</summary>
    Athlete = 3,
}
