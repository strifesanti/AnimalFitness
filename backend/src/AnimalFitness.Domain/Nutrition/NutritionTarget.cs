using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Nutrition;

/// <summary>Objetivos diarios de nutrición de un alumno (orientativos).</summary>
public sealed class NutritionTarget : TenantScopedEntity
{
    private NutritionTarget(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Calorías objetivo.</summary>
    public int Calories { get; private set; }

    /// <summary>Proteína en gramos.</summary>
    public decimal ProteinGrams { get; private set; }

    /// <summary>Carbohidratos en gramos.</summary>
    public decimal CarbsGrams { get; private set; }

    /// <summary>Grasas en gramos.</summary>
    public decimal FatGrams { get; private set; }

    /// <summary>Agua objetivo en ml.</summary>
    public int WaterMl { get; private set; }

    /// <summary>Vigente desde (UTC fecha).</summary>
    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>Crea objetivos diarios.</summary>
    public static NutritionTarget Create(
        Guid tenantId,
        Guid athleteProfileId,
        int calories,
        decimal proteinGrams,
        decimal carbsGrams,
        decimal fatGrams,
        int waterMl,
        DateOnly effectiveFrom,
        DateTimeOffset now,
        Guid? createdBy)
    {
        Validate(calories, proteinGrams, carbsGrams, fatGrams, waterMl);
        var target = new NutritionTarget(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            Calories = calories,
            ProteinGrams = proteinGrams,
            CarbsGrams = carbsGrams,
            FatGrams = fatGrams,
            WaterMl = waterMl,
            EffectiveFrom = effectiveFrom,
        };
        target.MarkCreated(now, createdBy);
        return target;
    }

    /// <summary>Actualiza los objetivos.</summary>
    public void Update(
        int calories,
        decimal proteinGrams,
        decimal carbsGrams,
        decimal fatGrams,
        int waterMl,
        DateOnly effectiveFrom,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        Validate(calories, proteinGrams, carbsGrams, fatGrams, waterMl);
        Calories = calories;
        ProteinGrams = proteinGrams;
        CarbsGrams = carbsGrams;
        FatGrams = fatGrams;
        WaterMl = waterMl;
        EffectiveFrom = effectiveFrom;
        MarkUpdated(now, actor);
    }

    private static void Validate(int calories, decimal protein, decimal carbs, decimal fat, int water)
    {
        if (calories is < 500 or > 10000)
        {
            throw new DomainException("Las calorías no son válidas.");
        }

        if (protein is < 0 or > 1000 || carbs is < 0 or > 1000 || fat is < 0 or > 1000)
        {
            throw new DomainException("Los macros no son válidos.");
        }

        if (water is < 0 or > 20000)
        {
            throw new DomainException("El objetivo de agua no es válido.");
        }
    }
}
