using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Profiles;

/// <summary>
/// Perfil deportivo de un alumno dentro de una organización.
/// </summary>
public sealed class AthleteProfile : TenantScopedEntity
{
    private AthleteProfile(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Persona global asociada.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Estatura en centímetros.</summary>
    public decimal? HeightCm { get; private set; }

    /// <summary>Peso corporal actual en kilogramos.</summary>
    public decimal? WeightKg { get; private set; }

    /// <summary>Fecha de nacimiento.</summary>
    public DateOnly? BirthDate { get; private set; }

    /// <summary>Notas visibles para el equipo de la academia.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Crea el perfil del alumno en la organización.
    /// </summary>
    public static AthleteProfile Create(
        Guid tenantId,
        Guid userId,
        DateTimeOffset now,
        Guid? createdBy,
        decimal? heightCm = null,
        decimal? weightKg = null,
        DateOnly? birthDate = null,
        string? notes = null)
    {
        var profile = new AthleteProfile(tenantId)
        {
            UserId = TextRules.RequiredId(userId, "La persona"),
        };
        profile.ApplyPhysical(heightCm, weightKg, birthDate, notes);
        profile.MarkCreated(now, createdBy);
        return profile;
    }

    /// <summary>Actualiza datos físicos y notas.</summary>
    public void Update(
        decimal? heightCm,
        decimal? weightKg,
        DateOnly? birthDate,
        string? notes,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        ApplyPhysical(heightCm, weightKg, birthDate, notes);
        MarkUpdated(now, actor);
    }

    private void ApplyPhysical(decimal? heightCm, decimal? weightKg, DateOnly? birthDate, string? notes)
    {
        if (heightCm is < 50 or > 300)
        {
            throw new DomainException("La estatura no es válida.");
        }

        if (weightKg is < 20 or > 400)
        {
            throw new DomainException("El peso no es válido.");
        }

        HeightCm = heightCm;
        WeightKg = weightKg;
        BirthDate = birthDate;
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : TextRules.Required(notes, "Las notas", FieldLengths.ProfileNotes);
    }
}
