using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="Tenant"/>. Es global: no tiene filtro por organización.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", table => table.HasCheckConstraint(
            "CK_Tenants_Status",
            $"[Status] IN ({(int)TenantStatus.Trial}, {(int)TenantStatus.Active}, {(int)TenantStatus.Suspended})"));

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(tenant => tenant.Name).HasMaxLength(FieldLengths.OrganizationName).IsRequired();
        builder.Property(tenant => tenant.Slug).HasMaxLength(FieldLengths.Slug).IsRequired();
        builder.HasIndex(tenant => tenant.Slug).IsUnique();
        builder.Property(tenant => tenant.Status).IsRequired();
        builder.Property(tenant => tenant.LogoFileId);
        builder.Property(tenant => tenant.ContactEmail).HasMaxLength(FieldLengths.Email).IsRequired();
        builder.Property(tenant => tenant.Phone).HasMaxLength(FieldLengths.Phone);
        builder.Property(tenant => tenant.TimeZoneId).HasMaxLength(FieldLengths.TimeZoneId).IsRequired();
        builder.Property(tenant => tenant.Currency).HasMaxLength(FieldLengths.Currency).IsRequired();
    }
}
