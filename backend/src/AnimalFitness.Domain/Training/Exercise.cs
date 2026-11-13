using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Ejercicio de la biblioteca de la organización.</summary>
public sealed class Exercise : TenantScopedEntity
{
    private Exercise(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Categoría opcional.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>Nombre.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descripción.</summary>
    public string? Description { get; private set; }

    /// <summary>Grupo muscular principal.</summary>
    public string PrimaryMuscle { get; private set; } = string.Empty;

    /// <summary>Músculos secundarios (texto libre).</summary>
    public string? SecondaryMuscles { get; private set; }

    /// <summary>Tipo.</summary>
    public ExerciseType Type { get; private set; }

    /// <summary>Equipamiento.</summary>
    public string? Equipment { get; private set; }

    /// <summary>Dificultad.</summary>
    public ExerciseDifficulty Difficulty { get; private set; }

    /// <summary>Instrucciones de ejecución.</summary>
    public string? Instructions { get; private set; }

    /// <summary>Variación de otro ejercicio de la misma organización.</summary>
    public Guid? VariationOfExerciseId { get; private set; }

    /// <summary>Crea un ejercicio.</summary>
    public static Exercise Create(
        Guid tenantId,
        string name,
        string primaryMuscle,
        ExerciseType type,
        ExerciseDifficulty difficulty,
        DateTimeOffset now,
        Guid? createdBy,
        Guid? categoryId = null,
        string? description = null,
        string? secondaryMuscles = null,
        string? equipment = null,
        string? instructions = null,
        Guid? variationOfExerciseId = null)
    {
        var exercise = new Exercise(tenantId);
        exercise.Apply(
            name,
            primaryMuscle,
            type,
            difficulty,
            categoryId,
            description,
            secondaryMuscles,
            equipment,
            instructions,
            variationOfExerciseId);
        exercise.MarkCreated(now, createdBy);
        return exercise;
    }

    /// <summary>Actualiza los metadatos del ejercicio.</summary>
    public void Update(
        string name,
        string primaryMuscle,
        ExerciseType type,
        ExerciseDifficulty difficulty,
        Guid? categoryId,
        string? description,
        string? secondaryMuscles,
        string? equipment,
        string? instructions,
        Guid? variationOfExerciseId,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        Apply(
            name,
            primaryMuscle,
            type,
            difficulty,
            categoryId,
            description,
            secondaryMuscles,
            equipment,
            instructions,
            variationOfExerciseId);
        MarkUpdated(now, actor);
    }

    private void Apply(
        string name,
        string primaryMuscle,
        ExerciseType type,
        ExerciseDifficulty difficulty,
        Guid? categoryId,
        string? description,
        string? secondaryMuscles,
        string? equipment,
        string? instructions,
        Guid? variationOfExerciseId)
    {
        if (variationOfExerciseId == Id)
        {
            throw new DomainException("Un ejercicio no puede ser variación de sí mismo.");
        }

        Name = TextRules.Required(name, "El nombre", FieldLengths.ExerciseName);
        PrimaryMuscle = TextRules.Required(primaryMuscle, "El grupo muscular", FieldLengths.MuscleOrEquipment);
        Type = TextRules.Defined(type, "El tipo");
        Difficulty = TextRules.Defined(difficulty, "La dificultad");
        CategoryId = categoryId;
        Description = Optional(description, "La descripción", FieldLengths.ExerciseDescription);
        SecondaryMuscles = Optional(secondaryMuscles, "Los músculos secundarios", FieldLengths.MuscleOrEquipment);
        Equipment = Optional(equipment, "El equipamiento", FieldLengths.MuscleOrEquipment);
        Instructions = Optional(instructions, "Las instrucciones", FieldLengths.Instructions);
        VariationOfExerciseId = variationOfExerciseId;
    }

    private static string? Optional(string? value, string field, int max)
        => string.IsNullOrWhiteSpace(value) ? null : TextRules.Required(value, field, max);
}
