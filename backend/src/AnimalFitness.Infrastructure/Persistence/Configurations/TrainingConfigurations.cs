using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

internal sealed class ExerciseCategoryConfiguration : IEntityTypeConfiguration<ExerciseCategory>
{
    public void Configure(EntityTypeBuilder<ExerciseCategory> builder)
    {
        builder.ToTable("ExerciseCategories");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Name).HasMaxLength(FieldLengths.ExerciseName).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(FieldLengths.ExerciseDescription);
        builder.HasIndex(e => new { e.TenantId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises", table =>
        {
            table.HasCheckConstraint("CK_Exercises_Type", $"[Type] IN ({(int)ExerciseType.Gym},{(int)ExerciseType.Calisthenics},{(int)ExerciseType.Cardio},{(int)ExerciseType.Other})");
            table.HasCheckConstraint("CK_Exercises_Difficulty", $"[Difficulty] IN ({(int)ExerciseDifficulty.Beginner},{(int)ExerciseDifficulty.Intermediate},{(int)ExerciseDifficulty.Advanced})");
        });
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseCategory>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.CategoryId })
            .HasPrincipalKey(c => new { c.TenantId, c.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<Exercise>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.VariationOfExerciseId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.Property(e => e.Name).HasMaxLength(FieldLengths.ExerciseName).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(FieldLengths.ExerciseDescription);
        builder.Property(e => e.PrimaryMuscle).HasMaxLength(FieldLengths.MuscleOrEquipment).IsRequired();
        builder.Property(e => e.SecondaryMuscles).HasMaxLength(FieldLengths.MuscleOrEquipment);
        builder.Property(e => e.Equipment).HasMaxLength(FieldLengths.MuscleOrEquipment);
        builder.Property(e => e.Instructions).HasMaxLength(FieldLengths.Instructions);
        builder.HasIndex(e => new { e.TenantId, e.Name });
        builder.HasIndex(e => new { e.TenantId, e.PrimaryMuscle, e.Type });
    }
}

internal sealed class ExerciseMediaConfiguration : IEntityTypeConfiguration<ExerciseMedia>
{
    public void Configure(EntityTypeBuilder<ExerciseMedia> builder)
    {
        builder.ToTable("ExerciseMedia", table =>
        {
            table.HasCheckConstraint("CK_ExerciseMedia_Type", $"[MediaType] IN ({(int)ExerciseMediaType.Image},{(int)ExerciseMediaType.Video})");
        });
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Exercise>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ExerciseId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Url).HasMaxLength(FieldLengths.MediaUrl).IsRequired();
        builder.Property(e => e.Caption).HasMaxLength(FieldLengths.WorkoutNotes);
    }
}

internal sealed class WorkoutPlanConfiguration : IEntityTypeConfiguration<WorkoutPlan>
{
    public void Configure(EntityTypeBuilder<WorkoutPlan> builder)
    {
        builder.ToTable("WorkoutPlans", table =>
        {
            table.HasCheckConstraint("CK_WorkoutPlans_Status", $"[Status] IN ({(int)WorkoutPlanStatus.Draft},{(int)WorkoutPlanStatus.Published},{(int)WorkoutPlanStatus.Archived})");
        });
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutPlan>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ParentPlanId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<TrainerProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AuthorTrainerProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.Property(e => e.Name).HasMaxLength(FieldLengths.WorkoutName).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(FieldLengths.ExerciseDescription);
        builder.Property(e => e.GoalLabel).HasMaxLength(FieldLengths.MuscleOrEquipment);
        builder.HasIndex(e => new { e.TenantId, e.Status, e.Name });
    }
}

internal sealed class WorkoutDayConfiguration : IEntityTypeConfiguration<WorkoutDay>
{
    public void Configure(EntityTypeBuilder<WorkoutDay> builder)
    {
        builder.ToTable("WorkoutDays");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutPlan>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.PlanId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Name).HasMaxLength(FieldLengths.WorkoutName).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.WorkoutNotes);
        builder.HasIndex(e => new { e.TenantId, e.PlanId, e.DayIndex }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.ToTable("WorkoutExercises");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutDay>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.DayId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Exercise>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ExerciseId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.TargetReps).HasMaxLength(40);
        builder.Property(e => e.TargetWeightKg).HasPrecision(8, 2);
        builder.Property(e => e.TargetDistanceMeters).HasPrecision(10, 2);
        builder.Property(e => e.Tempo).HasMaxLength(FieldLengths.Tempo);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.WorkoutNotes);
    }
}

internal sealed class WorkoutPlanAssignmentConfiguration : IEntityTypeConfiguration<WorkoutPlanAssignment>
{
    public void Configure(EntityTypeBuilder<WorkoutPlanAssignment> builder)
    {
        builder.ToTable("WorkoutPlanAssignments", table =>
        {
            table.HasCheckConstraint("CK_WorkoutPlanAssignments_Status", $"[Status] IN ({(int)PlanAssignmentStatus.Active},{(int)PlanAssignmentStatus.Ended})");
        });
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutPlan>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.PlanId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.PlanId, e.AthleteProfileId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)PlanAssignmentStatus.Active} AND [IsDeleted] = 0");
    }
}

internal sealed class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("WorkoutSessions", table =>
        {
            table.HasCheckConstraint("CK_WorkoutSessions_Status", $"[Status] IN ({(int)WorkoutSessionStatus.InProgress},{(int)WorkoutSessionStatus.Completed},{(int)WorkoutSessionStatus.Cancelled})");
        });
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutPlan>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.PlanId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<WorkoutDay>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.DayId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.WorkoutNotes);
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.Status });
    }
}

internal sealed class ExerciseLogConfiguration : IEntityTypeConfiguration<ExerciseLog>
{
    public void Configure(EntityTypeBuilder<ExerciseLog> builder)
    {
        builder.ToTable("ExerciseLogs");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutSession>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.SessionId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkoutExercise>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.WorkoutExerciseId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<Exercise>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ExerciseId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.WeightKg).HasPrecision(8, 2);
        builder.Property(e => e.DistanceMeters).HasPrecision(10, 2);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.WorkoutNotes);
        builder.HasIndex(e => new { e.TenantId, e.SessionId, e.ExerciseId, e.SetNumber });
    }
}
