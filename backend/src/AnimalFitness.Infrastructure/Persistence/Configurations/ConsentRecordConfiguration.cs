using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Compliance;
using AnimalFitness.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="ConsentRecord"/>. No tiene borrado lógico: las autorizaciones se revocan, no se eliminan.
/// </summary>
internal sealed class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.ToTable("ConsentRecords");

        builder.ConfigureAuditable();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(record => record.UserId).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(record => record.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(record => record.IsActive);
        builder.Property(record => record.ConsentType).IsRequired();
        builder.Property(record => record.PolicyVersion).HasMaxLength(FieldLengths.PolicyVersion).IsRequired();
        builder.Property(record => record.GrantedAt).IsRequired();
        builder.Property(record => record.RevokedAt);
        builder.Property(record => record.IpAddress).HasMaxLength(FieldLengths.IpAddress);

        builder.HasIndex(record => new { record.UserId, record.ConsentType });
        builder.HasIndex(record => record.TenantId);
    }
}
