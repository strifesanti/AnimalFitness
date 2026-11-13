namespace AnimalFitness.Domain.Challenges;

/// <summary>Estado del reto.</summary>
public enum ChallengeStatus
{
    /// <summary>Borrador.</summary>
    Draft = 1,

    /// <summary>Activo.</summary>
    Active = 2,

    /// <summary>Finalizado.</summary>
    Completed = 3,

    /// <summary>Cancelado.</summary>
    Cancelled = 4,
}
