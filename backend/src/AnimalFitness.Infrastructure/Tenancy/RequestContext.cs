using AnimalFitness.Application.Abstractions;
using AnimalFitness.Domain.Accounts;

namespace AnimalFitness.Infrastructure.Tenancy;

/// <summary>
/// Contexto de una petición o proceso. Existe una instancia por ámbito de DI.
/// Una vez asignada la organización, no puede cambiarse dentro del mismo ámbito.
/// </summary>
public sealed class RequestContext : IRequestContext
{
    /// <inheritdoc />
    public Guid? TenantId { get; private set; }

    /// <inheritdoc />
    public Guid? UserId { get; private set; }

    /// <inheritdoc />
    public MembershipRole? Role { get; private set; }

    /// <summary>
    /// Registra a la persona que actúa, sin organización. Útil en rutas de plataforma.
    /// </summary>
    /// <param name="userId">Persona autenticada.</param>
    public void EstablishUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("La persona no puede ser vacía.", nameof(userId));
        }

        if (UserId is { } current && current != userId)
        {
            throw new InvalidOperationException("El contexto ya está asociado a otra persona.");
        }

        UserId = userId;
    }

    /// <summary>
    /// Asigna la organización, la persona y el rol. Debe llamarse después de verificar la membresía.
    /// </summary>
    /// <param name="tenantId">Organización verificada.</param>
    /// <param name="userId">Persona que actúa.</param>
    /// <param name="role">Rol verificado en la organización.</param>
    public void Establish(Guid tenantId, Guid userId, MembershipRole role)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("La organización no puede ser vacía.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("La persona no puede ser vacía.", nameof(userId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException("El rol no es válido.", nameof(role));
        }

        if (TenantId is { } current && current != tenantId)
        {
            throw new InvalidOperationException("El contexto ya está asociado a otra organización.");
        }

        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }

    /// <summary>
    /// Asigna la organización y la persona. Conservado para procesos del sistema sin rol de membresía.
    /// </summary>
    /// <param name="tenantId">Organización verificada.</param>
    /// <param name="userId">Persona que actúa, si existe.</param>
    public void Establish(Guid tenantId, Guid? userId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("La organización no puede ser vacía.", nameof(tenantId));
        }

        if (TenantId is { } current && current != tenantId)
        {
            throw new InvalidOperationException("El contexto ya está asociado a otra organización.");
        }

        TenantId = tenantId;
        UserId = userId;
        Role = null;
    }
}
