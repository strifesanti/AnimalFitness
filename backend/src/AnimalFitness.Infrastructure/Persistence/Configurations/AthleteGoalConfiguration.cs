using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

internal sealed class AthleteGoalConfiguration : IEntityTypeConfiguration<AthleteGoal>
{
    public void Configure(EntityTypeBuilder<AthleteGoal> builder)
    {
        builder.ToTable("AthleteGoals", table =>
        {
            table.HasCheckConstraint(
                "CK_AthleteGoals_Status",
                $"[Status] IN ({(int)AthleteGoalStatus.Active}, {(int)AthleteGoalStatus.Completed}, {(int)AthleteGoalStatus.Cancelled})");
        });

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(goal => goal.TenantId).IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(goal => goal.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(goal => goal.AthleteProfileId).IsRequired();
        builder.HasOne<AthleteProfile>()
            .WithMany()
            .HasForeignKey(goal => new { goal.TenantId, goal.AthleteProfileId })
            .HasPrincipalKey(profile => new { profile.TenantId, profile.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(goal => goal.Title).HasMaxLength(FieldLengths.GoalTitle).IsRequired();
        builder.Property(goal => goal.Description).HasMaxLength(FieldLengths.GoalDescription);
        builder.Property(goal => goal.TargetValue).HasPrecision(12, 2);
        builder.Property(goal => goal.Unit).HasMaxLength(FieldLengths.GoalUnit);
        builder.Property(goal => goal.Status).IsRequired();

        builder.HasIndex(goal => new { goal.TenantId, goal.AthleteProfileId, goal.Status });
    }
}
