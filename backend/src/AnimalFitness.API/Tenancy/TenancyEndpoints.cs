using AnimalFitness.Application.Abstractions;

namespace AnimalFitness.API.Tenancy;

/// <summary>
/// Endpoints de diagnóstico del contexto multi-tenant.
/// </summary>
public static class TenancyEndpoints
{
    /// <summary>
    /// Registra las rutas de tenancy.
    /// </summary>
    public static IEndpointRouteBuilder MapTenancyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/tenancy/context", GetContext)
            .WithName("GetTenancyContext")
            .WithTags("Tenancy");

        endpoints.MapGet("/api/v1/t/{slug}/context", GetContext)
            .WithName("GetTenantScopedContext")
            .WithTags("Tenancy");

        return endpoints;
    }

    private static IResult GetContext(IRequestContext context)
        => Results.Ok(new
        {
            tenantId = context.TenantId,
            userId = context.UserId,
            role = context.Role?.ToString(),
            hasTenant = context.HasTenant,
        });
}
