namespace AnimalFitness.Domain.Common;

/// <summary>
/// Borrado lógico. El registro deja de verse, pero se conserva para auditoría y recuperación.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Indica si el registro fue eliminado.</summary>
    bool IsDeleted { get; }

    /// <summary>Momento del borrado lógico, en UTC.</summary>
    DateTimeOffset? DeletedAt { get; }
}
