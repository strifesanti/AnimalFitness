using AnimalFitness.Domain.Auditing;
using AnimalFitness.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="AuditLog"/>. Sin claves foráneas, para que el historial sobreviva a cualquier cambio en las tablas de negocio.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.TenantId);
        builder.Property(entry => entry.UserId);
        builder.Property(entry => entry.Action).HasMaxLength(FieldLengths.AuditAction).IsRequired();
        builder.Property(entry => entry.EntityType).HasMaxLength(FieldLengths.EntityType).IsRequired();
        builder.Property(entry => entry.EntityId);
        builder.Property(entry => entry.ChangesJson);
        builder.Property(entry => entry.IpAddress).HasMaxLength(FieldLengths.IpAddress);
        builder.Property(entry => entry.UserAgent).HasMaxLength(FieldLengths.UserAgent);
        builder.Property(entry => entry.OccurredAt).IsRequired();

        builder.HasIndex(entry => new { entry.TenantId, entry.OccurredAt });
        builder.HasIndex(entry => new { entry.UserId, entry.OccurredAt });
        builder.HasIndex(entry => entry.Action);
    }
}
