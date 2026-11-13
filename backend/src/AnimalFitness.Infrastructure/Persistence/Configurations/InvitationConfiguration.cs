using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="Invitation"/>. Solo se guarda el hash del token.
/// </summary>
internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations", table =>
        {
            table.HasCheckConstraint(
                "CK_Invitations_Role",
                $"[Role] IN ({(int)MembershipRole.Owner}, {(int)MembershipRole.Trainer}, {(int)MembershipRole.Athlete})");
            table.HasCheckConstraint(
                "CK_Invitations_Status",
                $"[Status] IN ({(int)InvitationStatus.Pending}, {(int)InvitationStatus.Accepted}, {(int)InvitationStatus.Expired}, {(int)InvitationStatus.Cancelled})");
        });

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(invitation => invitation.TenantId).IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(invitation => invitation.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(invitation => invitation.Email).HasMaxLength(FieldLengths.Email).IsRequired();
        builder.Property(invitation => invitation.Role).IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(FieldLengths.TokenHash).IsRequired();
        builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
        builder.Property(invitation => invitation.ExpiresAt).IsRequired();
        builder.Property(invitation => invitation.Status).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invitation => invitation.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(invitation => invitation.InvitedBy).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invitation => invitation.AcceptedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(invitation => invitation.AcceptedAt);

        builder.HasIndex(invitation => new { invitation.TenantId, invitation.Email, invitation.Status });
    }
}
