using AnimalFitness.API.Http;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Application.Features.Profiles;
using AnimalFitness.Domain.Accounts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AnimalFitness.API.Admin;

/// <summary>Endpoints de administración (Fase 2).</summary>
public static class AdminEndpoints
{
    /// <summary>Registra rutas de plataforma, organización, miembros, invitaciones y perfiles.</summary>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapPlatform(endpoints);
        MapDirectory(endpoints);
        MapTenantAdmin(endpoints);
        MapProfile(endpoints);
        return endpoints;
    }

    private static void MapPlatform(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/platform/tenants").WithTags("Platform");

        group.MapGet("/", async (IOrganizationService service, CancellationToken ct)
            => (await service.ListPlatformTenantsAsync(ct)).ToHttpResult());

        group.MapGet("/{tenantId:guid}", async (Guid tenantId, IOrganizationService service, CancellationToken ct)
            => (await service.GetPlatformTenantAsync(tenantId, ct)).ToHttpResult());

        group.MapPost("/", async (
            [FromBody] CreateTenantRequest request,
            IOrganizationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CreateTenantAsync(request, ct)).ToHttpResult();
        });

        group.MapPost("/{tenantId:guid}/activate", async (
            Guid tenantId,
            IOrganizationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.ActivateTenantAsync(tenantId, ct)).ToHttpResult();
        });

        group.MapPost("/{tenantId:guid}/suspend", async (
            Guid tenantId,
            IOrganizationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.SuspendTenantAsync(tenantId, ct)).ToHttpResult();
        });
    }

    private static void MapDirectory(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/organizations", async (IOrganizationService service, CancellationToken ct)
            => (await service.ListDirectoryAsync(ct)).ToHttpResult())
            .WithTags("Organizations");

        endpoints.MapPost("/api/v1/organizations/{slug}/join-requests", async (
            string slug,
            IJoinRequestService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.SubmitAsync(slug, ct)).ToHttpResult();
        }).WithTags("Organizations");

        endpoints.MapPost("/api/v1/invitations/accept", async (
            [FromBody] AcceptInvitationRequest request,
            IInvitationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.AcceptAsync(request, ct)).ToHttpResult();
        }).WithTags("Invitations");

        endpoints.MapPost("/api/v1/join-requests/{requestId:guid}/cancel", async (
            Guid requestId,
            IJoinRequestService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CancelAsync(requestId, ct)).ToHttpResult();
        }).WithTags("Organizations");
    }

    private static void MapTenantAdmin(IEndpointRouteBuilder endpoints)
    {
        var org = endpoints.MapGroup("/api/v1/t/{slug}/organization").WithTags("Organization");
        org.MapGet("/", async (IOrganizationService service, CancellationToken ct)
            => (await service.GetCurrentOrganizationAsync(ct)).ToHttpResult());
        org.MapPut("/", async (
            [FromBody] UpdateOrganizationRequest request,
            IOrganizationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.UpdateCurrentOrganizationAsync(request, ct)).ToHttpResult();
        });
        org.MapPut("/settings", async (
            [FromBody] UpdateOrganizationSettingsRequest request,
            IOrganizationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.UpdateCurrentSettingsAsync(request, ct)).ToHttpResult();
        });

        var members = endpoints.MapGroup("/api/v1/t/{slug}/members").WithTags("Members");
        members.MapGet("/", async (MembershipRole? role, IMembershipAdminService service, CancellationToken ct)
            => (await service.ListMembersAsync(role, ct)).ToHttpResult());
        members.MapPost("/{membershipId:guid}/role", async (
            Guid membershipId,
            [FromBody] ChangeMemberRoleRequest request,
            IMembershipAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.ChangeRoleAsync(membershipId, request, ct)).ToHttpResult();
        });
        members.MapPost("/{membershipId:guid}/suspend", async (
            Guid membershipId,
            IMembershipAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.SuspendAsync(membershipId, ct)).ToHttpResult();
        });
        members.MapPost("/{membershipId:guid}/reactivate", async (
            Guid membershipId,
            IMembershipAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.ReactivateAsync(membershipId, ct)).ToHttpResult();
        });
        members.MapPost("/{membershipId:guid}/remove", async (
            Guid membershipId,
            IMembershipAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.RemoveAsync(membershipId, ct)).ToHttpResult();
        });

        var invitations = endpoints.MapGroup("/api/v1/t/{slug}/invitations").WithTags("Invitations");
        invitations.MapGet("/", async (IInvitationService service, CancellationToken ct)
            => (await service.ListAsync(ct)).ToHttpResult());
        invitations.MapPost("/", async (
            [FromBody] CreateInvitationRequest request,
            IInvitationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CreateAsync(request, ct)).ToHttpResult();
        });
        invitations.MapPost("/{invitationId:guid}/cancel", async (
            Guid invitationId,
            IInvitationService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CancelAsync(invitationId, ct)).ToHttpResult();
        });

        var requests = endpoints.MapGroup("/api/v1/t/{slug}/join-requests").WithTags("JoinRequests");
        requests.MapGet("/", async (IJoinRequestService service, CancellationToken ct)
            => (await service.ListAsync(ct)).ToHttpResult());
        requests.MapPost("/{requestId:guid}/approve", async (
            Guid requestId,
            IJoinRequestService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.ApproveAsync(requestId, ct)).ToHttpResult();
        });
        requests.MapPost("/{requestId:guid}/reject", async (
            Guid requestId,
            [FromBody] RejectJoinRequestRequest body,
            IJoinRequestService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.RejectAsync(requestId, body, ct)).ToHttpResult();
        });

        var athletes = endpoints.MapGroup("/api/v1/t/{slug}/athletes").WithTags("Athletes");
        athletes.MapGet("/", async (IProfileAdminService service, CancellationToken ct)
            => (await service.ListAthletesAsync(ct)).ToHttpResult());
        athletes.MapPut("/{athleteProfileId:guid}", async (
            Guid athleteProfileId,
            [FromBody] UpdateAthleteProfileRequest body,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.UpdateAthleteAsync(athleteProfileId, body, ct)).ToHttpResult();
        });
        athletes.MapPost("/assign", async (
            [FromBody] AssignAthleteRequest body,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.AssignAthleteAsync(body, ct)).ToHttpResult();
        });
        athletes.MapPost("/assignments/{assignmentId:guid}/end", async (
            Guid assignmentId,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.UnassignAthleteAsync(assignmentId, ct)).ToHttpResult();
        });
        athletes.MapGet("/{athleteProfileId:guid}/goals", async (
            Guid athleteProfileId,
            IProfileAdminService service,
            CancellationToken ct)
            => (await service.ListGoalsAsync(athleteProfileId, ct)).ToHttpResult());
        athletes.MapPost("/goals", async (
            [FromBody] CreateAthleteGoalRequest body,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CreateGoalAsync(body, ct)).ToHttpResult();
        });
        athletes.MapPost("/goals/{goalId:guid}/complete", async (
            Guid goalId,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CompleteGoalAsync(goalId, ct)).ToHttpResult();
        });
        athletes.MapPost("/goals/{goalId:guid}/cancel", async (
            Guid goalId,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.CancelGoalAsync(goalId, ct)).ToHttpResult();
        });

        var trainers = endpoints.MapGroup("/api/v1/t/{slug}/trainers").WithTags("Trainers");
        trainers.MapGet("/", async (IProfileAdminService service, CancellationToken ct)
            => (await service.ListTrainersAsync(ct)).ToHttpResult());
        trainers.MapPut("/{trainerProfileId:guid}", async (
            Guid trainerProfileId,
            [FromBody] UpdateTrainerProfileRequest body,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.UpdateTrainerAsync(trainerProfileId, body, ct)).ToHttpResult();
        });
    }

    private static void MapProfile(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/v1/profile/me", async (
            [FromBody] UpdateMyProfileRequest body,
            IProfileAdminService service,
            IAntiforgery antiforgery,
            HttpContext http,
            CancellationToken ct) =>
        {
            var csrf = await Csrf.ValidateAsync(antiforgery, http);
            return csrf ?? (await service.UpdateMyProfileAsync(body, ct)).ToHttpResult();
        }).WithTags("Profile");
    }

}
