using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Accounts;

/// <summary>
/// Vínculo de una persona con una organización y el rol que tiene allí.
/// Una misma persona puede tener una membresía distinta en cada academia.
/// </summary>
public sealed class TenantMembership : TenantScopedEntity
{
    private TenantMembership(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Persona vinculada.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Rol dentro de esta organización.</summary>
    public MembershipRole Role { get; private set; }

    /// <summary>Estado de la vinculación.</summary>
    public MembershipStatus Status { get; private set; }

    /// <summary>Momento en que la persona quedó vinculada, en UTC.</summary>
    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>
    /// Vincula una persona a una organización.
    /// </summary>
    /// <param name="tenantId">Organización.</param>
    /// <param name="userId">Persona.</param>
    /// <param name="role">Rol dentro de la organización.</param>
    /// <param name="now">Momento de la vinculación, en UTC.</param>
    /// <param name="createdBy">Autor.</param>
    /// <returns>La membresía activa.</returns>
    public static TenantMembership Create(
        Guid tenantId,
        Guid userId,
        MembershipRole role,
        DateTimeOffset now,
        Guid? createdBy)
    {
        var membership = new TenantMembership(tenantId)
        {
            UserId = TextRules.RequiredId(userId, "La persona"),
            Role = TextRules.Defined(role, "El rol"),
            Status = MembershipStatus.Active,
            JoinedAt = now,
        };
        membership.MarkCreated(now, createdBy);
        return membership;
    }

    /// <summary>Cambia el rol mientras la membresía está activa.</summary>
    /// <param name="role">Nuevo rol.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void ChangeRole(MembershipRole role, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        EnsureActive();
        var defined = TextRules.Defined(role, "El rol");
        if (defined == Role)
        {
            return;
        }

        MarkUpdated(now, actor);
        Role = defined;
    }

    /// <summary>Suspende el acceso sin terminar la vinculación.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que suspende.</param>
    public void Suspend(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        EnsureActive();
        MarkUpdated(now, actor);
        Status = MembershipStatus.Suspended;
    }

    /// <summary>Restaura una membresía suspendida.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que restaura.</param>
    public void Reactivate(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != MembershipStatus.Suspended)
        {
            throw new DomainException("Solo se puede reactivar una membresía suspendida.");
        }

        MarkUpdated(now, actor);
        Status = MembershipStatus.Active;
    }

    /// <summary>Termina la vinculación. No se reactiva: hace falta crear otra.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que retira el acceso.</param>
    public void Remove(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status == MembershipStatus.Removed)
        {
            throw new DomainException("La membresía ya fue retirada.");
        }

        MarkUpdated(now, actor);
        Status = MembershipStatus.Removed;
    }

    private void EnsureActive()
    {
        if (Status != MembershipStatus.Active)
        {
            throw new DomainException("La membresía no está activa.");
        }
    }
}
