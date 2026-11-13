using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Access;

/// <summary>
/// Invitación de una organización a un correo. Guarda el hash del enlace, nunca el token en claro.
/// </summary>
public sealed class Invitation : TenantScopedEntity
{
    private Invitation(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Correo invitado.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Rol que recibirá la persona si acepta.</summary>
    public MembershipRole Role { get; private set; }

    /// <summary>Hash del token de un solo uso. El token en claro no se almacena.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Momento a partir del cual la invitación deja de ser válida, en UTC.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Estado.</summary>
    public InvitationStatus Status { get; private set; }

    /// <summary>Persona que envió la invitación.</summary>
    public Guid InvitedBy { get; private set; }

    /// <summary>Cuenta que aceptó la invitación.</summary>
    public Guid? AcceptedByUserId { get; private set; }

    /// <summary>Momento de la aceptación, en UTC.</summary>
    public DateTimeOffset? AcceptedAt { get; private set; }

    /// <summary>
    /// Crea una invitación pendiente.
    /// </summary>
    /// <param name="tenantId">Organización.</param>
    /// <param name="email">Correo invitado.</param>
    /// <param name="role">Rol que se asignará al aceptar.</param>
    /// <param name="tokenHash">Hash del token. No el token en claro.</param>
    /// <param name="expiresAt">Vencimiento, en UTC.</param>
    /// <param name="now">Momento de creación, en UTC.</param>
    /// <param name="invitedBy">Persona que invita.</param>
    /// <returns>La invitación pendiente.</returns>
    public static Invitation Create(
        Guid tenantId,
        string email,
        MembershipRole role,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        Guid invitedBy)
    {
        if (expiresAt <= now)
        {
            throw new DomainException("La invitación debe vencer en el futuro.");
        }

        var invitation = new Invitation(tenantId)
        {
            Email = TextRules.NormalizeEmail(email),
            Role = TextRules.Defined(role, "El rol"),
            TokenHash = TextRules.TokenHash(tokenHash),
            ExpiresAt = expiresAt,
            Status = InvitationStatus.Pending,
            InvitedBy = TextRules.RequiredId(invitedBy, "Quien invita"),
        };
        invitation.MarkCreated(now, invitedBy);
        return invitation;
    }

    /// <summary>
    /// Acepta la invitación. Si ya venció, no cambia el estado: hay que marcarla como vencida.
    /// </summary>
    /// <param name="userId">Cuenta que acepta.</param>
    /// <param name="now">Momento de la aceptación, en UTC.</param>
    /// <param name="actor">Usuario que acepta.</param>
    public void Accept(Guid userId, DateTimeOffset now, Guid? actor)
    {
        EnsurePending(now);
        var acceptedBy = TextRules.RequiredId(userId, "La persona");
        MarkUpdated(now, actor);
        AcceptedByUserId = acceptedBy;
        AcceptedAt = now;
        Status = InvitationStatus.Accepted;
    }

    /// <summary>Cancela una invitación que todavía está pendiente y no ha vencido.</summary>
    /// <param name="now">Momento de la cancelación, en UTC.</param>
    /// <param name="actor">Usuario que cancela.</param>
    public void Cancel(DateTimeOffset now, Guid? actor)
    {
        EnsurePending(now);
        MarkUpdated(now, actor);
        Status = InvitationStatus.Cancelled;
    }

    /// <summary>Marca la invitación como vencida cuando ya pasó su fecha.</summary>
    /// <param name="now">Momento de la revisión, en UTC.</param>
    /// <param name="actor">Proceso o usuario que la marca.</param>
    public void Expire(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status != InvitationStatus.Pending)
        {
            throw new DomainException("Solo se puede vencer una invitación pendiente.");
        }

        if (now < ExpiresAt)
        {
            throw new DomainException("La invitación todavía está vigente.");
        }

        MarkUpdated(now, actor);
        Status = InvitationStatus.Expired;
    }

    private void EnsurePending(DateTimeOffset now)
    {
        EnsureNotDeleted();
        if (Status != InvitationStatus.Pending)
        {
            throw new DomainException("La invitación ya no está pendiente.");
        }

        if (now >= ExpiresAt)
        {
            throw new DomainException("La invitación está vencida.");
        }
    }
}
