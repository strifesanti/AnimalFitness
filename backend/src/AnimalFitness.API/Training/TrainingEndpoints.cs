using AnimalFitness.API.Http;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Training;
using AnimalFitness.Domain.Training;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Training;

/// <summary>Endpoints de entrenamiento (Fase 3): ejercicios, planes y sesiones.</summary>
public static class TrainingEndpoints
{
    /// <summary>Registra rutas de ejercicios, planes de entrenamiento y sesiones.</summary>
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapExercises(endpoints);
        MapWorkouts(endpoints);
        MapSessions(endpoints);
        return endpoints;
    }

    private static async Task<IResult> RunAsync(
        IAntiforgery antiforgery,
        HttpContext http,
        Func<Task<AppResult>> action)
    {
        var csrf = await Csrf.ValidateAsync(antiforgery, http);
        return csrf ?? (await action()).ToHttpResult();
    }

    private static void MapExercises(IEndpointRouteBuilder endpoints)
    {
        var categories = endpoints.MapGroup("/api/v1/t/{slug}/exercises/categories").WithTags("Exercises");
        categories.MapGet("/", async (IExerciseService service, CancellationToken ct)
            => (await service.ListCategoriesAsync(ct)).ToHttpResult());
        categories.MapPost("/", (
            [FromBody] CreateExerciseCategoryRequest body,
            IExerciseService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreateCategoryAsync(body, ct)));
        categories.MapPut("/{id:guid}", (
            Guid id,
            [FromBody] UpdateExerciseCategoryRequest body,
            IExerciseService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdateCategoryAsync(id, body, ct)));

        var exercises = endpoints.MapGroup("/api/v1/t/{slug}/exercises").WithTags("Exercises");
        exercises.MapGet("/", async (
            string? q,
            ExerciseType? type,
            string? muscle,
            IExerciseService service,
            CancellationToken ct)
            => (await service.ListExercisesAsync(q, type, muscle, ct)).ToHttpResult());
        exercises.MapPost("/", (
            [FromBody] SaveExerciseRequest body,
            IExerciseService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreateExerciseAsync(body, ct)));
        exercises.MapPut("/{id:guid}", (
            Guid id,
            [FromBody] SaveExerciseRequest body,
            IExerciseService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdateExerciseAsync(id, body, ct)));
        exercises.MapPost("/{id:guid}/media", (
            Guid id,
            [FromBody] AddExerciseMediaRequest body,
            IExerciseService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.AddMediaAsync(id, body, ct)));
    }

    private static void MapWorkouts(IEndpointRouteBuilder endpoints)
    {
        var workouts = endpoints.MapGroup("/api/v1/t/{slug}/workouts").WithTags("Workouts");

        workouts.MapGet("/", async (IWorkoutService service, CancellationToken ct)
            => (await service.ListPlansAsync(ct)).ToHttpResult());
        workouts.MapGet("/{planId:guid}", async (Guid planId, IWorkoutService service, CancellationToken ct)
            => (await service.GetPlanAsync(planId, ct)).ToHttpResult());
        workouts.MapPost("/", (
            [FromBody] CreateWorkoutPlanRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreatePlanAsync(body, ct)));
        workouts.MapPut("/{planId:guid}", (
            Guid planId,
            [FromBody] UpdateWorkoutPlanRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdatePlanAsync(planId, body, ct)));
        workouts.MapPost("/{planId:guid}/publish", (
            Guid planId,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.PublishPlanAsync(planId, ct)));
        workouts.MapPost("/{planId:guid}/archive", (
            Guid planId,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.ArchivePlanAsync(planId, ct)));
        workouts.MapPost("/{planId:guid}/new-version", (
            Guid planId,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreateNewVersionAsync(planId, ct)));
        workouts.MapPost("/{planId:guid}/days", (
            Guid planId,
            [FromBody] CreateWorkoutDayRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.AddDayAsync(planId, body, ct)));
        workouts.MapPut("/days/{dayId:guid}", (
            Guid dayId,
            [FromBody] UpdateWorkoutDayRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdateDayAsync(dayId, body, ct)));
        workouts.MapPost("/days/{dayId:guid}/exercises", (
            Guid dayId,
            [FromBody] AddWorkoutExerciseRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.AddExerciseAsync(dayId, body, ct)));
        workouts.MapPut("/exercises/{id:guid}", (
            Guid id,
            [FromBody] UpdateWorkoutExerciseRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdateExerciseAsync(id, body, ct)));
        workouts.MapPost("/{planId:guid}/assign", (
            Guid planId,
            [FromBody] AssignWorkoutRequest body,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.AssignAsync(planId, body, ct)));
        workouts.MapPost("/assignments/{id:guid}/end", (
            Guid id,
            IWorkoutService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.EndAssignmentAsync(id, ct)));
    }

    private static void MapSessions(IEndpointRouteBuilder endpoints)
    {
        var sessions = endpoints.MapGroup("/api/v1/t/{slug}/sessions").WithTags("Sessions");

        sessions.MapGet("/", async (ISessionService service, CancellationToken ct)
            => (await service.ListSessionsAsync(ct)).ToHttpResult());
        sessions.MapPost("/start", (
            [FromBody] StartSessionRequest body,
            ISessionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.StartAsync(body, ct)));
        sessions.MapPost("/{id:guid}/complete", (
            Guid id,
            [FromBody] CompleteSessionRequest? body,
            ISessionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CompleteAsync(id, body, ct)));
        sessions.MapPost("/{id:guid}/cancel", (
            Guid id,
            ISessionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CancelAsync(id, ct)));
        sessions.MapPost("/{id:guid}/logs", (
            Guid id,
            [FromBody] LogSetRequest body,
            ISessionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.LogSetAsync(id, body, ct)));
        sessions.MapGet("/{id:guid}/logs", async (Guid id, ISessionService service, CancellationToken ct)
            => (await service.ListLogsAsync(id, ct)).ToHttpResult());
    }
}
