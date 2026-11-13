using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="TenantSettings"/>. Una fila por organización.
/// </summary>
internal sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("TenantSettings");

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(settings => settings.TenantId).IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(settings => settings.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(settings => settings.TenantId).IsUnique();

        builder.Property(settings => settings.PrimaryColor).HasMaxLength(FieldLengths.Color).IsRequired();
        builder.Property(settings => settings.Locale).HasMaxLength(FieldLengths.Locale).IsRequired();
        builder.Property(settings => settings.Currency).HasMaxLength(FieldLengths.Currency).IsRequired();
        builder.Property(settings => settings.SettingsJson).HasMaxLength(FieldLengths.SettingsJson).IsRequired();
    }
}
