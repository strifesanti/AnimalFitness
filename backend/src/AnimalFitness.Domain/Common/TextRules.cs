using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AnimalFitness.Domain.Common;

/// <summary>
/// Normalización y validación de textos del dominio.
/// </summary>
/// <summary>
/// Normalización y validación de textos del dominio. Usable desde Application e Infrastructure.
/// </summary>
public static partial class TextRules
{
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal)
    {
        "www",
        "api",
        "admin",
        "app",
        "mail",
        "static",
        "animalfitness",
    };

    /// <summary>Texto obligatorio recortado.</summary>
    public static string Required(string? value, string field, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException($"{field} es obligatorio.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} supera los {maxLength} caracteres.");
        }

        return trimmed;
    }

    /// <summary>Nombre o apellido de persona.</summary>
    public static string PersonName(string? value, string field)
    {
        var text = CollapseWhitespace(Required(value, field, FieldLengths.PersonName));
        if (text.Length < 2 || !text.Any(char.IsLetter))
        {
            throw new DomainException($"{field} no es válido.");
        }

        return text;
    }

    /// <summary>Nombre de organización.</summary>
    public static string OrganizationName(string? value)
    {
        var text = CollapseWhitespace(Required(value, "El nombre", FieldLengths.OrganizationName));
        if (text.Length < 2 || !text.Any(char.IsLetter))
        {
            throw new DomainException("El nombre no es válido.");
        }

        return text;
    }

    /// <summary>Correo normalizado a minúsculas.</summary>
    public static string NormalizeEmail(string? email)
    {
        var value = Required(email, "El correo", FieldLengths.Email).ToLowerInvariant();
        if (!EmailRegex().IsMatch(value))
        {
            throw new DomainException("El correo no tiene un formato válido.");
        }

        return value;
    }

    /// <summary>Slug normalizado.</summary>
    public static string NormalizeSlug(string? slug)
    {
        var folded = RemoveDiacritics(Required(slug, "El identificador", FieldLengths.Slug)).ToLowerInvariant();
        if (folded.Length < 3 || !SlugRegex().IsMatch(folded) || ReservedSlugs.Contains(folded))
        {
            throw new DomainException("El identificador solo puede contener minúsculas, números y guiones, y no puede ser una palabra reservada.");
        }

        return folded;
    }

    /// <summary>Teléfono opcional.</summary>
    public static string? OptionalPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var value = phone.Trim();
        if (value.Length > FieldLengths.Phone || !PhoneRegex().IsMatch(value))
        {
            throw new DomainException("El teléfono no es válido.");
        }

        return value;
    }

    /// <summary>Moneda ISO.</summary>
    public static string NormalizeCurrency(string? currency)
    {
        var value = string.IsNullOrWhiteSpace(currency)
            ? DomainConstants.DefaultCurrency
            : currency.Trim().ToUpperInvariant();

        if (!CurrencyRegex().IsMatch(value))
        {
            throw new DomainException("La moneda debe ser un código ISO de tres letras.");
        }

        return value;
    }

    /// <summary>Cultura, por ejemplo es-CO.</summary>
    public static string NormalizeLocale(string? locale)
    {
        var value = string.IsNullOrWhiteSpace(locale)
            ? DomainConstants.DefaultCulture
            : locale.Trim();

        var separator = value.IndexOf('-', StringComparison.Ordinal);
        if (separator > 0 && separator < value.Length - 1)
        {
            value = string.Concat(
                value[..separator].ToLowerInvariant(),
                "-",
                value[(separator + 1)..].ToUpperInvariant());
        }

        if (!LocaleRegex().IsMatch(value))
        {
            throw new DomainException("La cultura no es válida. Usa el formato es-CO.");
        }

        return value;
    }

    /// <summary>Color #RRGGBB.</summary>
    public static string NormalizeColor(string? color)
    {
        var value = string.IsNullOrWhiteSpace(color)
            ? DomainConstants.DefaultAccentColor
            : color.Trim().ToUpperInvariant();

        if (!ColorRegex().IsMatch(value))
        {
            throw new DomainException("El color debe expresarse como #RRGGBB.");
        }

        return value;
    }

    /// <summary>Zona horaria del sistema.</summary>
    public static string RequireTimeZone(string? timeZoneId)
    {
        var value = string.IsNullOrWhiteSpace(timeZoneId)
            ? DomainConstants.DefaultTimeZoneId
            : timeZoneId.Trim();

        if (value.Length > FieldLengths.TimeZoneId || !TimeZoneInfo.TryFindSystemTimeZoneById(value, out _))
        {
            throw new DomainException("La zona horaria no es válida.");
        }

        return value;
    }

    /// <summary>Objeto JSON obligatorio.</summary>
    public static string RequireJsonObject(string? json)
    {
        var value = string.IsNullOrWhiteSpace(json) ? DomainConstants.EmptySettingsJson : json.Trim();
        if (value.Length > FieldLengths.SettingsJson)
        {
            throw new DomainException($"La configuración supera los {FieldLengths.SettingsJson} caracteres.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(value);
        }
        catch (JsonException)
        {
            throw new DomainException("La configuración no es JSON válido.");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new DomainException("La configuración debe ser un objeto JSON.");
            }
        }

        return value;
    }

    /// <summary>Objeto JSON opcional.</summary>
    public static string? OptionalJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return RequireJsonObject(json);
    }

    /// <summary>Hash de token de invitación.</summary>
    public static string TokenHash(string? tokenHash)
    {
        var value = Required(tokenHash, "El hash de la invitación", FieldLengths.TokenHash);
        if (value.Length < FieldLengths.TokenHashMinimum || value.Any(char.IsWhiteSpace))
        {
            throw new DomainException("El hash de la invitación no es válido.");
        }

        return value;
    }

    /// <summary>Dirección IP opcional.</summary>
    public static string? OptionalIp(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var value = ipAddress.Trim();
        if (value.Length > FieldLengths.IpAddress || value.Contains(' ', StringComparison.Ordinal))
        {
            throw new DomainException("La dirección IP no es válida.");
        }

        return value;
    }

    /// <summary>Agente de usuario opcional.</summary>
    public static string? OptionalUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var value = userAgent.Trim();
        if (value.Length > FieldLengths.UserAgent)
        {
            throw new DomainException($"El agente de usuario supera los {FieldLengths.UserAgent} caracteres.");
        }

        return value;
    }

    /// <summary>Enum definido.</summary>
    public static TEnum Defined<TEnum>(TEnum value, string field)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{field} no es válido.");
        }

        return value;
    }

    /// <summary>Identificador no vacío.</summary>
    public static Guid RequiredId(Guid id, string field)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"{field} es obligatorio.");
        }

        return id;
    }

    private static string CollapseWhitespace(string value)
    {
        var cleaned = value.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', parts);
    }

    private static string RemoveDiacritics(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"^\+?[0-9][0-9 \-()]{6,28}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyRegex();

    [GeneratedRegex(@"^[a-z]{2}-[A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex LocaleRegex();

    [GeneratedRegex(@"^#[0-9A-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();
}
