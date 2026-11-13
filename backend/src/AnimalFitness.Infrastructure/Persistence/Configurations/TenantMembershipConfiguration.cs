using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="TenantMembership"/>. Pertenece a una organización y referencia a una persona global.
/// </summary>
internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships", table =>
        {
            table.HasCheckConstraint(
                "CK_TenantMemberships_Role",
                $"[Role] IN ({(int)MembershipRole.Owner}, {(int)MembershipRole.Trainer}, {(int)MembershipRole.Athlete})");
            table.HasCheckConstraint(
                "CK_TenantMemberships_Status",
                $"[Status] IN ({(int)MembershipStatus.Active}, {(int)MembershipStatus.Suspended}, {(int)MembershipStatus.Removed})");
        });

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(membership => membership.TenantId).IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(membership => membership.UserId).IsRequired();
        builder.Property(membership => membership.Role).IsRequired();
        builder.Property(membership => membership.Status).IsRequired();
        builder.Property(membership => membership.JoinedAt).IsRequired();

        // Una persona solo puede tener una membresía vigente por organización.
        // Las membresías retiradas o borradas no bloquean una nueva vinculación.
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId })
            .IsUnique()
            .HasFilter($"[Status] <> {(int)MembershipStatus.Removed} AND [IsDeleted] = 0");

        builder.HasIndex(membership => new { membership.UserId, membership.Status });
    }
}
