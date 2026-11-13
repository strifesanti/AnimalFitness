using AnimalFitness.Application.Features.Nutrition;
using AnimalFitness.Domain.Attendance;
using AnimalFitness.Domain.Challenges;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Communication;
using AnimalFitness.Domain.Nutrition;
using AnimalFitness.Infrastructure.Advanced;

namespace AnimalFitness.UnitTests.Advanced;

public sealed class ChallengeDomainTests
{
    private static readonly DateTimeOffset Now = TestClock.Now;
    private static readonly Guid TenantId = Guid.CreateVersion7();

    private static Challenge NewChallenge(DateTimeOffset? endsAt = null)
        => Challenge.Create(
            TenantId,
            "Reto de noviembre",
            ChallengeKind.Individual,
            ChallengeMetric.ManualPoints,
            Now,
            endsAt ?? Now.AddDays(30),
            Now,
            TestClock.Actor);

    [Fact]
    public void New_challenge_is_a_draft_and_activation_makes_it_active()
    {
        var challenge = NewChallenge();
        Assert.Equal(ChallengeStatus.Draft, challenge.Status);

        challenge.Activate(Now, TestClock.Actor);

        Assert.Equal(ChallengeStatus.Active, challenge.Status);
    }

    [Fact]
    public void Challenge_requires_end_after_start()
    {
        Assert.Throws<DomainException>(() => NewChallenge(Now));
        Assert.Throws<DomainException>(() => NewChallenge(Now.AddDays(-1)));
    }

    [Fact]
    public void Challenge_lifecycle_rejects_invalid_transitions()
    {
        var challenge = NewChallenge();
        Assert.Throws<DomainException>(() => challenge.Complete(Now, TestClock.Actor));

        challenge.Activate(Now, TestClock.Actor);
        Assert.Throws<DomainException>(() => challenge.Activate(Now, TestClock.Actor));

        challenge.Complete(Now, TestClock.Actor);
        Assert.Equal(ChallengeStatus.Completed, challenge.Status);
        Assert.Throws<DomainException>(() => challenge.Cancel(Now, TestClock.Actor));
    }

    [Fact]
    public void Draft_or_active_challenge_can_be_cancelled_once()
    {
        var challenge = NewChallenge();
        challenge.Cancel(Now, TestClock.Actor);

        Assert.Equal(ChallengeStatus.Cancelled, challenge.Status);
        Assert.Throws<DomainException>(() => challenge.Cancel(Now, TestClock.Actor));
    }

    [Fact]
    public void Joining_starts_at_zero_points_and_activity_points_accumulate()
    {
        var challenge = NewChallenge();
        challenge.Activate(Now, TestClock.Actor);
        var participant = ChallengeParticipant.Create(TenantId, challenge.Id, Guid.CreateVersion7(), Now, TestClock.Actor);
        Assert.Equal(0, participant.Points);

        participant.AddPoints(10, Now, TestClock.Actor);
        participant.AddPoints(5, Now, TestClock.Actor);

        Assert.Equal(15, participant.Points);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Points_must_be_positive(int points)
    {
        var participant = ChallengeParticipant.Create(TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), Now, TestClock.Actor);

        Assert.Throws<DomainException>(() => participant.AddPoints(points, Now, TestClock.Actor));
        Assert.Throws<DomainException>(() => ChallengeActivity.Create(
            TenantId,
            Guid.CreateVersion7(),
            participant.Id,
            points,
            Now,
            Now,
            TestClock.Actor));
        Assert.Equal(0, participant.Points);
    }

    [Fact]
    public void Leaderboard_orders_by_points_descending_and_shares_rank_on_ties()
    {
        var challengeId = Guid.CreateVersion7();
        ChallengeParticipant Join(int points, int minute)
        {
            var participant = ChallengeParticipant.Create(
                TenantId,
                challengeId,
                Guid.CreateVersion7(),
                Now.AddMinutes(minute),
                TestClock.Actor);
            if (points > 0)
            {
                participant.AddPoints(points, Now.AddMinutes(minute), TestClock.Actor);
            }

            return participant;
        }

        var low = Join(5, 0);
        var firstTop = Join(20, 2);
        var secondTop = Join(20, 3);
        var none = Join(0, 4);

        var board = ChallengeService.BuildLeaderboard(
            [low, none, secondTop, firstTop],
            new Dictionary<Guid, string> { [firstTop.AthleteProfileId] = "Ana" });

        Assert.Equal(
            [firstTop.Id, secondTop.Id, low.Id, none.Id],
            board.Select(entry => entry.ParticipantId).ToArray());
        Assert.Equal([1, 1, 3, 4], board.Select(entry => entry.Rank).ToArray());
        Assert.Equal("Ana", board[0].AthleteName);
        Assert.Equal(string.Empty, board[1].AthleteName);
    }
}

public sealed class NutritionDomainTests
{
    private static readonly DateTimeOffset Now = TestClock.Now;
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid AthleteId = Guid.CreateVersion7();
    private static readonly DateOnly Today = new(2026, 11, 13);

    private static NutritionTarget Target(
        int calories = 2200,
        decimal protein = 150,
        decimal carbs = 250,
        decimal fat = 70,
        int water = 2500)
        => NutritionTarget.Create(TenantId, AthleteId, calories, protein, carbs, fat, water, Today, Now, TestClock.Actor);

    [Fact]
    public void Valid_target_is_created()
    {
        var target = Target();

        Assert.Equal(2200, target.Calories);
        Assert.Equal(2500, target.WaterMl);
        Assert.Equal(Today, target.EffectiveFrom);
    }

    [Theory]
    [InlineData(499, 150, 250, 70, 2500)]
    [InlineData(10001, 150, 250, 70, 2500)]
    [InlineData(2200, -1, 250, 70, 2500)]
    [InlineData(2200, 150, 1001, 70, 2500)]
    [InlineData(2200, 150, 250, 1001, 2500)]
    [InlineData(2200, 150, 250, 70, -1)]
    [InlineData(2200, 150, 250, 70, 20001)]
    public void Invalid_target_values_are_rejected(int calories, decimal protein, decimal carbs, decimal fat, int water)
    {
        Assert.Throws<DomainException>(() => Target(calories, protein, carbs, fat, water));
    }

    [Fact]
    public void Target_boundaries_are_accepted()
    {
        Assert.Equal(500, Target(calories: 500, protein: 0, carbs: 0, fat: 0, water: 0).Calories);
        Assert.Equal(10000, Target(calories: 10000, protein: 1000, carbs: 1000, fat: 1000, water: 20000).Calories);
    }

    [Fact]
    public void Updating_a_target_revalidates_values()
    {
        var target = Target();

        target.Update(1800, 120, 200, 60, 2000, Today.AddDays(1), Now, TestClock.Actor);
        Assert.Equal(1800, target.Calories);
        Assert.Equal(Today.AddDays(1), target.EffectiveFrom);

        Assert.Throws<DomainException>(() => target.Update(100, 120, 200, 60, 2000, Today, Now, TestClock.Actor));
        Assert.Equal(1800, target.Calories);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public void Water_logs_validate_amount(int amount)
    {
        Assert.Throws<DomainException>(() => WaterLog.Create(TenantId, AthleteId, Today, amount, Now, TestClock.Actor));
    }

    [Fact]
    public void Food_entry_rejects_invalid_calories()
    {
        Assert.Throws<DomainException>(() => FoodEntry.Create(
            TenantId, AthleteId, Today, MealType.Lunch, "Arroz", -1, 1, 1, 1, Now, TestClock.Actor));
    }

    [Fact]
    public void Summary_totals_consumption_and_compares_with_target()
    {
        var target = Target(calories: 2000, protein: 100, carbs: 200, fat: 50, water: 2000);
        var targetDto = new NutritionTargetDto(
            target.Id, AthleteId, target.Calories, target.ProteinGrams, target.CarbsGrams, target.FatGrams, target.WaterMl, Today);
        var foods = new[]
        {
            new FoodEntryDto(Guid.NewGuid(), AthleteId, Today, MealType.Breakfast, "Avena", 400, 20, 60, 8, null),
            new FoodEntryDto(Guid.NewGuid(), AthleteId, Today, MealType.Lunch, "Pollo y arroz", 700, 50, 80, 15, null),
        };
        var water = new[]
        {
            new WaterLogDto(Guid.NewGuid(), AthleteId, Today, 500, Now),
            new WaterLogDto(Guid.NewGuid(), AthleteId, Today, 1000, Now),
        };

        var summary = NutritionService.BuildSummary(AthleteId, Today, targetDto, foods, water);

        Assert.Equal(1100, summary.Consumed.Calories);
        Assert.Equal(70, summary.Consumed.ProteinGrams);
        Assert.Equal(1500, summary.Consumed.WaterMl);
        Assert.Equal(900, summary.Remaining!.Calories);
        Assert.Equal(500, summary.Remaining.WaterMl);
        Assert.Equal(55m, summary.Percentages!.Calories);
        Assert.Equal(75m, summary.Percentages.Water);
    }

    [Fact]
    public void Summary_without_target_has_no_remaining_or_percentages_and_never_goes_negative()
    {
        var summary = NutritionService.BuildSummary(AthleteId, Today, null, [], []);
        Assert.Null(summary.Remaining);
        Assert.Null(summary.Percentages);
        Assert.Equal(0, summary.Consumed.Calories);

        var small = new NutritionTargetDto(Guid.NewGuid(), AthleteId, 500, 0, 0, 0, 0, Today);
        var over = NutritionService.BuildSummary(
            AthleteId,
            Today,
            small,
            [new FoodEntryDto(Guid.NewGuid(), AthleteId, Today, MealType.Snack, "Pizza", 900, 0, 0, 0, null)],
            []);
        Assert.Equal(0, over.Remaining!.Calories);
        Assert.Equal(180m, over.Percentages!.Calories);
        Assert.Equal(0m, over.Percentages.Water);
    }
}

public sealed class AttendanceAndCommunicationDomainTests
{
    private static readonly DateTimeOffset Now = TestClock.Now;
    private static readonly Guid TenantId = Guid.CreateVersion7();

    [Fact]
    public void Attendance_stats_compute_present_percentages()
    {
        var stats = AttendanceService.ComputeStats(
            null,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            [
                AttendanceStatus.Present,
                AttendanceStatus.Present,
                AttendanceStatus.Late,
                AttendanceStatus.Absent,
            ]);

        Assert.Equal(4, stats.Total);
        Assert.Equal(2, stats.Present);
        Assert.Equal(50m, stats.PresentPercent);
        Assert.Equal(75m, stats.AttendedPercent);
        Assert.Equal(0, AttendanceService.ComputeStats(null, default, default, []).Total);
        Assert.Equal(0m, AttendanceService.ComputeStats(null, default, default, []).PresentPercent);
    }

    [Fact]
    public void Attendance_update_changes_status_and_notes()
    {
        var record = AttendanceRecord.Create(
            TenantId, Guid.CreateVersion7(), new DateOnly(2026, 11, 13), AttendanceStatus.Absent, TestClock.Actor, Now);

        record.Update(AttendanceStatus.Excused, "Certificado médico", Now, TestClock.Actor);

        Assert.Equal(AttendanceStatus.Excused, record.Status);
        Assert.Equal("Certificado médico", record.Notes);
    }

    [Fact]
    public void Chat_message_needs_text_or_attachment()
    {
        var conversation = Guid.CreateVersion7();
        Assert.Throws<DomainException>(() => ChatMessage.Create(TenantId, conversation, TestClock.Actor, "  ", Now));

        Assert.Equal("Hola", ChatMessage.Create(TenantId, conversation, TestClock.Actor, " Hola ", Now).Body);
        Assert.Equal(
            "https://example.test/a.png",
            ChatMessage.Create(TenantId, conversation, TestClock.Actor, string.Empty, Now, "https://example.test/a.png").AttachmentUrl);
    }

    [Fact]
    public void Notification_is_marked_read_once()
    {
        var notification = AppNotification.Create(
            TenantId, Guid.CreateVersion7(), NotificationKind.General, "Hola", "Bienvenido", Now);
        Assert.False(notification.IsRead);

        notification.MarkRead(Now, TestClock.Actor);
        notification.MarkRead(Now.AddMinutes(1), TestClock.Actor);

        Assert.True(notification.IsRead);
    }
}
