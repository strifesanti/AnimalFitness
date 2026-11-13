namespace AnimalFitness.Domain.Common;

/// <summary>
/// Fechas y autores de creación y modificación. La aplicación los informa; no se aceptan desde el cliente sin verificar.
/// </summary>
public interface IAuditable
{
    /// <summary>Momento de creación, en UTC.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>Usuario que creó el registro. Nulo cuando lo crea un proceso del sistema.</summary>
    Guid? CreatedBy { get; }

    /// <summary>Momento de la última modificación, en UTC.</summary>
    DateTimeOffset? UpdatedAt { get; }

    /// <summary>Usuario que hizo la última modificación.</summary>
    Guid? UpdatedBy { get; }
}
