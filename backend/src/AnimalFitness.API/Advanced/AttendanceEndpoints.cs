using AnimalFitness.API.Http;
using AnimalFitness.Application.Features.Attendance;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Advanced;

/// <summary>Endpoints de asistencia (Fase 5).</summary>
public static class AttendanceEndpoints
{
    /// <summary>Registra rutas de listado, registro (upsert) y estadísticas de asistencia.</summary>
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/t/{slug}/attendance").WithTags("Attendance");

        group.MapGet("/", async (
            Guid? athleteProfileId,
            DateOnly? from,
            DateOnly? to,
            IAttendanceService service,
            CancellationToken ct)
            => (await service.ListAsync(athleteProfileId, from, to, ct)).ToHttpResult());

        // POST y PUT hacen lo mismo: crean o actualizan el registro del alumno en esa fecha.
        group.MapPost("/", (
            [FromBody] UpsertAttendanceRequest body,
            IAttendanceService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.UpsertAsync(body, ct)));
        group.MapPut("/", (
            [FromBody] UpsertAttendanceRequest body,
            IAttendanceService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct)
            => EndpointRunner.RunAsync(antiforgery, http, () => service.UpsertAsync(body, ct)));

        group.MapGet("/stats", async (
            Guid? athleteProfileId,
            DateOnly? from,
            DateOnly? to,
            IAttendanceService service,
            CancellationToken ct)
            => (await service.GetStatsAsync(athleteProfileId, from, to, ct)).ToHttpResult());

        return endpoints;
    }
}
