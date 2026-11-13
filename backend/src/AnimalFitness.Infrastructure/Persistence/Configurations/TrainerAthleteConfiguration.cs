using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

internal sealed class TrainerAthleteConfiguration : IEntityTypeConfiguration<TrainerAthlete>
{
    public void Configure(EntityTypeBuilder<TrainerAthlete> builder)
    {
        builder.ToTable("TrainerAthletes", table =>
        {
            table.HasCheckConstraint(
                "CK_TrainerAthletes_Status",
                $"[Status] IN ({(int)TrainerAthleteAssignmentStatus.Active}, {(int)TrainerAthleteAssignmentStatus.Ended})");
        });

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(assignment => assignment.TenantId).IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(assignment => assignment.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.TrainerProfileId).IsRequired();
        builder.Property(assignment => assignment.AthleteProfileId).IsRequired();
        builder.Property(assignment => assignment.AssignedAt).IsRequired();
        builder.Property(assignment => assignment.Status).IsRequired();

        builder.HasOne<TrainerProfile>()
            .WithMany()
            .HasForeignKey(assignment => new { assignment.TenantId, assignment.TrainerProfileId })
            .HasPrincipalKey(profile => new { profile.TenantId, profile.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AthleteProfile>()
            .WithMany()
            .HasForeignKey(assignment => new { assignment.TenantId, assignment.AthleteProfileId })
            .HasPrincipalKey(profile => new { profile.TenantId, profile.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => new { assignment.TenantId, assignment.TrainerProfileId, assignment.AthleteProfileId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)TrainerAthleteAssignmentStatus.Active} AND [IsDeleted] = 0");
    }
}
