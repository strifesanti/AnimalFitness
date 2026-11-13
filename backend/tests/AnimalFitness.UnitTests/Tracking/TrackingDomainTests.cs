using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Tracking;

namespace AnimalFitness.UnitTests.Tracking;

public sealed class TrackingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid AthleteId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static BodyMeasurement Measure(
        decimal? weightKg = null,
        decimal? heightCm = null,
        decimal? bodyFatPercent = null,
        decimal? waistCm = null)
        => BodyMeasurement.Create(
            TenantId,
            AthleteId,
            Now,
            UserId,
            Now,
            weightKg: weightKg,
            heightCm: heightCm,
            bodyFatPercent: bodyFatPercent,
            waistCm: waistCm);

    [Fact]
    public void ComputeBmi_uses_weight_and_height()
    {
        var measurement = Measure(weightKg: 70, heightCm: 175);

        Assert.Equal(22.86m, measurement.ComputeBmi());
    }

    [Fact]
    public void ComputeBmi_is_null_without_weight_or_height()
    {
        Assert.Null(Measure(weightKg: 70).ComputeBmi());
        Assert.Null(Measure(heightCm: 175).ComputeBmi());
        Assert.Null(Measure(waistCm: 80).ComputeBmi());
    }

    [Fact]
    public void Create_requires_at_least_one_value()
    {
        Assert.Throws<DomainException>(() => Measure());
    }

    [Fact]
    public void Create_rejects_out_of_range_values()
    {
        Assert.Throws<DomainException>(() => Measure(weightKg: 10));
        Assert.Throws<DomainException>(() => Measure(weightKg: 500));
        Assert.Throws<DomainException>(() => Measure(heightCm: 10));
        Assert.Throws<DomainException>(() => Measure(heightCm: 400));
        Assert.Throws<DomainException>(() => Measure(bodyFatPercent: 0));
        Assert.Throws<DomainException>(() => Measure(bodyFatPercent: 80));
    }

    [Fact]
    public void Create_rejects_empty_ids()
    {
        Assert.Throws<DomainException>(() =>
            BodyMeasurement.Create(TenantId, Guid.Empty, Now, UserId, Now, weightKg: 70));
        Assert.Throws<DomainException>(() =>
            BodyMeasurement.Create(Guid.Empty, AthleteId, Now, UserId, Now, weightKg: 70));
        Assert.Throws<DomainException>(() =>
            BodyMeasurement.Create(TenantId, AthleteId, Now, Guid.Empty, Now, weightKg: 70));
    }

    [Fact]
    public void Update_changes_values_and_recomputes_bmi()
    {
        var measurement = Measure(weightKg: 70, heightCm: 175);

        measurement.Update(Now.AddDays(1), 80, 180, null, null, null, null, null, null, null, "  ", Now.AddDays(1), UserId);

        Assert.Equal(24.69m, measurement.ComputeBmi());
        Assert.Null(measurement.Notes);
        Assert.Equal(Now.AddDays(1), measurement.MeasuredAt);
    }

    [Fact]
    public void Update_cannot_clear_every_value()
    {
        var measurement = Measure(weightKg: 70);

        Assert.Throws<DomainException>(() =>
            measurement.Update(Now, null, null, null, null, null, null, null, null, null, null, Now, UserId));
    }

    [Fact]
    public void Deleted_measurement_cannot_be_updated()
    {
        var measurement = Measure(weightKg: 70);
        measurement.SoftDelete(Now, UserId);

        Assert.Throws<DomainException>(() =>
            measurement.Update(Now, 71, null, null, null, null, null, null, null, null, null, Now, UserId));
    }

    [Fact]
    public void Photo_requires_url_and_valid_pose()
    {
        Assert.Throws<DomainException>(() =>
            ProgressPhoto.Create(TenantId, AthleteId, Now, PhotoPose.Front, " ", UserId, Now));
        Assert.Throws<DomainException>(() =>
            ProgressPhoto.Create(TenantId, AthleteId, Now, (PhotoPose)99, "https://x.test/a.jpg", UserId, Now));

        var photo = ProgressPhoto.Create(TenantId, AthleteId, Now, PhotoPose.Side, "https://x.test/a.jpg", UserId, Now);
        Assert.Equal(PhotoPose.Side, photo.Pose);
    }

    [Fact]
    public void Assessment_validates_score_and_text()
    {
        Assert.Throws<DomainException>(() =>
            FitnessAssessment.Create(TenantId, AthleteId, Now, "Fuerza", "Buen nivel", UserId, Now, score: 11));
        Assert.Throws<DomainException>(() =>
            FitnessAssessment.Create(TenantId, AthleteId, Now, " ", "Buen nivel", UserId, Now));

        var assessment = FitnessAssessment.Create(TenantId, AthleteId, Now, "Fuerza", "Buen nivel", UserId, Now, 8);
        Assert.Equal(8, assessment.Score);
    }
}
