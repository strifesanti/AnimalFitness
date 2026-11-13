using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Training;

namespace AnimalFitness.UnitTests.Training;

public sealed class TrainingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();

    [Fact]
    public void Published_plan_cannot_be_edited_without_new_version()
    {
        var plan = WorkoutPlan.Create(TenantId, "Fuerza", Now, null);
        plan.Publish(Now.AddMinutes(1), null);

        Assert.Throws<DomainException>(() =>
            plan.UpdateDraft("Otro", null, null, Now.AddMinutes(2), null));

        var next = plan.CreateNextVersion(Now.AddMinutes(3), null);
        Assert.Equal(2, next.Version);
        Assert.Equal(WorkoutPlanStatus.Draft, next.Status);
        Assert.Equal(plan.Id, next.ParentPlanId);
    }

    [Fact]
    public void Session_completes_once_and_keeps_plan_reference()
    {
        var athleteId = Guid.CreateVersion7();
        var planId = Guid.CreateVersion7();
        var session = WorkoutSession.Start(TenantId, athleteId, Now, null, planId);

        session.Complete(Now.AddHours(1), null, "Buena sesión");

        Assert.Equal(WorkoutSessionStatus.Completed, session.Status);
        Assert.Equal(planId, session.PlanId);
        Assert.Throws<DomainException>(() => session.Complete(Now.AddHours(2), null));
    }

    [Fact]
    public void Exercise_rejects_self_variation()
    {
        var exercise = Exercise.Create(
            TenantId,
            "Press banca",
            "Pecho",
            ExerciseType.Gym,
            ExerciseDifficulty.Intermediate,
            Now,
            null);

        Assert.Throws<DomainException>(() =>
            exercise.Update(
                "Press banca",
                "Pecho",
                ExerciseType.Gym,
                ExerciseDifficulty.Intermediate,
                null,
                null,
                null,
                null,
                null,
                exercise.Id,
                Now.AddMinutes(1),
                null));
    }
}
