using AnimalFitness.API.Http;
using AnimalFitness.Application.Features.Challenges;
using AnimalFitness.Domain.Challenges;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Advanced;

/// <summary>Endpoints de retos, clasificación y logros (Fase 5).</summary>
public static class ChallengeEndpoints
{
    /// <summary>Registra rutas de retos, participación, actividades, clasificación y logros.</summary>
    public static IEndpointRouteBuilder MapChallengeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/t/{slug}/challenges").WithTags("Challenges");

        group.MapGet("/", async (ChallengeStatus? status, IChallengeService service, CancellationToken ct)
            => (await service.ListAsync(status, ct)).ToHttpResult());
        group.MapPost("/", (
            [FromBody] CreateChallengeRequest body,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CreateAsync(body, ct)));
        group.MapGet("/{id:guid}", async (Guid id, IChallengeService service, CancellationToken ct)
            => (await service.GetAsync(id, ct)).ToHttpResult());

        group.MapPost("/{id:guid}/activate", (
            Guid id,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.ActivateAsync(id, ct)));
        group.MapPost("/{id:guid}/complete", (
            Guid id,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CompleteAsync(id, ct)));
        group.MapPost("/{id:guid}/cancel", (
            Guid id,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.CancelAsync(id, ct)));

        group.MapPost("/{id:guid}/join", (
            Guid id,
            [FromBody] JoinChallengeRequest? body,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(
                antiforgery,
                http,
                () => service.JoinAsync(id, body ?? new JoinChallengeRequest(null), ct)));

        group.MapGet("/{id:guid}/activities", async (Guid id, IChallengeService service, CancellationToken ct)
            => (await service.ListActivitiesAsync(id, ct)).ToHttpResult());
        group.MapPost("/{id:guid}/activities", (
            Guid id,
            [FromBody] AddChallengeActivityRequest body,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.AddActivityAsync(id, body, ct)));

        group.MapGet("/{id:guid}/leaderboard", async (Guid id, IChallengeService service, CancellationToken ct)
            => (await service.GetLeaderboardAsync(id, ct)).ToHttpResult());

        // Logros
        var achievements = endpoints.MapGroup("/api/v1/t/{slug}/achievements").WithTags("Challenges");
        achievements.MapGet("/", async (Guid? athleteProfileId, IChallengeService service, CancellationToken ct)
            => (await service.ListAchievementsAsync(athleteProfileId, ct)).ToHttpResult());
        achievements.MapPost("/", (
            [FromBody] AwardAchievementRequest body,
            IChallengeService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.AwardAchievementAsync(body, ct)));

        return endpoints;
    }
}
