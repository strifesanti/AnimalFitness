using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Nutrition;

/// <summary>Registro de consumo de agua.</summary>
public sealed class WaterLog : TenantScopedEntity
{
    private WaterLog(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Día.</summary>
    public DateOnly EntryDate { get; private set; }

    /// <summary>Mililitros consumidos en este registro.</summary>
    public int AmountMl { get; private set; }

    /// <summary>Crea un log de agua.</summary>
    public static WaterLog Create(
        Guid tenantId,
        Guid athleteProfileId,
        DateOnly entryDate,
        int amountMl,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (amountMl is < 1 or > 5000)
        {
            throw new DomainException("La cantidad de agua no es válida.");
        }

        var log = new WaterLog(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            EntryDate = entryDate,
            AmountMl = amountMl,
        };
        log.MarkCreated(now, createdBy);
        return log;
    }
}
