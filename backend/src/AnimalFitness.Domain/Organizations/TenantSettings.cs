using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Organizations;

/// <summary>
/// Preferencias de una organización: idioma, moneda, color y configuración adicional.
/// </summary>
public sealed class TenantSettings : TenantScopedEntity
{
    private TenantSettings(Guid tenantId)
        : base(tenantId)
    {
    }

    /// <summary>Color de acento en formato #RRGGBB.</summary>
    public string PrimaryColor { get; private set; } = DomainConstants.DefaultAccentColor;

    /// <summary>Cultura de presentación, por ejemplo es-CO.</summary>
    public string Locale { get; private set; } = DomainConstants.DefaultCulture;

    /// <summary>Moneda predeterminada de la organización.</summary>
    public string Currency { get; private set; } = DomainConstants.DefaultCurrency;

    /// <summary>Configuración adicional en un objeto JSON.</summary>
    public string SettingsJson { get; private set; } = DomainConstants.EmptySettingsJson;

    /// <summary>
    /// Crea las preferencias con los valores predeterminados de la plataforma.
    /// </summary>
    /// <param name="tenantId">Organización propietaria.</param>
    /// <param name="now">Momento de creación, en UTC.</param>
    /// <param name="createdBy">Autor.</param>
    /// <returns>Las preferencias creadas.</returns>
    public static TenantSettings Create(Guid tenantId, DateTimeOffset now, Guid? createdBy)
    {
        var settings = new TenantSettings(tenantId);
        settings.MarkCreated(now, createdBy);
        return settings;
    }

    /// <summary>Reemplaza las preferencias visibles y la configuración JSON.</summary>
    /// <param name="primaryColor">Color de acento #RRGGBB.</param>
    /// <param name="locale">Cultura, por ejemplo es-CO.</param>
    /// <param name="currency">Moneda ISO.</param>
    /// <param name="settingsJson">Objeto JSON. Si se omite, queda vacío.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void Update(
        string? primaryColor,
        string? locale,
        string? currency,
        string? settingsJson,
        DateTimeOffset now,
        Guid? actor)
    {
        EnsureNotDeleted();
        var color = TextRules.NormalizeColor(primaryColor);
        var normalizedLocale = TextRules.NormalizeLocale(locale);
        var normalizedCurrency = TextRules.NormalizeCurrency(currency);
        var json = TextRules.RequireJsonObject(settingsJson);
        MarkUpdated(now, actor);
        PrimaryColor = color;
        Locale = normalizedLocale;
        Currency = normalizedCurrency;
        SettingsJson = json;
    }
}
