using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnimalFitness.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="User"/> (tabla UserProfiles). Comparte la clave con AspNetUsers, así que cada persona tiene una sola cuenta global.
/// </summary>
internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("UserProfiles");

        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<User>(profile => profile.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(profile => profile.FullName);
        builder.Property(profile => profile.Email).HasMaxLength(FieldLengths.Email).IsRequired();
        builder.HasIndex(profile => profile.Email).IsUnique();
        builder.Property(profile => profile.FirstName).HasMaxLength(FieldLengths.PersonName).IsRequired();
        builder.Property(profile => profile.LastName).HasMaxLength(FieldLengths.PersonName).IsRequired();
        builder.Property(profile => profile.PhoneNumber).HasMaxLength(FieldLengths.Phone);
        builder.Property(profile => profile.IsActive).IsRequired();
        builder.Property(profile => profile.EmailConfirmed).IsRequired();
        builder.Property(profile => profile.LastLoginAt);
    }
}
