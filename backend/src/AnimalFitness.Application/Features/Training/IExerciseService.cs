using AnimalFitness.Application.Common;
using AnimalFitness.Domain.Training;

namespace AnimalFitness.Application.Features.Training;

/// <summary>Biblioteca de ejercicios y categorías.</summary>
public interface IExerciseService
{
    /// <summary>Lista las categorías de la organización activa.</summary>
    Task<AppResult> ListCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Crea una categoría.</summary>
    Task<AppResult> CreateCategoryAsync(CreateExerciseCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza una categoría.</summary>
    Task<AppResult> UpdateCategoryAsync(Guid categoryId, UpdateExerciseCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lista ejercicios con filtros opcionales por texto, tipo y músculo.</summary>
    Task<AppResult> ListExercisesAsync(string? query, ExerciseType? type, string? muscle, CancellationToken cancellationToken = default);

    /// <summary>Crea un ejercicio.</summary>
    Task<AppResult> CreateExerciseAsync(SaveExerciseRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza un ejercicio.</summary>
    Task<AppResult> UpdateExerciseAsync(Guid exerciseId, SaveExerciseRequest request, CancellationToken cancellationToken = default);

    /// <summary>Agrega una imagen o video a un ejercicio.</summary>
    Task<AppResult> AddMediaAsync(Guid exerciseId, AddExerciseMediaRequest request, CancellationToken cancellationToken = default);
}
