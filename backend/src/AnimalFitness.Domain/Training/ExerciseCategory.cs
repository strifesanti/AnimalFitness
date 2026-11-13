using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>Categoría de la biblioteca de ejercicios de una organización.</summary>
public sealed class ExerciseCategory : TenantScopedEntity
{
    private ExerciseCategory(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Nombre visible.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descripción opcional.</summary>
    public string? Description { get; private set; }

    /// <summary>Orden de presentación.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Crea una categoría.</summary>
    public static ExerciseCategory Create(
        Guid tenantId,
        string name,
        DateTimeOffset now,
        Guid? createdBy,
        string? description = null,
        int sortOrder = 0)
    {
        var category = new ExerciseCategory(tenantId)
        {
            Name = TextRules.Required(name, "El nombre", FieldLengths.ExerciseName),
            Description = OptionalText(description, "La descripción", FieldLengths.ExerciseDescription),
            SortOrder = sortOrder,
        };
        category.MarkCreated(now, createdBy);
        return category;
    }

    /// <summary>Actualiza nombre, descripción y orden.</summary>
    public void Update(string name, string? description, int sortOrder, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        Name = TextRules.Required(name, "El nombre", FieldLengths.ExerciseName);
        Description = OptionalText(description, "La descripción", FieldLengths.ExerciseDescription);
        SortOrder = sortOrder;
        MarkUpdated(now, actor);
    }

    private static string? OptionalText(string? value, string field, int max)
        => string.IsNullOrWhiteSpace(value) ? null : TextRules.Required(value, field, max);
}
