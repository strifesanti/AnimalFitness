namespace AnimalFitness.Domain.Common;

/// <summary>
/// Entidad que se oculta con borrado lógico en lugar de desaparecer de la base de datos.
/// </summary>
public abstract class SoftDeletableEntity : BaseEntity, ISoftDeletable
{
    /// <summary>
    /// Crea la entidad con un identificador nuevo.
    /// </summary>
    protected SoftDeletableEntity()
    {
    }

    /// <summary>
    /// Crea la entidad con un identificador conocido.
    /// </summary>
    /// <param name="id">Identificador. No puede ser vacío.</param>
    protected SoftDeletableEntity(Guid id)
        : base(id)
    {
    }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Marca la entidad como eliminada. La operación no se puede deshacer desde el dominio.
    /// </summary>
    /// <param name="at">Momento del borrado, en UTC.</param>
    /// <param name="by">Usuario que elimina.</param>
    public void SoftDelete(DateTimeOffset at, Guid? by)
    {
        if (IsDeleted)
        {
            throw new DomainException("El registro ya fue eliminado.");
        }

        base.MarkUpdated(at, by);
        IsDeleted = true;
        DeletedAt = at;
    }

    /// <inheritdoc />
    protected override void MarkUpdated(DateTimeOffset at, Guid? by)
    {
        EnsureNotDeleted();
        base.MarkUpdated(at, by);
    }

    /// <summary>
    /// Impide operar sobre un registro eliminado.
    /// </summary>
    protected void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("No se puede modificar un registro eliminado.");
        }
    }
}
