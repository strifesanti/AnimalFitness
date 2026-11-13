using AnimalFitness.API.Http;
using AnimalFitness.Application.Features.Nutrition;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Advanced;

/// <summary>Endpoints de nutrición (Fase 5).</summary>
public static class NutritionEndpoints
{
    /// <summary>Registra rutas de objetivos, comidas, agua y resumen diario.</summary>
    public static IEndpointRouteBuilder MapNutritionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/t/{slug}/nutrition").WithTags("Nutrition");

        // Objetivos
        group.MapGet("/targets", async (Guid? athleteProfileId, INutritionService service, CancellationToken ct)
            => (await service.ListTargetsAsync(athleteProfileId, ct)).ToHttpResult());
        group.MapPost("/targets", (
            [FromBody] SaveNutritionTargetRequest body,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CreateTargetAsync(body, ct)));
        group.MapPut("/targets/{id:guid}", (
            Guid id,
            [FromBody] SaveNutritionTargetRequest body,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.UpdateTargetAsync(id, body, ct)));
        group.MapDelete("/targets/{id:guid}", (
            Guid id,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.DeleteTargetAsync(id, ct)));

        // Comidas
        group.MapGet("/foods", async (Guid? athleteProfileId, DateOnly? date, INutritionService service, CancellationToken ct)
            => (await service.ListFoodsAsync(athleteProfileId, date, ct)).ToHttpResult());
        group.MapPost("/foods", (
            [FromBody] CreateFoodEntryRequest body,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CreateFoodAsync(body, ct)));
        group.MapDelete("/foods/{id:guid}", (
            Guid id,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.DeleteFoodAsync(id, ct)));

        // Agua
        group.MapGet("/water", async (Guid? athleteProfileId, DateOnly? date, INutritionService service, CancellationToken ct)
            => (await service.ListWaterAsync(athleteProfileId, date, ct)).ToHttpResult());
        group.MapPost("/water", (
            [FromBody] CreateWaterLogRequest body,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CreateWaterAsync(body, ct)));
        group.MapDelete("/water/{id:guid}", (
            Guid id,
            INutritionService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.DeleteWaterAsync(id, ct)));

        // Resumen diario
        group.MapGet("/summary", async (Guid? athleteProfileId, DateOnly? date, INutritionService service, CancellationToken ct)
            => (await service.GetSummaryAsync(athleteProfileId, date, ct)).ToHttpResult());

        return endpoints;
    }
}
