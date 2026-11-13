using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Common;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Training;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Advanced;

/// <summary>Alumno objetivo de una operación: uno concreto, «todos» (solo personal) o un fallo.</summary>
public sealed record AthleteTarget(Guid? AthleteId, bool IsAll, AppResult? Failure);

/// <summary>
/// Resuelve sobre qué alumno actúa una petición. Los alumnos solo tocan su propio perfil;
/// Owner y Trainer indican el alumno con <c>athleteProfileId</c>.
/// </summary>
public sealed class AthleteResolver(
    AppDbContext db,
    IRequestContext requestContext,
    AdminAccess access,
    TimeProvider timeProvider)
{
    /// <summary>El usuario actual es un alumno.</summary>
    public bool IsAthlete => requestContext.Role == MembershipRole.Athlete;

    /// <summary>
    /// Resuelve para lecturas. Un alumno sin perfil recibe <c>null</c> (resultado vacío).
    /// Con <paramref name="allowAll"/>, el personal puede omitir el alumno para ver todos.
    /// </summary>
    public async Task<AthleteTarget> ForReadAsync(
        string permission,
        Guid? requested,
        bool allowAll,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await access.RequirePermissionAsync(permission, cancellationToken);
        if (failure is not null)
        {
            return new AthleteTarget(null, false, failure);
        }

        if (IsAthlete)
        {
            var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, access.RequireUserId(), cancellationToken);
            if (requested is { } other && other != own?.Id)
            {
                return new AthleteTarget(null, false, AdminAccess.Fail("Solo puedes ver tus propios datos.", 403));
            }

            return new AthleteTarget(own?.Id, false, null);
        }

        if (requested is null && allowAll)
        {
            return new AthleteTarget(null, true, null);
        }

        return await RequireAthleteAsync(requested, cancellationToken);
    }

    /// <summary>Resuelve para escrituras; crea el perfil propio de un alumno si falta (si se pide).</summary>
    public async Task<AthleteTarget> ForWriteAsync(
        string permission,
        Guid? requested,
        bool createOwnIfMissing,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await access.RequirePermissionAsync(permission, cancellationToken);
        if (failure is not null)
        {
            return new AthleteTarget(null, false, failure);
        }

        if (IsAthlete)
        {
            var userId = access.RequireUserId();
            var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, userId, cancellationToken);
            if (own is null)
            {
                if (!createOwnIfMissing)
                {
                    return new AthleteTarget(null, false, AdminAccess.Fail("No se encontró tu perfil de alumno.", 404));
                }

                own = AthleteProfile.Create(access.RequireTenantId(), userId, timeProvider.GetUtcNow(), userId);
                db.AthleteProfiles.Add(own);
            }

            if (requested is { } other && other != own.Id)
            {
                return new AthleteTarget(null, false, AdminAccess.Fail("Solo puedes registrar tus propios datos.", 403));
            }

            return new AthleteTarget(own.Id, false, null);
        }

        return await RequireAthleteAsync(requested, cancellationToken);
    }

    /// <summary>Un alumno solo puede tocar registros de su propio perfil.</summary>
    public async Task<bool> CanTouchAsync(Guid athleteProfileId, CancellationToken cancellationToken)
    {
        if (!IsAthlete)
        {
            return true;
        }

        var own = await TrainingLookups.FindAthleteProfileByUserAsync(db, access.RequireUserId(), cancellationToken);
        return own is not null && own.Id == athleteProfileId;
    }

    /// <summary>Nombres completos de alumnos por perfil, para proyecciones.</summary>
    public async Task<Dictionary<Guid, string>> NamesAsync(
        IReadOnlyCollection<Guid> athleteProfileIds,
        CancellationToken cancellationToken)
    {
        if (athleteProfileIds.Count == 0)
        {
            return [];
        }

        var profiles = await db.AthleteProfiles.AsNoTracking()
            .Where(profile => athleteProfileIds.Contains(profile.Id))
            .ToListAsync(cancellationToken);
        var userIds = profiles.Select(profile => profile.UserId).Distinct().ToList();
        var users = await db.UserProfiles.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return profiles.ToDictionary(
            profile => profile.Id,
            profile => users.TryGetValue(profile.UserId, out var user) ? user.FullName.Trim() : string.Empty);
    }

    private async Task<AthleteTarget> RequireAthleteAsync(Guid? requested, CancellationToken cancellationToken)
    {
        if (requested is not { } id || id == Guid.Empty)
        {
            return new AthleteTarget(null, false, AdminAccess.Fail("Indica el alumno.", 400));
        }

        if (!await db.AthleteProfiles.AnyAsync(profile => profile.Id == id, cancellationToken))
        {
            return new AthleteTarget(null, false, AdminAccess.Fail("No se encontró el alumno.", 404));
        }

        return new AthleteTarget(id, false, null);
    }
}
