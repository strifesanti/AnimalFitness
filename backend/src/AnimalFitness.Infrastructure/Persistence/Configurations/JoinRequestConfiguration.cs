using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="JoinRequest"/>.
/// </summary>
internal sealed class JoinRequestConfiguration : IEntityTypeConfiguration<JoinRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JoinRequest> builder)
    {
        builder.ToTable("JoinRequests", table => table.HasCheckConstraint(
            "CK_JoinRequests_Status",
            $"[Status] IN ({(int)JoinRequestStatus.Pending}, {(int)JoinRequestStatus.Approved}, {(int)JoinRequestStatus.Rejected}, {(int)JoinRequestStatus.Cancelled})"));

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(request => request.TenantId).IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(request => request.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(request => request.UserId).IsRequired();

        builder.Property(request => request.Status).IsRequired();
        builder.Property(request => request.ReviewedAt);
        builder.Property(request => request.RejectionReason).HasMaxLength(FieldLengths.RejectionReason);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Solo puede haber una solicitud pendiente por persona y organización.
        builder.HasIndex(request => new { request.TenantId, request.UserId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)JoinRequestStatus.Pending} AND [IsDeleted] = 0");

        builder.HasIndex(request => new { request.TenantId, request.Status });
    }
}
