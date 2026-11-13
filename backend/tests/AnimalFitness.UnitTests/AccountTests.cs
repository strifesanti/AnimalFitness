using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;

namespace AnimalFitness.UnitTests;

public sealed class AccountTests
{
    [Fact]
    public void Create_normalizes_the_email_and_blocks_login_when_deactivated()
    {
        var user = User.Create("Ana@AnimalFitness.test", "Ana", "Pérez", TestClock.Now, phoneNumber: "+57 300 123 4567");

        Assert.Equal("ana@animalfitness.test", user.Email);
        Assert.Equal("Ana Pérez", user.FullName);
        Assert.True(user.IsActive);
        Assert.False(user.EmailConfirmed);

        user.Deactivate(TestClock.Now.AddMinutes(1), TestClock.Actor);
        Assert.Throws<DomainException>(() => user.RecordLogin(TestClock.Now.AddMinutes(2), user.Id));
        Assert.Null(user.LastLoginAt);
    }

    [Fact]
    public void Same_person_can_belong_to_two_organizations_with_different_roles()
    {
        var user = User.Create("ana@animalfitness.test", "Ana", "Pérez", TestClock.Now);
        var firstTenant = Guid.CreateVersion7();
        var secondTenant = Guid.CreateVersion7();

        var trainer = TenantMembership.Create(firstTenant, user.Id, MembershipRole.Trainer, TestClock.Now, TestClock.Actor);
        var athlete = TenantMembership.Create(secondTenant, user.Id, MembershipRole.Athlete, TestClock.Now, TestClock.Actor);

        Assert.Equal(user.Id, trainer.UserId);
        Assert.Equal(user.Id, athlete.UserId);
        Assert.NotEqual(trainer.TenantId, athlete.TenantId);
        Assert.DoesNotContain(Enum.GetNames<MembershipRole>(), name => name == PlatformRoles.SuperAdmin);
    }

    [Fact]
    public void Removed_membership_cannot_be_reactivated()
    {
        var membership = TenantMembership.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            MembershipRole.Athlete,
            TestClock.Now,
            TestClock.Actor);

        membership.Suspend(TestClock.Now.AddMinutes(1), TestClock.Actor);
        membership.Reactivate(TestClock.Now.AddMinutes(2), TestClock.Actor);
        membership.Remove(TestClock.Now.AddMinutes(3), TestClock.Actor);

        Assert.Throws<DomainException>(() => membership.Reactivate(TestClock.Now.AddMinutes(4), TestClock.Actor));
        Assert.Equal(MembershipStatus.Removed, membership.Status);
    }

    [Fact]
    public void Invalid_role_value_is_rejected()
    {
        Assert.Throws<DomainException>(() => TenantMembership.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            (MembershipRole)0,
            TestClock.Now,
            TestClock.Actor));
    }
}
