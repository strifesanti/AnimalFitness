using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Training;

/// <summary>Consultas compartidas por los servicios de entrenamiento.</summary>
internal static class TrainingLookups
{
    /// <summary>Perfil de alumno del usuario en la organización activa, si existe.</summary>
    public static Task<AthleteProfile?> FindAthleteProfileByUserAsync(
        AppDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
        => db.AthleteProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
}
