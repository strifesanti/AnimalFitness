using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Tracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

internal sealed class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.ToTable("BodyMeasurements");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.WeightKg).HasPrecision(6, 2);
        builder.Property(e => e.HeightCm).HasPrecision(6, 2);
        builder.Property(e => e.BodyFatPercent).HasPrecision(5, 2);
        builder.Property(e => e.MuscleMassKg).HasPrecision(6, 2);
        builder.Property(e => e.WaistCm).HasPrecision(6, 2);
        builder.Property(e => e.ChestCm).HasPrecision(6, 2);
        builder.Property(e => e.ArmCm).HasPrecision(6, 2);
        builder.Property(e => e.LegCm).HasPrecision(6, 2);
        builder.Property(e => e.HipCm).HasPrecision(6, 2);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.ProgressNotes);
        builder.Property(e => e.RecordedByUserId).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.MeasuredAt });
    }
}

internal sealed class ProgressPhotoConfiguration : IEntityTypeConfiguration<ProgressPhoto>
{
    public void Configure(EntityTypeBuilder<ProgressPhoto> builder)
    {
        builder.ToTable("ProgressPhotos", table =>
        {
            table.HasCheckConstraint(
                "CK_ProgressPhotos_Pose",
                $"[Pose] IN ({(int)PhotoPose.Front},{(int)PhotoPose.Side},{(int)PhotoPose.Back},{(int)PhotoPose.Other})");
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
        builder.Property(e => e.Url).HasMaxLength(FieldLengths.MediaUrl).IsRequired();
        builder.Property(e => e.Caption).HasMaxLength(FieldLengths.ProgressNotes);
        builder.Property(e => e.Notes).HasMaxLength(FieldLengths.ProgressNotes);
        builder.Property(e => e.RecordedByUserId).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.TakenAt });
    }
}

internal sealed class FitnessAssessmentConfiguration : IEntityTypeConfiguration<FitnessAssessment>
{
    public void Configure(EntityTypeBuilder<FitnessAssessment> builder)
    {
        builder.ToTable("FitnessAssessments");
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AthleteProfile>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.AthleteProfileId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Title).HasMaxLength(FieldLengths.AssessmentTitle).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(FieldLengths.ProgressNotes).IsRequired();
        builder.Property(e => e.AssessedByUserId).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.AthleteProfileId, e.AssessedAt });
    }
}
