using AnimalFitness.Domain.Attendance;
using AnimalFitness.Domain.Challenges;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Communication;
using AnimalFitness.Domain.Nutrition;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

internal sealed class NutritionTargetConfiguration : IEntityTypeConfiguration<NutritionTarget>
{
    public void Configure(EntityTypeBuilder<NutritionTarget> builder)
    {
        builder.ToTable("NutritionTargets");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.ProteinGrams).HasPrecision(8, 2);
        builder.Property(e => e.CarbsGrams).HasPrecision(8, 2);
        builder.Property(e => e.FatGrams).HasPrecision(8, 2);
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.EffectiveFrom });
    }
}

internal sealed class FoodEntryConfiguration : IEntityTypeConfiguration<FoodEntry>
{
    public void Configure(EntityTypeBuilder<FoodEntry> builder)
    {
        builder.ToTable("FoodEntries", t =>
            t.HasCheckConstraint("CK_FoodEntries_MealType",
                $"[MealType] IN ({(int)MealType.Breakfast},{(int)MealType.Lunch},{(int)MealType.Dinner},{(int)MealType.Snack},{(int)MealType.Other})"));
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Name).HasMaxLength(FieldLengths.NutritionOrChallengeName).IsRequired();
        builder.Property(e => e.ProteinGrams).HasPrecision(8, 2);
        builder.Property(e => e.CarbsGrams).HasPrecision(8, 2);
        builder.Property(e => e.FatGrams).HasPrecision(8, 2);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.ProgressNotes);
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.EntryDate });
    }
}

internal sealed class WaterLogConfiguration : IEntityTypeConfiguration<WaterLog>
{
    public void Configure(EntityTypeBuilder<WaterLog> builder)
    {
        builder.ToTable("WaterLogs");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.EntryDate });
    }
}

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords", t =>
            t.HasCheckConstraint("CK_AttendanceRecords_Status",
                $"[Status] IN ({(int)AttendanceStatus.Present},{(int)AttendanceStatus.Absent},{(int)AttendanceStatus.Excused},{(int)AttendanceStatus.Late})"));
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.ProgressNotes);
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.AttendanceDate })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.ToTable("Challenges", t =>
        {
            t.HasCheckConstraint("CK_Challenges_Kind", $"[Kind] IN ({(int)ChallengeKind.Individual},{(int)ChallengeKind.Group})");
            t.HasCheckConstraint("CK_Challenges_Metric", $"[Metric] IN ({(int)ChallengeMetric.WorkoutsCompleted},{(int)ChallengeMetric.AttendanceDays},{(int)ChallengeMetric.ManualPoints})");
            t.HasCheckConstraint("CK_Challenges_Status", $"[Status] IN ({(int)ChallengeStatus.Draft},{(int)ChallengeStatus.Active},{(int)ChallengeStatus.Completed},{(int)ChallengeStatus.Cancelled})");
        });
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Name).HasMaxLength(FieldLengths.NutritionOrChallengeName).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(FieldLengths.ChallengeDescription);
        builder.HasIndex(e => new { e.TenantId, e.Status, e.StartsAt });
    }
}

internal sealed class ChallengeParticipantConfiguration : IEntityTypeConfiguration<ChallengeParticipant>
{
    public void Configure(EntityTypeBuilder<ChallengeParticipant> builder)
    {
        builder.ToTable("ChallengeParticipants");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Challenge>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ChallengeId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.ChallengeId, e.AthleteProfileId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class ChallengeActivityConfiguration : IEntityTypeConfiguration<ChallengeActivity>
{
    public void Configure(EntityTypeBuilder<ChallengeActivity> builder)
    {
        builder.ToTable("ChallengeActivities");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Challenge>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ChallengeId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ChallengeParticipant>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ParticipantId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Note).HasMaxLength(FieldLengths.ProgressNotes);
    }
}

internal sealed class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Challenge>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ChallengeId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.Property(e => e.Title).HasMaxLength(FieldLengths.NutritionOrChallengeName).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(FieldLengths.ChallengeDescription);
    }
}

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations", t =>
            t.HasCheckConstraint("CK_Conversations_Kind", $"[Kind] IN ({(int)ConversationKind.Direct},{(int)ConversationKind.Group})"));
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Title).HasMaxLength(FieldLengths.NutritionOrChallengeName);
    }
}

internal sealed class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Conversation>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ConversationId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.ConversationId, e.UserId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Conversation>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ConversationId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Body).HasMaxLength(FieldLengths.MessageBody).IsRequired();
        builder.Property(e => e.AttachmentUrl).HasMaxLength(FieldLengths.MediaUrl);
        builder.HasIndex(e => new { e.TenantId, e.ConversationId, e.SentAt });
    }
}

internal sealed class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        builder.ToTable("AppNotifications", t =>
            t.HasCheckConstraint("CK_AppNotifications_Kind",
                $"[Kind] IN ({(int)NotificationKind.General},{(int)NotificationKind.ChatMessage},{(int)NotificationKind.Challenge},{(int)NotificationKind.Membership},{(int)NotificationKind.Workout},{(int)NotificationKind.Reminder})"));
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Title).HasMaxLength(FieldLengths.NotificationTitle).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(FieldLengths.MessageBody).IsRequired();
        builder.Property(e => e.LinkPath).HasMaxLength(FieldLengths.MediaUrl);
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.IsRead, e.RaisedAt });
    }
}
