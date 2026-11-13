using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Training;

/// <summary>
/// Plan/rutina de entrenamiento. Las sesiones históricas apuntan a una versión concreta;
/// al editar un plan publicado se debe crear una nueva versión.
/// </summary>
public sealed class WorkoutPlan : TenantScopedEntity
{
    private WorkoutPlan(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Nombre.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descripción.</summary>
    public string? Description { get; private set; }

    /// <summary>Objetivo textual (hipertrofia, fuerza, etc.).</summary>
    public string? GoalLabel { get; private set; }

    /// <summary>Número de versión (1, 2, …).</summary>
    public int Version { get; private set; }

    /// <summary>Plan del que deriva esta versión.</summary>
    public Guid? ParentPlanId { get; private set; }

    /// <summary>Estado.</summary>
    public WorkoutPlanStatus Status { get; private set; }

    /// <summary>Entrenador autor (perfil), si aplica.</summary>
    public Guid? AuthorTrainerProfileId { get; private set; }

    /// <summary>Crea un borrador en versión 1.</summary>
    public static WorkoutPlan Create(
        Guid tenantId,
        string name,
        DateTimeOffset now,
        Guid? createdBy,
        string? description = null,
        string? goalLabel = null,
        Guid? authorTrainerProfileId = null)
    {
        var plan = new WorkoutPlan(tenantId)
        {
            Name = TextRules.Required(name, "El nombre", FieldLengths.WorkoutName),
            Description = Optional(description),
            GoalLabel = OptionalGoal(goalLabel),
            Version = 1,
            Status = WorkoutPlanStatus.Draft,
            AuthorTrainerProfileId = authorTrainerProfileId,
        };
        plan.MarkCreated(now, createdBy);
        return plan;
    }

    /// <summary>Actualiza metadatos solo en borrador.</summary>
    public void UpdateDraft(
        string name,
        string? description,
        string? goalLabel,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureDraft();
        Name = TextRules.Required(name, "El nombre", FieldLengths.WorkoutName);
        Description = Optional(description);
        GoalLabel = OptionalGoal(goalLabel);
        MarkUpdated(now, actor);
    }

    /// <summary>Publica el plan.</summary>
    public void Publish(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status == WorkoutPlanStatus.Published)
        {
            throw new DomainException("El plan ya está publicado.");
        }

        if (Status == WorkoutPlanStatus.Archived)
        {
            throw new DomainException("No se puede publicar un plan archivado.");
        }

        MarkUpdated(now, actor);
        Status = WorkoutPlanStatus.Published;
    }

    /// <summary>Archiva el plan.</summary>
    public void Archive(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status == WorkoutPlanStatus.Archived)
        {
            throw new DomainException("El plan ya está archivado.");
        }

        MarkUpdated(now, actor);
        Status = WorkoutPlanStatus.Archived;
    }

    /// <summary>Crea una nueva versión en borrador a partir de este plan.</summary>
    public WorkoutPlan CreateNextVersion(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != WorkoutPlanStatus.Published && Status != WorkoutPlanStatus.Archived)
        {
            throw new DomainException("Solo se versionan planes publicados o archivados.");
        }

        var next = new WorkoutPlan(TenantId)
        {
            Name = Name,
            Description = Description,
            GoalLabel = GoalLabel,
            Version = Version + 1,
            ParentPlanId = Id,
            Status = WorkoutPlanStatus.Draft,
            AuthorTrainerProfileId = AuthorTrainerProfileId,
        };
        next.MarkCreated(now, actor);
        return next;
    }

    private void EnsureDraft()
    {
        EnsureNotDeleted();
        if (Status != WorkoutPlanStatus.Draft)
        {
            throw new DomainException("Solo se puede editar un plan en borrador. Crea una nueva versión.");
        }
    }

    private static string? Optional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : TextRules.Required(value, "La descripción", FieldLengths.ExerciseDescription);

    private static string? OptionalGoal(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : TextRules.Required(value, "El objetivo", FieldLengths.MuscleOrEquipment);
}
