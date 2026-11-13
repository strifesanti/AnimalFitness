using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;

namespace AnimalFitness.UnitTests;

public sealed class AccessTests
{
    private const string TokenHash = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void Accept_rejects_an_expired_invitation_without_changing_it()
    {
        var invitation = Invitation.Create(
            Guid.CreateVersion7(),
            "alumno@animalfitness.test",
            MembershipRole.Athlete,
            TokenHash,
            TestClock.Now.AddDays(2),
            TestClock.Now,
            TestClock.Actor);

        var act = () => invitation.Accept(Guid.CreateVersion7(), invitation.ExpiresAt, TestClock.Actor);

        Assert.Throws<DomainException>(act);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.AcceptedByUserId);
    }

    [Fact]
    public void Expire_then_accept_is_not_allowed()
    {
        var invitation = Invitation.Create(
            Guid.CreateVersion7(),
            "alumno@animalfitness.test",
            MembershipRole.Athlete,
            TokenHash,
            TestClock.Now.AddHours(1),
            TestClock.Now,
            TestClock.Actor);

        invitation.Expire(invitation.ExpiresAt, TestClock.Actor);

        Assert.Equal(InvitationStatus.Expired, invitation.Status);
        Assert.Throws<DomainException>(() => invitation.Accept(Guid.CreateVersion7(), invitation.ExpiresAt.AddMinutes(1), TestClock.Actor));
    }

    [Fact]
    public void Short_token_hash_is_rejected()
    {
        Assert.Throws<DomainException>(() => Invitation.Create(
            Guid.CreateVersion7(),
            "alumno@animalfitness.test",
            MembershipRole.Athlete,
            "token-en-claro",
            TestClock.Now.AddDays(1),
            TestClock.Now,
            TestClock.Actor));
    }

    [Fact]
    public void Resolved_join_request_cannot_be_approved_again()
    {
        var request = JoinRequest.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), TestClock.Now);
        request.Reject(TestClock.Actor, "No hay cupo en este periodo.", TestClock.Now.AddMinutes(10));

        Assert.Equal(JoinRequestStatus.Rejected, request.Status);
        Assert.Throws<DomainException>(() => request.Approve(TestClock.Actor, TestClock.Now.AddMinutes(11)));
        Assert.Equal("No hay cupo en este periodo.", request.RejectionReason);
    }

    [Fact]
    public void Reject_without_a_reason_leaves_the_request_pending()
    {
        var request = JoinRequest.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), TestClock.Now);

        Assert.Throws<DomainException>(() => request.Reject(TestClock.Actor, "   ", TestClock.Now.AddMinutes(1)));
        Assert.Equal(JoinRequestStatus.Pending, request.Status);
        Assert.Null(request.ReviewedBy);
    }
}
