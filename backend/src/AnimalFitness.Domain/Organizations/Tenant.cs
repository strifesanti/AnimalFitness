using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Organizations;

/// <summary>
/// Academia, gimnasio o entrenador que contrata AnimalFitness. No pertenece a otra organización.
/// </summary>
public sealed class Tenant : SoftDeletableEntity
{
    private Tenant()
    {
    }

    /// <summary>Nombre visible de la organización.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Identificador legible y único. Está pensado para usarse también como subdominio.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Estado operativo.</summary>
    public TenantStatus Status { get; private set; }

    /// <summary>Archivo del logotipo. Nulo si todavía no tiene.</summary>
    public Guid? LogoFileId { get; private set; }

    /// <summary>Correo de contacto.</summary>
    public string ContactEmail { get; private set; } = string.Empty;

    /// <summary>Teléfono de contacto.</summary>
    public string? Phone { get; private set; }

    /// <summary>Zona horaria en la que se presentan las fechas.</summary>
    public string TimeZoneId { get; private set; } = DomainConstants.DefaultTimeZoneId;

    /// <summary>Moneda en la que la organización cobra a sus alumnos.</summary>
    public string Currency { get; private set; } = DomainConstants.DefaultCurrency;

    /// <summary>
    /// Crea una organización en periodo de prueba.
    /// </summary>
    /// <param name="name">Nombre visible.</param>
    /// <param name="slug">Identificador legible.</param>
    /// <param name="contactEmail">Correo de contacto.</param>
    /// <param name="now">Momento de creación, en UTC.</param>
    /// <param name="createdBy">Autor. Nulo si lo crea un proceso del sistema.</param>
    /// <param name="phone">Teléfono de contacto.</param>
    /// <param name="timeZoneId">Zona horaria. Si se omite, se usa America/Bogota.</param>
    /// <param name="currency">Moneda ISO. Si se omite, se usa COP.</param>
    /// <returns>La organización creada.</returns>
    public static Tenant Create(
        string name,
        string slug,
        string contactEmail,
        DateTimeOffset now,
        Guid? createdBy = null,
        string? phone = null,
        string? timeZoneId = null,
        string? currency = null)
    {
        var tenant = new Tenant
        {
            Name = TextRules.OrganizationName(name),
            Slug = TextRules.NormalizeSlug(slug),
            Status = TenantStatus.Trial,
            ContactEmail = TextRules.NormalizeEmail(contactEmail),
            Phone = TextRules.OptionalPhone(phone),
            TimeZoneId = TextRules.RequireTimeZone(timeZoneId),
            Currency = TextRules.NormalizeCurrency(currency),
        };
        tenant.MarkCreated(now, createdBy);
        return tenant;
    }

    /// <summary>Actualiza el nombre y los datos de contacto.</summary>
    /// <param name="name">Nombre visible.</param>
    /// <param name="contactEmail">Correo de contacto.</param>
    /// <param name="phone">Teléfono de contacto.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void UpdateProfile(string name, string contactEmail, string? phone, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var normalizedName = TextRules.OrganizationName(name);
        var email = TextRules.NormalizeEmail(contactEmail);
        var normalizedPhone = TextRules.OptionalPhone(phone);
        MarkUpdated(now, actor);
        Name = normalizedName;
        ContactEmail = email;
        Phone = normalizedPhone;
    }

    /// <summary>Cambia el identificador legible.</summary>
    /// <param name="slug">Nuevo identificador.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void ChangeSlug(string slug, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var normalized = TextRules.NormalizeSlug(slug);
        if (normalized == Slug)
        {
            return;
        }

        MarkUpdated(now, actor);
        Slug = normalized;
    }

    /// <summary>Cambia la moneda de cobro a los alumnos.</summary>
    /// <param name="currency">Código ISO de tres letras.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void ChangeCurrency(string currency, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var normalized = TextRules.NormalizeCurrency(currency);
        if (normalized == Currency)
        {
            return;
        }

        MarkUpdated(now, actor);
        Currency = normalized;
    }

    /// <summary>Cambia la zona horaria de presentación.</summary>
    /// <param name="timeZoneId">Identificador de zona horaria.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void ChangeTimeZone(string timeZoneId, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var normalized = TextRules.RequireTimeZone(timeZoneId);
        if (normalized == TimeZoneId)
        {
            return;
        }

        MarkUpdated(now, actor);
        TimeZoneId = normalized;
    }

    /// <summary>Asocia el logotipo.</summary>
    /// <param name="fileId">Archivo previamente almacenado.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void SetLogo(Guid fileId, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var logoId = TextRules.RequiredId(fileId, "El logotipo");
        MarkUpdated(now, actor);
        LogoFileId = logoId;
    }

    /// <summary>Quita el logotipo.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void ClearLogo(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (LogoFileId is null)
        {
            return;
        }

        LogoFileId = null;
        MarkUpdated(now, actor);
    }

    /// <summary>Pasa la organización a estado activo.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void Activate(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status == TenantStatus.Active)
        {
            throw new DomainException("La organización ya está activa.");
        }

        MarkUpdated(now, actor);
        Status = TenantStatus.Active;
    }

    /// <summary>Bloquea la operación de la organización.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void Suspend(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (Status == TenantStatus.Suspended)
        {
            throw new DomainException("La organización ya está suspendida.");
        }

        MarkUpdated(now, actor);
        Status = TenantStatus.Suspended;
    }
}
