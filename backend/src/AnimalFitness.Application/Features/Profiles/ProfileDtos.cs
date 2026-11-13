using AnimalFitness.Domain.Profiles;

namespace AnimalFitness.Application.Features.Profiles;

/// <summary>Actualizar el perfil global del usuario autenticado.</summary>
public sealed record UpdateMyProfileRequest(string FirstName, string LastName, string? PhoneNumber);

/// <summary>Perfil de alumno.</summary>
public sealed record AthleteProfileDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    decimal? HeightCm,
    decimal? WeightKg,
    DateOnly? BirthDate,
    string? Notes,
    Guid? AssignedTrainerProfileId);

/// <summary>Actualizar perfil de alumno.</summary>
public sealed record UpdateAthleteProfileRequest(
    decimal? HeightCm,
    decimal? WeightKg,
    DateOnly? BirthDate,
    string? Notes);

/// <summary>Perfil de entrenador.</summary>
public sealed record TrainerProfileDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Bio,
    string? Specialties,
    int ActiveAthleteCount);

/// <summary>Actualizar perfil de entrenador.</summary>
public sealed record UpdateTrainerProfileRequest(string? Bio, string? Specialties);

/// <summary>Asignar alumno a entrenador.</summary>
public sealed record AssignAthleteRequest(Guid TrainerProfileId, Guid AthleteProfileId);

/// <summary>Objetivo de alumno.</summary>
public sealed record AthleteGoalDto(
    Guid Id,
    Guid AthleteProfileId,
    string Title,
    string? Description,
    decimal? TargetValue,
    string? Unit,
    DateOnly? DueDate,
    AthleteGoalStatus Status);

/// <summary>Crear objetivo.</summary>
public sealed record CreateAthleteGoalRequest(
    Guid AthleteProfileId,
    string Title,
    string? Description,
    decimal? TargetValue,
    string? Unit,
    DateOnly? DueDate);
