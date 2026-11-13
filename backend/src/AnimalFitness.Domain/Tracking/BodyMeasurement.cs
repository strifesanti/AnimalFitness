using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Tracking;

/// <summary>
/// Medición corporal de un alumno en un momento concreto.
/// El IMC se calcula al presentar los datos, no se persiste.
/// </summary>
public sealed class BodyMeasurement : TenantScopedEntity
{
    private BodyMeasurement(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno medido.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Fecha/hora de la medición (UTC).</summary>
    public DateTimeOffset MeasuredAt { get; private set; }

    /// <summary>Peso en kg.</summary>
    public decimal? WeightKg { get; private set; }

    /// <summary>Estatura en cm.</summary>
    public decimal? HeightCm { get; private set; }

    /// <summary>Porcentaje de grasa corporal.</summary>
    public decimal? BodyFatPercent { get; private set; }

    /// <summary>Masa muscular estimada en kg.</summary>
    public decimal? MuscleMassKg { get; private set; }

    /// <summary>Cintura en cm.</summary>
    public decimal? WaistCm { get; private set; }

    /// <summary>Pecho en cm.</summary>
    public decimal? ChestCm { get; private set; }

    /// <summary>Brazo (circunferencia) en cm.</summary>
    public decimal? ArmCm { get; private set; }

    /// <summary>Pierna (circunferencia) en cm.</summary>
    public decimal? LegCm { get; private set; }

    /// <summary>Cadera en cm.</summary>
    public decimal? HipCm { get; private set; }

    /// <summary>Observaciones.</summary>
    public string? Notes { get; private set; }

    /// <summary>Quién registró la medición.</summary>
    public Guid RecordedByUserId { get; private set; }

    /// <summary>Registra una medición.</summary>
    public static BodyMeasurement Create(
        Guid tenantId,
        Guid athleteProfileId,
        DateTimeOffset measuredAt,
        Guid recordedByUserId,
        DateTimeOffset now,
        decimal? weightKg = null,
        decimal? heightCm = null,
        decimal? bodyFatPercent = null,
        decimal? muscleMassKg = null,
        decimal? waistCm = null,
        decimal? chestCm = null,
        decimal? armCm = null,
        decimal? legCm = null,
        decimal? hipCm = null,
        string? notes = null)
    {
        var measurement = new BodyMeasurement(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            MeasuredAt = measuredAt,
            RecordedByUserId = TextRules.RequiredId(recordedByUserId, "Quien registra"),
        };
        measurement.Apply(
            weightKg,
            heightCm,
            bodyFatPercent,
            muscleMassKg,
            waistCm,
            chestCm,
            armCm,
            legCm,
            hipCm,
            notes);
        measurement.EnsureHasData();
        measurement.MarkCreated(now, recordedByUserId);
        return measurement;
    }

    /// <summary>Actualiza valores de la medición.</summary>
    public void Update(
        DateTimeOffset measuredAt,
        decimal? weightKg,
        decimal? heightCm,
        decimal? bodyFatPercent,
        decimal? muscleMassKg,
        decimal? waistCm,
        decimal? chestCm,
        decimal? armCm,
        decimal? legCm,
        decimal? hipCm,
        string? notes,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        MeasuredAt = measuredAt;
        Apply(
            weightKg,
            heightCm,
            bodyFatPercent,
            muscleMassKg,
            waistCm,
            chestCm,
            armCm,
            legCm,
            hipCm,
            notes);
        EnsureHasData();
        MarkUpdated(now, actor);
    }

    /// <summary>IMC si hay peso y estatura válidos; si no, nulo.</summary>
    public decimal? ComputeBmi()
    {
        if (WeightKg is null or <= 0 || HeightCm is null or <= 0)
        {
            return null;
        }

        var meters = HeightCm.Value / 100m;
        return Math.Round(WeightKg.Value / (meters * meters), 2);
    }

    private void Apply(
        decimal? weightKg,
        decimal? heightCm,
        decimal? bodyFatPercent,
        decimal? muscleMassKg,
        decimal? waistCm,
        decimal? chestCm,
        decimal? armCm,
        decimal? legCm,
        decimal? hipCm,
        string? notes)
    {
        WeightKg = RequireRange(weightKg, 20, 400, "El peso");
        HeightCm = RequireRange(heightCm, 50, 300, "La estatura");
        BodyFatPercent = RequireRange(bodyFatPercent, 1, 70, "El porcentaje de grasa");
        MuscleMassKg = RequireRange(muscleMassKg, 10, 200, "La masa muscular");
        WaistCm = RequireRange(waistCm, 30, 300, "La cintura");
        ChestCm = RequireRange(chestCm, 30, 300, "El pecho");
        ArmCm = RequireRange(armCm, 10, 100, "El brazo");
        LegCm = RequireRange(legCm, 20, 150, "La pierna");
        HipCm = RequireRange(hipCm, 30, 300, "La cadera");
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : TextRules.Required(notes, "Las notas", FieldLengths.ProgressNotes);
    }

    private void EnsureHasData()
    {
        if (WeightKg is null
            && HeightCm is null
            && BodyFatPercent is null
            && MuscleMassKg is null
            && WaistCm is null
            && ChestCm is null
            && ArmCm is null
            && LegCm is null
            && HipCm is null)
        {
            throw new DomainException("Debes registrar al menos un valor en la medición.");
        }
    }

    private static decimal? RequireRange(decimal? value, decimal min, decimal max, string field)
    {
        if (value is null)
        {
            return null;
        }

        if (value < min || value > max)
        {
            throw new DomainException($"{field} no es válido.");
        }

        return value;
    }
}
