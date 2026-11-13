using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

internal sealed class TrainerProfileConfiguration : IEntityTypeConfiguration<TrainerProfile>
{
    public void Configure(EntityTypeBuilder<TrainerProfile> builder)
    {
        builder.ToTable("TrainerProfiles");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(profile => profile.TenantId).IsRequired();
        builder.HasAlternateKey(profile => new { profile.TenantId, profile.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(profile => profile.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(profile => profile.UserId).IsRequired();
        builder.Property(profile => profile.Bio).HasMaxLength(FieldLengths.ProfileBio);
        builder.Property(profile => profile.Specialties).HasMaxLength(FieldLengths.ProfileSpecialties);

        builder.HasIndex(profile => new { profile.TenantId, profile.UserId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
