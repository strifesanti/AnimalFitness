using AnimalFitness.API.Http;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Progress;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Progress;

/// <summary>Endpoints de seguimiento del progreso (Fase 4).</summary>
public static class ProgressEndpoints
{
    /// <summary>Registra rutas de mediciones, fotos, valoraciones, objetivos y gráficos.</summary>
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/t/{slug}/progress").WithTags("Progress");

        // Mediciones
        group.MapGet("/measurements", async (Guid? athleteProfileId, IProgressService service, CancellationToken ct)
            => (await service.ListMeasurementsAsync(athleteProfileId, ct)).ToHttpResult());
        group.MapPost("/measurements", (
            [FromBody] SaveBodyMeasurementRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreateMeasurementAsync(body, ct)));
        group.MapPut("/measurements/{id:guid}", (
            Guid id,
            [FromBody] SaveBodyMeasurementRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdateMeasurementAsync(id, body, ct)));

        // Fotos
        group.MapGet("/photos", async (Guid? athleteProfileId, IProgressService service, CancellationToken ct)
            => (await service.ListPhotosAsync(athleteProfileId, ct)).ToHttpResult());
        group.MapPost("/photos", (
            [FromBody] SaveProgressPhotoRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreatePhotoAsync(body, ct)));
        group.MapPut("/photos/{id:guid}", (
            Guid id,
            [FromBody] SaveProgressPhotoRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdatePhotoAsync(id, body, ct)));

        // Valoraciones
        group.MapGet("/assessments", async (Guid? athleteProfileId, IProgressService service, CancellationToken ct)
            => (await service.ListAssessmentsAsync(athleteProfileId, ct)).ToHttpResult());
        group.MapPost("/assessments", (
            [FromBody] SaveFitnessAssessmentRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreateAssessmentAsync(body, ct)));
        group.MapPut("/assessments/{id:guid}", (
            Guid id,
            [FromBody] SaveFitnessAssessmentRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.UpdateAssessmentAsync(id, body, ct)));

        // Objetivos
        group.MapGet("/goals", async (Guid? athleteProfileId, IProgressService service, CancellationToken ct)
            => (await service.ListGoalsAsync(athleteProfileId, ct)).ToHttpResult());
        group.MapPost("/goals", (
            [FromBody] CreateProgressGoalRequest body,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CreateGoalAsync(body, ct)));
        group.MapPost("/goals/{id:guid}/complete", (
            Guid id,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CompleteGoalAsync(id, ct)));
        group.MapPost("/goals/{id:guid}/cancel", (
            Guid id,
            IProgressService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => RunAsync(antiforgery, http, () => service.CancelGoalAsync(id, ct)));

        // Gráficos
        group.MapGet("/charts", async (
            Guid? athleteProfileId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            IProgressService service,
            CancellationToken ct)
            => (await service.GetChartsAsync(athleteProfileId, from, to, ct)).ToHttpResult());

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
}
