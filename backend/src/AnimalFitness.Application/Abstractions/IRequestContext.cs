using AnimalFitness.Domain.Accounts;

namespace AnimalFitness.Application.Abstractions;

/// <summary>
/// Contexto de la petición o del proceso en curso: organización activa, persona y rol verificados.
/// El valor lo establece el servidor a partir de la sesión y la membresía, nunca desde el cliente sin validar.
/// </summary>
public interface IRequestContext
{
    /// <summary>Organización activa. Nulo cuando la operación no pertenece a ninguna organización.</summary>
    Guid? TenantId { get; }

    /// <summary>Persona que actúa. Nulo en procesos del sistema.</summary>
    Guid? UserId { get; }

    /// <summary>Rol de la persona en la organización activa. Nulo fuera de una organización.</summary>
    MembershipRole? Role { get; }

    /// <summary>Indica si el contexto ya tiene una organización establecida.</summary>
    bool HasTenant => TenantId.HasValue;
}
