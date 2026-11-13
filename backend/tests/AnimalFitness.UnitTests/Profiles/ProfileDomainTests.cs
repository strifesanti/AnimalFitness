using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Profiles;

namespace AnimalFitness.UnitTests.Profiles;

public sealed class ProfileDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void TrainerAthlete_assignment_can_end_once()
    {
        var trainer = TrainerProfile.Create(TenantId, UserId, Now, null);
        var athlete = AthleteProfile.Create(TenantId, Guid.CreateVersion7(), Now, null);
        var assignment = TrainerAthlete.Create(TenantId, trainer.Id, athlete.Id, Now, null);

        assignment.End(Now.AddMinutes(1), UserId);

        Assert.Equal(TrainerAthleteAssignmentStatus.Ended, assignment.Status);
        Assert.Throws<DomainException>(() => assignment.End(Now.AddMinutes(2), UserId));
    }

    [Fact]
    public void AthleteGoal_completes_and_rejects_second_complete()
    {
        var athlete = AthleteProfile.Create(TenantId, UserId, Now, null);
        var goal = AthleteGoal.Create(TenantId, athlete.Id, "Correr 5K", Now, null);

        goal.Complete(Now.AddDays(1), UserId);

        Assert.Equal(AthleteGoalStatus.Completed, goal.Status);
        Assert.Throws<DomainException>(() => goal.Complete(Now.AddDays(2), UserId));
    }
}
