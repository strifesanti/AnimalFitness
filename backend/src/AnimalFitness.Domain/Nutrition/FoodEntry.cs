using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Nutrition;

/// <summary>Registro de una comida o alimento.</summary>
public sealed class FoodEntry : TenantScopedEntity
{
    private FoodEntry(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Alumno.</summary>
    public Guid AthleteProfileId { get; private set; }

    /// <summary>Día del registro.</summary>
    public DateOnly EntryDate { get; private set; }

    /// <summary>Tipo de comida.</summary>
    public MealType MealType { get; private set; }

    /// <summary>Nombre del alimento o plato.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Calorías.</summary>
    public int Calories { get; private set; }

    /// <summary>Proteína g.</summary>
    public decimal ProteinGrams { get; private set; }

    /// <summary>Carbohidratos g.</summary>
    public decimal CarbsGrams { get; private set; }

    /// <summary>Grasas g.</summary>
    public decimal FatGrams { get; private set; }

    /// <summary>Notas.</summary>
    public string? Notes { get; private set; }

    /// <summary>Crea un registro de comida.</summary>
    public static FoodEntry Create(
        Guid tenantId,
        Guid athleteProfileId,
        DateOnly entryDate,
        MealType mealType,
        string name,
        int calories,
        decimal proteinGrams,
        decimal carbsGrams,
        decimal fatGrams,
        DateTimeOffset now,
        Guid? createdBy,
        string? notes = null)
    {
        if (calories is < 0 or > 10000)
        {
            throw new DomainException("Las calorías no son válidas.");
        }

        var entry = new FoodEntry(tenantId)
        {
            AthleteProfileId = TextRules.RequiredId(athleteProfileId, "El alumno"),
            EntryDate = entryDate,
            MealType = TextRules.Defined(mealType, "El tipo de comida"),
            Name = TextRules.Required(name, "El nombre", FieldLengths.NutritionOrChallengeName),
            Calories = calories,
            ProteinGrams = proteinGrams,
            CarbsGrams = carbsGrams,
            FatGrams = fatGrams,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : TextRules.Required(notes, "Las notas", FieldLengths.ProgressNotes),
        };
        entry.MarkCreated(now, createdBy);
        return entry;
    }
}
