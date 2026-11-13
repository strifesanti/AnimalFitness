using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Profiles;

/// <summary>
/// Perfil profesional de un entrenador dentro de una organización.
/// </summary>
public sealed class TrainerProfile : TenantScopedEntity
{
    private TrainerProfile(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Persona global asociada.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Biografía corta.</summary>
    public string? Bio { get; private set; }

    /// <summary>Especialidades, texto libre.</summary>
    public string? Specialties { get; private set; }

    /// <summary>
    /// Crea el perfil del entrenador en la organización.
    /// </summary>
    public static TrainerProfile Create(
        Guid tenantId,
        Guid userId,
        DateTimeOffset now,
        Guid? createdBy,
        string? bio = null,
        string? specialties = null)
    {
        var profile = new TrainerProfile(tenantId)
        {
            UserId = TextRules.RequiredId(userId, "La persona"),
        };
        profile.ApplyText(bio, specialties);
        profile.MarkCreated(now, createdBy);
        return profile;
    }

    /// <summary>Actualiza biografía y especialidades.</summary>
    public void Update(string? bio, string? specialties, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        ApplyText(bio, specialties);
        MarkUpdated(now, actor);
    }

    private void ApplyText(string? bio, string? specialties)
    {
        Bio = string.IsNullOrWhiteSpace(bio)
            ? null
            : TextRules.Required(bio, "La biografía", FieldLengths.ProfileBio);
        Specialties = string.IsNullOrWhiteSpace(specialties)
            ? null
            : TextRules.Required(specialties, "Las especialidades", FieldLengths.ProfileSpecialties);
    }
}
