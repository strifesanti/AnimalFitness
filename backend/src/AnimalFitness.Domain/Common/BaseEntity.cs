namespace AnimalFitness.Domain.Common;

/// <summary>
/// Identidad y auditoría de una entidad persistente. El identificador es un Guid versión 7, ordenable en el tiempo.
/// </summary>
public abstract class BaseEntity : IAuditable
{
    /// <summary>
    /// Crea la entidad con un identificador nuevo.
    /// </summary>
    protected BaseEntity()
        : this(Guid.CreateVersion7())
    {
    }

    /// <summary>
    /// Crea la entidad con un identificador conocido, por ejemplo el de la cuenta de Identity.
    /// </summary>
    /// <param name="id">Identificador. No puede ser vacío.</param>
    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("El identificador no es válido.");
        }

        Id = id;
    }

    /// <summary>Identificador de la entidad.</summary>
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; private set; }

    /// <summary>
    /// Token de concurrencia optimista. La base de datos lo actualiza en cada escritura.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    /// <summary>
    /// Registra la creación. Solo puede invocarse una vez.
    /// </summary>
    /// <param name="at">Momento de creación, en UTC.</param>
    /// <param name="by">Autor. Nulo si lo crea un proceso del sistema.</param>
    protected void MarkCreated(DateTimeOffset at, Guid? by)
    {
        if (CreatedAt != default)
        {
            throw new DomainException("La entidad ya tiene fecha de creación.");
        }

        if (at == default)
        {
            throw new DomainException("La fecha de creación no es válida.");
        }

        CreatedAt = at;
        CreatedBy = by;
    }

    /// <summary>
    /// Registra una modificación. No puede ser anterior a la creación.
    /// </summary>
    /// <param name="at">Momento de la modificación, en UTC.</param>
    /// <param name="by">Autor de la modificación.</param>
    protected virtual void MarkUpdated(DateTimeOffset at, Guid? by)
    {
        if (CreatedAt == default)
        {
            throw new DomainException("No se puede modificar una entidad sin fecha de creación.");
        }

        if (at < CreatedAt)
        {
            throw new DomainException("La fecha de modificación no puede ser anterior a la de creación.");
        }

        UpdatedAt = at;
        UpdatedBy = by;
    }
}
