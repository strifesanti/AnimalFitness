using AnimalFitness.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reglas comunes de mapeo para entidades auditables y de borrado lógico.
/// </summary>
internal static class ModelConfigurationExtensions
{
    /// <summary>
    /// Clave primaria asignada por el dominio, auditoría y token de concurrencia.
    /// </summary>
    internal static void ConfigureAuditable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy);
        builder.Property(entity => entity.UpdatedAt);
        builder.Property(entity => entity.UpdatedBy);
        builder.Property(entity => entity.RowVersion).IsConcurrencyToken();
    }

    /// <summary>
    /// Columnas de borrado lógico.
    /// </summary>
    internal static void ConfigureSoftDeletable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : SoftDeletableEntity
    {
        builder.Property(entity => entity.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(entity => entity.DeletedAt);
    }
}
