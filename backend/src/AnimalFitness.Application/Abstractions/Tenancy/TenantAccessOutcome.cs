namespace AnimalFitness.Application.Abstractions.Tenancy;

/// <summary>
/// Resultado de intentar resolver el acceso a una organización.
/// </summary>
public abstract record TenantAccessOutcome
{
    /// <summary>La organización no existe o no es visible.</summary>
    public sealed record NotFound : TenantAccessOutcome;

    /// <summary>La organización existe, pero la persona no tiene una membresía activa.</summary>
    public sealed record Forbidden(string Reason) : TenantAccessOutcome;

    /// <summary>La organización está suspendida.</summary>
    public sealed record Suspended : TenantAccessOutcome;

    /// <summary>Acceso concedido.</summary>
    public sealed record Granted(TenantAccess Access) : TenantAccessOutcome;
}
